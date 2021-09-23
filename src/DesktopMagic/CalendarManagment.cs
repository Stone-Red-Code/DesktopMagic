using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace DesktopMagic
{
    internal class CalendarManagment
    {
        private static string[] Scopes = { CalendarService.Scope.CalendarReadonly };

        private static string ApplicationName = "Google Calendar API .NET " + App.AppName;

        [Obsolete]
        public (List<string>, List<string>) GetEvents()
        {
            List<string> upcomingEventNames = new List<string>();
            List<string> upcomingEventTimes = new List<string>();
            UserCredential credential;

            if (!File.Exists("credentials.json"))
            {
                return (new(), new());
            }

            using (FileStream stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Debug.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Calendar API service.
            CalendarService service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define parameters of request.
            EventsResource.ListRequest request = service.Events.List("primary");
            request.TimeMin = DateTime.Now;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 10;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // List events.
            Events events = request.Execute();
            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (Event eventItem in events.Items)
                {
                    if (upcomingEventNames.Count < 10)
                    {
                        string when = eventItem.Start.DateTime.ToString();
                        if (string.IsNullOrEmpty(when))
                        {
                            when = eventItem.Start.Date;
                        }
                        upcomingEventNames.Add(eventItem.Summary);
                        upcomingEventTimes.Add(when);
                    }
                }
            }
            return (upcomingEventNames, upcomingEventTimes);
        }
    }

    internal class CalendarItems
    {
        public string eventname { get; set; }
        public string dateTime { get; set; }
        public string font { get; set; }
        public string color { get; set; }
    }
}