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
    public class CredentialIntelligenceManager
    {
        private ICredentialsIntelligenceProtocol protocol;
        private List<ExtractorObject> loginCredentialsExtractors;

        public CredentialIntelligenceManager(string ciVersion, List<ExtractorObject> loginCredentialsExraction)
        {
            this.protocol = CredentialsIntelligenceProtocolFactory.Create(ciVersion);
            this.loginCredentialsExtractors = loginCredentialsExraction;
        }

        public LoginCredentialsFields ExtractCredentialsFromRequest(PxContext context, HttpRequest request, ICredentialsExtractionHandler credentialsExtractionHandler)
        {
            try
            {  
                ExtractorObject extractionDetails = FindMatchCredentialsDetails(request);

                if (extractionDetails != null)
                {
                    ExtractedCredentials extractedCredentials = ExtractLoginCredentials(context, request, credentialsExtractionHandler, extractionDetails);


                    if (extractedCredentials != null)
                    {
                        return protocol.ProcessCredentials(extractedCredentials);
                    }
                }

            } catch (Exception ex)
            {
                PxLoggingUtils.LogError(string.Format("Failed to extract credentials.", ex.Message));
            }

            return null;
        }

        private ExtractedCredentials ExtractLoginCredentials(PxContext context, HttpRequest request, ICredentialsExtractionHandler credentialsExtractionHandler, ExtractorObject extractionDetails)
        {
            if (credentialsExtractionHandler != null)
            {
                return credentialsExtractionHandler.Handle(request);
            }
            else
            {
                return HandleExtractCredentials(extractionDetails, context, request);
            }
        }

        private ExtractorObject FindMatchCredentialsDetails(HttpRequest request)
        {
            foreach (ExtractorObject loginObject in this.loginCredentialsExtractors)
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

        private ExtractedCredentials HandleExtractCredentials(ExtractorObject extractionDetails, PxContext pxContext, HttpRequest request)
        {
            string userFieldName = extractionDetails.UserFieldName;
            string passwordFieldName = extractionDetails.PassFieldName;

            if (userFieldName == null || passwordFieldName == null)
            {
                return null;
            }

            Dictionary<string, string> headers = pxContext.lowercaseHttpHeaders;

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
                return ExtractFromBody(userFieldName, passwordFieldName, headers, request);
            }

            return null;
        }

        public static ExtractedCredentials ExtractFromHeader(string userFieldName, string passwordFieldName, Dictionary<string, string> headers)
        {
            bool isUsernameHeaderExist = headers.TryGetValue(userFieldName.ToLower(), out string userName);
            bool isPasswordHeaderExist = headers.TryGetValue(passwordFieldName.ToLower(), out string password);

            if (!isUsernameHeaderExist && !isPasswordHeaderExist) { return null; }

            return new ExtractedCredentials(userName, password);
        }

        private  ExtractedCredentials ExtractFromBody(string userFieldName, string passwordFieldName, Dictionary<string, string> headers, HttpRequest request)
        {
            bool isContentTypeHeaderExist = headers.TryGetValue("content-type", out string contentType);

            string body = BodyReader.ReadRequestBody(request);

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

            return ExtractCredentialsFromDictionary(parametersDictionary, userFieldName, passwordFieldName);
        }

        private ExtractedCredentials ExtractValueFromMultipart(string body, string contentType, string userFieldName, string passwordFieldName)
        {
            Dictionary<string, string> formData = BodyReader.GetFormDataContentAsDictionary(body, contentType);

            return ExtractCredentialsFromDictionary(formData, userFieldName, passwordFieldName);
        }

        private ExtractedCredentials ExtractCredentialsFromDictionary(Dictionary<string, string> parametersDictionary, string userFieldName, string passwordFieldName)
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
