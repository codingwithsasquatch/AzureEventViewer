using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureEventViewer
{
    public static class AzureEventViewer
    {
        [FunctionName("AzureEventViewer")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            string requestContent = await req.Content.ReadAsStringAsync();

            //validate subscription if this is a validation request
            if (req.Headers.TryGetValues("Aeg-Event-Type", out IEnumerable<string> headerValues))
            {
                string validationHeaderValue = headerValues.FirstOrDefault<string>();
                if (validationHeaderValue == "SubscriptionValidation")
                {
                    EventGridEvent[] events = JsonConvert.DeserializeObject<EventGridEvent[]>(requestContent);
                    dynamic dataObject = events.FirstOrDefault().Data as dynamic;
                    return req.CreateResponse(HttpStatusCode.OK, new { validationResponse = dataObject.validationCode });
                }
            }

            log.Info(requestContent);
            return req.CreateResponse(HttpStatusCode.OK, requestContent);
        }
    }
}
