using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using Jil;
using Microsoft.SqlServer.Server;
using PerimeterX.Internals.CredentialsIntelligence;

namespace PerimeterX
{
    public class PxLoginData
    {
        private ICredentialsIntelligenceProtocol protocol;
        private List<ExtractorObject> loginCredentialsExtractor;

        public PxLoginData(string ciVersion, List<ExtractorObject> loginCredentialsExraction)
        {
            this.protocol = CredentialsIntelligenceProtocolFactory.Create(ciVersion);
            this.loginCredentialsExtractor = loginCredentialsExraction;
        }

        public LoginCredentialsFields ExtractCredentials(PxContext context, HttpRequest request)
        {
            ExtractorObject extarctionDetails = FindMatchCredentialsDetails(request);
            if (extarctionDetails != null)
            {
                ExtractedCredentials extractedCredentials = ExtractCredentials(extarctionDetails, context, request);
                if (extractedCredentials != null)
                {
                    return protocol.ProcessCredentials(extractedCredentials);
                }
            }

            return null;
        }

        private ExtractorObject FindMatchCredentialsDetails(HttpRequest request)
        {
            foreach (ExtractorObject loginObject in this.loginCredentialsExtractor)
            {
                if (IsMatchedPath(loginObject, request))
                {
                    return loginObject;
                }
            }

            return null;
        }

        public static bool IsMatchedPath(ExtractorObject extractorObject, HttpRequest request)
        {
            if (request.HttpMethod.ToLower() == extractorObject.Method)
            {
                if (extractorObject.PathType == "exact" && request.Path == extractorObject.Path)
                {
                    return true;
                }

                if (extractorObject.PathType == "regex" && Regex.IsMatch(request.Path, extractorObject.Path))
                {
                    return true;
                }
            }

            return false;
        }

        public ExtractedCredentials ExtractCredentials(ExtractorObject extractionDetails, PxContext pxContext, HttpRequest request)
        {
            string userFieldName = extractionDetails.UserFieldName;
            string passwordFieldName = extractionDetails.PassFieldName;

            Dictionary<string, string> headers = pxContext.GetHeadersAsDictionary();

            if (userFieldName == null || passwordFieldName == null)
            {
                return null;
            }

            if (extractionDetails.SentThrough == "header")
            {
                return ExtractFromHeader(userFieldName, passwordFieldName, headers);
            } else if (extractionDetails.SentThrough == "query-param")
            {
                return new ExtractedCredentials(
                    request.QueryString[userFieldName].Replace(" ", "+"),
                    request.QueryString[passwordFieldName].Replace(" ", "+")
                );
            } else if (extractionDetails.SentThrough == "body")
            {
                return ExtractFromBodyAsync(userFieldName, passwordFieldName, headers, request).Result;
            }

            return null;
        }

        public static ExtractedCredentials ExtractFromHeader(string userFieldName, string passwordFieldName, Dictionary<string, string> headers)
        {
            bool isUsernameHeaderExist = headers.TryGetValue(userFieldName, out string userName);
            bool isPasswordHeaderExist = headers.TryGetValue(passwordFieldName, out string password);

            if (!isUsernameHeaderExist && !isPasswordHeaderExist) { return null; }

            return new ExtractedCredentials(userName, password);
        }

        public async Task<ExtractedCredentials> ExtractFromBodyAsync(string userFieldName, string passwordFieldName, Dictionary<string, string> headers, HttpRequest request)
        {
            bool isContentTypeHeaderExist = headers.TryGetValue("Content-Type", out string contentType);

            string body = await ReadRequestBodyAsync(request);

            if (!isContentTypeHeaderExist)
            {
                return null;
            } else if (contentType.Contains("application/json"))
            {
                return ConvertToJson(body, userFieldName, passwordFieldName);
            } else if (contentType.Contains("x-www-form-urlencoded"))
            {
                return ReadValueFromUrlEncoded(body, userFieldName, passwordFieldName);
            } else if (contentType.Contains("form-data"))
            {
                return ExtarctValueFromMultipart(body, contentType, userFieldName, passwordFieldName);
            }
            
            return null;
        }

        public static async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            using (var reader = new StreamReader(request.InputStream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        public ExtractedCredentials ConvertToJson(string body, string userFieldName, string passwordFieldName) {

            dynamic jsonBody = JSON.DeserializeDynamic(body, PxConstants.JSON_OPTIONS);

            string userValue = PxCommonUtils.ExtractValueFromNestedJson(userFieldName, jsonBody);
            string passValue = PxCommonUtils.ExtractValueFromNestedJson(passwordFieldName, jsonBody);

            return new ExtractedCredentials(userValue, passValue);
        }

        public ExtractedCredentials ReadValueFromUrlEncoded(string body, string userFieldName, string passwordFieldName)
        {
            var parametersQueryString = HttpUtility.ParseQueryString(body);
            var parametersDictionary = new Dictionary<string, string>();
            foreach (var key in parametersQueryString.AllKeys)
            {
                parametersDictionary.Add(key, parametersQueryString[key]);
            }

            return ExtractCredentialsFromDictinary(parametersDictionary, userFieldName, passwordFieldName);
        }

        public ExtractedCredentials ExtarctValueFromMultipart(string body, string contentType, string userFieldName, string passwordFieldName)
        {
            var formData = new Dictionary<string, string>();

            var boundary = contentType.Split(';')
                  .SingleOrDefault(x => x.Trim().StartsWith("boundary="))?
                  .Split('=')[1];

            var parts = body.Split(new[] { "--" + boundary }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var lines = part.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                if (part.StartsWith("--"))
                {
                    continue;
                }

                var key = string.Empty;
                var value = new StringBuilder();
                foreach (var line in lines)
                {
                    if (line.StartsWith("Content-Disposition"))
                    {
                        key = line.Split(new[] { "name=" }, StringSplitOptions.RemoveEmptyEntries)[1].Trim('\"');
                    }
                    else
                    {
                        value.Append(line);
                    }
                }

                formData.Add(key, value.ToString());
            }

            return ExtractCredentialsFromDictinary(formData, userFieldName, passwordFieldName);
        }

        public ExtractedCredentials ExtractCredentialsFromDictinary(Dictionary<string, string> parametersDictionary, string userFieldName, string passwordFieldName)
        {
            bool isUsernameExist = parametersDictionary.TryGetValue(userFieldName, out string userField);
            bool isPasswordExist = parametersDictionary.TryGetValue(passwordFieldName, out string passwordField);

            if (!isUsernameExist && !isPasswordExist)
            {
                return null;
            }

            return new ExtractedCredentials(userField, passwordField);
        }
    } 


}
