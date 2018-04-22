using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;

namespace AzureEventViewer
{
    public static class AzureEventViewer
    {
        [FunctionName("AzureEventViewer")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            string requestContent = await req.Content.ReadAsStringAsync();

            //validate subscription if this is a validation request
            if (req.Headers.TryGetValues("Aeg-Event-Type", out IEnumerable<string> headerValues))
            {
                if (headerValues.FirstOrDefault<string>() == "SubscriptionValidation")
                {
                    EventGridEvent[] events = JsonConvert.DeserializeObject<EventGridEvent[]>(requestContent);
                    JObject dataObject = events.FirstOrDefault().Data as JObject;
                    return req.CreateResponse(HttpStatusCode.OK, new { validationResponse = dataObject["validationCode"] });
                }
            }

            log.Info(requestContent);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("EventQueue");
            queue.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage(requestContent);
            await queue.AddMessageAsync(message);

            return req.CreateResponse(HttpStatusCode.OK, requestContent);
        }
    }
}
