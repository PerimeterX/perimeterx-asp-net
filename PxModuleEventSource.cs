using System.Diagnostics.Tracing;

namespace PerimeterX
{
    [EventSource(Name = "PerimeterX.PxModule")]
    sealed class PxModuleEventSource : EventSource
    {
        public static PxModuleEventSource Log = new PxModuleEventSource();

        [Event(1, Message = "Failure: {0}", Level = EventLevel.Error)]
        public void Failure(string message) { WriteEvent(1, message); }

        [Event(2, Message = "No risk cookie found. RawUrl={0}", Level = EventLevel.Verbose)]
        public void NoRiskCookie(string rawUrl)
        {
            if (IsEnabled()) WriteEvent(2, rawUrl);
        }
        
        [Event(3, Message = "Expired risk cookie. RawUrl={0}", Level = EventLevel.Verbose)]
        public void CookieExpired(string rawUrl)
        {
            if (IsEnabled()) WriteEvent(3, rawUrl);
        }

        [Event(4, Message = "Invalid risk cookie: {1}. RawUrl={0}", Level = EventLevel.Verbose)]
        public void InvalidCookie(string rawUrl, string reason)
        {
            if (IsEnabled()) WriteEvent(4, rawUrl, reason);
        }

        [Event(5, Message = "Ignore request: {1}. RawUrl={0}", Level = EventLevel.Verbose)]
        public void IgnoreRequest(string rawUrl, string reason)
        {
            if (IsEnabled()) WriteEvent(5, rawUrl, reason);
        }

        [Event(6, Message = "Request blocked: {1}. UUID={2} RawUrl={0}", Level = EventLevel.Informational)]
        public void RequestBlocked(string rawUrl, string reason, string uuid)
        {
            if (IsEnabled()) WriteEvent(6, rawUrl, reason, uuid);
        }

        [Event(7, Message = "Failed risk API called failed: {1} RawUrl={0}", Level = EventLevel.Error)]
        public void FailedRiskApi(string rawUrl, string reason)
        {
            WriteEvent(7, rawUrl, reason);
        }

        [Event(8, Message = "Failed posting activities ({0}): {1}", Level = EventLevel.Error)]
        public void FailedPostActivities(int activityCount, string error)
        {
            WriteEvent(8, activityCount, error);
        }
    }
}
