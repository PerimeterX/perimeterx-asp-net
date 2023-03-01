namespace PerimeterX
{
    public class AdditionalS2SUtils
    {
        public static Activity CreateAdditionalS2SActivity(PxModuleConfigurationSection config, int? statusCode, bool? loginSuccessful, PxContext pxContext)
        {
            bool isBreachedAccount = pxContext.IsBreachedAccount();
            LoginCredentialsFields loginCredentialsFields = pxContext.LoginCredentialsFields;

            bool shouldAddRawUsername = isBreachedAccount &&
                config.SendRawUsernameOnAdditionalS2SActivity &&
                loginCredentialsFields.RawUsername != null;

            AdditionalS2SActivityDetails details = new AdditionalS2SActivityDetails
            {
                ModuleVersion = PxConstants.MODULE_VERSION,
                ClientUuid = pxContext.UUID,
                RequestId = pxContext.RequestId,
                CiVersion = config.CiVersion,
                CredentialsCompromised = isBreachedAccount,
                SsoStep = config.CiVersion == CIVersion.MULTISTEP_SSO ? loginCredentialsFields.SsoStep : null
            };

            if (statusCode.HasValue)
            {
                details.HttpStatusCode = statusCode.Value;
            }

            if (loginSuccessful.HasValue)
            {
                bool isLoginSuccessful = loginSuccessful.Value;
                details.LoginSuccessful = isLoginSuccessful;
                details.RawUsername = shouldAddRawUsername && isLoginSuccessful ? loginCredentialsFields.RawUsername : null;
            }

            var activity = new Activity
            {
                Type = "additional_s2s",
                Timestamp = PxConstants.GetTimestamp(),
                AppId = config.AppId,
                SocketIP = pxContext.Ip,
                Url = pxContext.FullUrl,
                Details = details,
            };

            if (!string.IsNullOrEmpty(pxContext.Vid))
            {
                activity.Vid = pxContext.Vid;
            }

            return activity;
        }
    }
}
