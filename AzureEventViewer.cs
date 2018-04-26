using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureEventViewer
{
    public static class AzureEventViewer
    {
        [FunctionName("AzureEventViewer")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            var requestContent = await req.Content.ReadAsStringAsync();

            // Retrieve event type value from the header and return a bad
            // request code if it is missing. 
            if (!req.Headers.TryGetValues("Aeg-Event-Type", out var headerValues))
                return req.CreateResponse(HttpStatusCode.BadRequest, "Missing event type in the header.");

            var eventTypeHeaderValue = headerValues.FirstOrDefault();

            // Echo back the validation code if the event type
            // is for a subscription validation. 
            if (eventTypeHeaderValue == "SubscriptionValidation")
            {
                var events = JsonConvert.DeserializeObject<EventGridEvent[]>(requestContent);
                if (events.FirstOrDefault()?.Data is JObject dataObject)
                {
                    return req.CreateResponse(HttpStatusCode.OK,
                        new {validationResponse = dataObject["validationCode"]});
                }

                req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid request");
            }
            else if (eventTypeHeaderValue == "Notification")
            {
                // Write the event to a storage queue if the event type is
                // an event notification.
                log.Info(requestContent);
                try
                {
                    var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                    var queueClient = storageAccount.CreateCloudQueueClient();
                    var queue = queueClient.GetQueueReference("eventqueue");
                    await queue.CreateIfNotExistsAsync();
                    var message = new CloudQueueMessage(requestContent);
                    await queue.AddMessageAsync(message);
                }
                catch (Exception e)
                {
                    log.Info(e.ToString());
                    return req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error with storage queue");
                }

                return req.CreateResponse(HttpStatusCode.OK);
            }

            // Return a bad request if the event type is missing from the header.
            return req.CreateResponse(HttpStatusCode.BadRequest, "Missing event type in the header.");
        }
    }
}
