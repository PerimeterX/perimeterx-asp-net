// 	Copyright © 2016 PerimeterX, Inc.
// 
// Permission is hereby granted, free of charge, to any
// person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the
// Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice
// shall be included in all copies or substantial portions of
// the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY
// KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE AUTHORS
// OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR
// OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
using Jil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PerimeterX
{
    public interface IActivityReporter
    {
        bool Post(Activity activity);
    }

    public class NullActivityMonitor : IActivityReporter
    {
        public bool Post(Activity activity)
        {
            return true;
        }
    }

    public sealed class ActivityReporter : IActivityReporter, IDisposable
    {
        private readonly BlockingCollection<Activity> activities;
        private readonly int bulkSize;
        private readonly HttpClient httpClient;
	private readonly string postUri;
        private readonly Options jsonOptions = new Options(false, true);

        public ActivityReporter(string baseUri, int capacity = 500, int bulkSize = 10, int timeout = 5000)
        {
            this.postUri = baseUri + PxConstants.ACTIVITIES_API_PATH;
            this.activities = new BlockingCollection<Activity>(capacity);
            this.bulkSize = bulkSize;
            this.httpClient = PxConstants.CreateHttpClient(false, timeout, false);

            Task.Run(() => SendActivitiesTask());
	    PxLoggingUtils.LogDebug("Reporter initialized");
        }

        public bool Post(Activity activity)
        {
            var added = activities.TryAdd(activity);
	    if (!added)
	    {
	    	PxLoggingUtils.LogError("Failed to post activity");
	    }
            return added;
        }

        private void SendActivitiesTask()
        {
            var bulk = new List<Activity>(this.bulkSize);
            while (!activities.IsCompleted)
            {
                Activity activity;
                for (int i = 0; i < bulkSize; ++i)
                {
                    if (!activities.TryTake(out activity, 1000))
                    {
                        break;
                    }

                    bulk.Add(activity);
                }
                if (bulk.Count > 0)
                {
                    SendActivities(bulk);
                    bulk.Clear();
                }
            }
        }

        private void SendActivities(List<Activity> activities)
        {
            try
            {
                var stringBuilder = new StringBuilder();
                using (var stringOutput = new StringWriter(stringBuilder))
                {
                    JSON.SerializeDynamic(activities, stringOutput, PxConstants.JSON_OPTIONS);
                }

                var content = new StringContent(stringBuilder.ToString(), Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(postUri, content).Result;
		PxLoggingUtils.LogDebug(string.Format("Post request for {0} ({1}), returned {2}", postUri, stringBuilder.ToString(), response));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
		PxLoggingUtils.LogError(string.Format("Failed sending activities (count {0}) - {1}", activities.Count, ex.Message));
            }
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
            this.activities.Dispose();
        }
    }
}
