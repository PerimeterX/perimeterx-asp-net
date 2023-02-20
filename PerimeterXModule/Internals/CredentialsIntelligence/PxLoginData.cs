using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Jil;
using PerimeterX.CustomBehavior;

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

        public LoginCredentialsFields ExtractCredentialsFromRequest(PxContext context, HttpRequest request, ICredentialsExtractionHandler credentialsExtractionHandler)
        {
            try
            {
                ExtractedCredentials extractedCredentials = null;

                if (credentialsExtractionHandler != null)
                {
                    extractedCredentials = credentialsExtractionHandler.Handle(request);
                }
                else
                {
                    ExtractorObject extractionDetails = FindMatchCredentialsDetails(request);

                    if (extractionDetails != null)
                    {
                        extractedCredentials = ExtractCredentials(extractionDetails, context, request);
                    }
                }

                if (extractedCredentials != null)
                {
                    return protocol.ProcessCredentials(extractedCredentials);
                }

                return null;
            } catch (Exception ex)
            {
                PxLoggingUtils.LogError(string.Format("Failed to extract credentials.", ex.Message));
            }

            return null;
        }

        private ExtractorObject FindMatchCredentialsDetails(HttpRequest request)
        {
            foreach (ExtractorObject loginObject in this.loginCredentialsExtractor)
            {
                if (IsRequestMatchLoginRequestConfiguration(loginObject, request))
                {
                    return loginObject;
                }
            }

            return null;
        }

        private static bool IsRequestMatchLoginRequestConfiguration(ExtractorObject extractorObject, HttpRequest request)
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

        private ExtractedCredentials ExtractCredentials(ExtractorObject extractionDetails, PxContext pxContext, HttpRequest request)
        {
            string userFieldName = extractionDetails.UserFieldName;
            string passwordFieldName = extractionDetails.PassFieldName;

            if (userFieldName == null || passwordFieldName == null)
            {
                return null;
            }

            Dictionary<string, string> headers = pxContext.GetHeadersAsDictionary();

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

        private async Task<ExtractedCredentials> ExtractFromBodyAsync(string userFieldName, string passwordFieldName, Dictionary<string, string> headers, HttpRequest request)
        {
            bool isContentTypeHeaderExist = headers.TryGetValue("Content-Type", out string contentType);

            HttpRequest httpRequest = request;

            string body = await BodyReader.ReadRequestBodyAsync(httpRequest);

            if (!isContentTypeHeaderExist)
            {
                return null;
            } else if (contentType.Contains("application/json"))
            {
                return ExtractCredentialsFromJson(body, userFieldName, passwordFieldName);
            } else if (contentType.Contains("x-www-form-urlencoded"))
            {
                return ReadValueFromUrlEncoded(body, userFieldName, passwordFieldName);
            } else if (contentType.Contains("form-data"))
            {
                return ExtractValueFromMultipart(body, contentType, userFieldName, passwordFieldName);
            }
            
            return null;
        }

        private ExtractedCredentials ExtractCredentialsFromJson(string body, string userFieldName, string passwordFieldName) {

            dynamic jsonBody = JSON.DeserializeDynamic(body, PxConstants.JSON_OPTIONS);

            string userValue = PxCommonUtils.ExtractValueFromNestedJson(userFieldName, jsonBody);
            string passValue = PxCommonUtils.ExtractValueFromNestedJson(passwordFieldName, jsonBody);

            return new ExtractedCredentials(userValue, passValue);
        }

        private ExtractedCredentials ReadValueFromUrlEncoded(string body, string userFieldName, string passwordFieldName)
        {
            var parametersQueryString = HttpUtility.ParseQueryString(body);
            var parametersDictionary = new Dictionary<string, string>();
            foreach (var key in parametersQueryString.AllKeys)
            {
                parametersDictionary.Add(key, parametersQueryString[key]);
            }

            return ExtractCredentialsFromDictinary(parametersDictionary, userFieldName, passwordFieldName);
        }

        private ExtractedCredentials ExtractValueFromMultipart(string body, string contentType, string userFieldName, string passwordFieldName)
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

        private ExtractedCredentials ExtractCredentialsFromDictinary(Dictionary<string, string> parametersDictionary, string userFieldName, string passwordFieldName)
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
