using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PerimeterX.CustomBehavior;

namespace PerimeterX
{
    public static class PxCustomFunctions
    {
        public static ILoginSuccessfulHandler GetCustomLoginSuccessfulHandler(string customHandlerName)
        {
            if (string.IsNullOrEmpty(customHandlerName))
            {
                return null;
            }

            try
            {
                var customLoginSuccessfulHandler =
                    getAssembliesTypes().FirstOrDefault(t => t.GetInterface(typeof(ILoginSuccessfulHandler).Name) != null &&
                                                  t.Name.Equals(customHandlerName) && t.IsClass && !t.IsAbstract);

                if (customLoginSuccessfulHandler != null)
                {
                    var instance = (ILoginSuccessfulHandler)Activator.CreateInstance(customLoginSuccessfulHandler, null);
                    PxLoggingUtils.LogDebug(string.Format("Successfully loaded ICustomLoginSuccessfulHandler '{0}'.", customHandlerName));
                    return instance;
                }
                else
                {
                    PxLoggingUtils.LogDebug(string.Format(
                        "Missing implementation of the configured ICustomLoginSuccessfulHandler ('customLoginSuccessfulHandler' attribute): {0}.",
                        customHandlerName));
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                PxLoggingUtils.LogError(string.Format("Failed to load the ICustomLoginSuccessfulHandler '{0}': {1}.",
                                              customHandlerName, ex.Message));
            }
            catch (Exception ex)
            {
                PxLoggingUtils.LogError(string.Format("Encountered an error while retrieving the ICustomLoginSuccessfulHandler '{0}': {1}.",
                                              customHandlerName, ex.Message));
            }

            return null;
        }

        private static IEnumerable<Type> getAssembliesTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a =>
                    {
                        try
                        {
                            return a.GetTypes();
                        }
                        catch
                        {
                            return new Type[0];
                        }
                    });
        }

        /// <summary>
        /// Uses reflection to check whether an IVerificationHandler was implemented by the customer.
        /// </summary>
        /// <returns>If found, returns the IVerificationHandler class instance. Otherwise, returns null.</returns>
        public static IVerificationHandler GetCustomVerificationHandler(string customHandlerName)
		{
			if (string.IsNullOrEmpty(customHandlerName))
			{
				return null;
			}

			try
			{
				var customVerificationHandlerType = getAssembliesTypes()
							 .FirstOrDefault(t => t.GetInterface(typeof(IVerificationHandler).Name) != null &&
												  t.Name.Equals(customHandlerName) && t.IsClass && !t.IsAbstract);

				if (customVerificationHandlerType != null)
				{
					var instance = (IVerificationHandler)Activator.CreateInstance(customVerificationHandlerType, null);
					PxLoggingUtils.LogDebug(string.Format("Successfully loaded ICustomeVerificationHandler '{0}'.", customHandlerName));
					return instance;
				}
				else
				{
					PxLoggingUtils.LogDebug(string.Format(
						"Missing implementation of the configured IVerificationHandler ('customVerificationHandler' attribute): {0}.",
						customHandlerName));
				}
			}
			catch (ReflectionTypeLoadException ex)
			{
				PxLoggingUtils.LogError(string.Format("Failed to load the ICustomeVerificationHandler '{0}': {1}.",
											  customHandlerName, ex.Message));
			}
			catch (Exception ex)
			{
				PxLoggingUtils.LogError(string.Format("Encountered an error while retrieving the ICustomeVerificationHandler '{0}': {1}.",
											  customHandlerName, ex.Message));
			}

			return null;
		}        
		
		public static ICredentialsExtractionHandler GetCustomLoginCredentialsExtractionHandler(string customHandlerName)
		{
			if (string.IsNullOrEmpty(customHandlerName))
			{
				return null;
			}

			try
			{
				var customCredentialsExtraction = getAssembliesTypes()
							 .FirstOrDefault(t => t.GetInterface(typeof(ICredentialsExtractionHandler).Name) != null &&
												  t.Name.Equals(customHandlerName) && t.IsClass && !t.IsAbstract);

				if (customCredentialsExtraction != null)
				{
					var instance = (ICredentialsExtractionHandler)Activator.CreateInstance(customCredentialsExtraction, null);
					PxLoggingUtils.LogDebug(string.Format("Successfully loaded ICredentialsExtractionHandler '{0}'.", customHandlerName));
					return instance;
				}
				else
				{
					PxLoggingUtils.LogDebug(string.Format(
                        "Missing implementation of the configured IVerificationHandler ('ICredentialsExtractionHandler' attribute): {0}.",
						customHandlerName));
				}
			}
			catch (ReflectionTypeLoadException ex)
			{
				PxLoggingUtils.LogError(string.Format("Failed to load the ICredentialsExtractionHandler '{0}': {1}.",
											  customHandlerName, ex.Message));
			}
			catch (Exception ex)
			{
				PxLoggingUtils.LogError(string.Format("Encountered an error while retrieving the ICredentialsExtractionHandler '{0}': {1}.",
											  customHandlerName, ex.Message));
			}

			return null;
		}
    }
}