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
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

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
            this.postUri = baseUri + @"/api/v1/collector/s2s";
            this.activities = new BlockingCollection<Activity>(capacity);
            this.bulkSize = bulkSize;
            var webRequestHandler = new WebRequestHandler
            {
                AllowPipelining = true,
                UseDefaultCredentials = true,
                UnsafeAuthenticatedConnectionSharing = true
            };
            this.httpClient = new HttpClient(webRequestHandler, true);
            this.httpClient.Timeout = TimeSpan.FromMilliseconds(timeout);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.ExpectContinue = false;
            Task.Run(() => SendActivitiesTask());
            Debug.WriteLine("Reporter initialized", PxModule.LOG_CATEGORY);
        }

        public bool Post(Activity activity)
        {
            var added = activities.TryAdd(activity);
            Debug.WriteLineIf(!added, "Failed to post activity", PxModule.LOG_CATEGORY);
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
                var jsonContent = JSON.Serialize(activities, jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(postUri, content).Result;
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Failed sending activities (count {0}) - {1}", activities.Count, ex.Message), PxModule.LOG_CATEGORY);
            }
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
            this.activities.Dispose();
        }
    }
}
