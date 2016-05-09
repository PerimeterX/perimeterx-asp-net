using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PerimeterX
{
    class PxModuleReporter
    {
        private readonly BlockingCollection<Activity> activities;
        private readonly int bulkSize;
        private readonly HttpClient httpClient;
        private readonly string postUri;

        private static volatile PxModuleReporter instance;
        private static object syncInstance = new Object();

        private PxModuleReporter(PxModuleConfigurationSection config)
        {
            this.activities = new BlockingCollection<Activity>(config.ActivitiesCapacity);
            this.bulkSize = config.ActivitiesBulkSize;
            this.postUri = config.BaseUri + @"/api/v1/collector/s2s";
            this.httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(5),
                MaxResponseContentBufferSize = 1024 * 1024 * 10
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PerimeterX middleware");
            httpClient.DefaultRequestHeaders.ExpectContinue = false;
            Task.Run(() => SendActivitiesTask());
        }

        public static PxModuleReporter Instance(PxModuleConfigurationSection config)
        {
            if (instance == null)
            {
                lock (syncInstance)
                {
                    if (instance == null)
                    {
                        instance = new PxModuleReporter(config);
                    }
                }
            }
            return instance;
        }

        public void Post(Activity activity)
        {
            activities.Add(activity);
        }

        private void SendActivitiesTask()
        {
            var lastSent = DateTime.UtcNow;
            var bulk = new List<Activity>(this.bulkSize);
            var sendActivities = false;
            while (!activities.IsCompleted)
            {
                Activity activity;
                if (activities.TryTake(out activity, 1000))
                {
                    bulk.Add(activity);
                    if (lastSent.AddSeconds(1) > DateTime.UtcNow)
                    {
                        sendActivities = true;
                    }
                }
                else
                {
                    sendActivities = bulk.Count > 0;
                }

                if (sendActivities)
                {
                    sendActivities = false;
                    lastSent = DateTime.UtcNow;
                    SendActivities(bulk);
                    bulk.Clear();
                }
            }
        }

        private void SendActivities(List<Activity> activities)
        {
            try
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, postUri)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(activities), Encoding.UTF8, "application/json")
                };
                var response = httpClient.SendAsync(requestMessage).Result;
                response.EnsureSuccessStatusCode();
                Debug.WriteLine("Sent {0} activitie(s)", activities.Count);
            }
            catch (Exception ex)
            {
                PxModuleEventSource.Log.FailedPostActivities(activities.Count, ex.Message);
                Debug.WriteLine("Failed to send activities (count {0})", activities.Count);
            }
        }
    }
}
