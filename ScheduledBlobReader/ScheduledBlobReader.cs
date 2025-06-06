using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace ScheduledBlobReader
{
    public class ScheduledBlobReader
    {
        [FunctionName("ScheduledBlobReader")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Scheduled execution at: {DateTime.Now}");
            await ReadBlobsAsync(log);
        }
        
        private async Task ReadBlobsAsync(ILogger log)
        {
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string containerName = "media-files";

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                log.LogInformation($"Blob found: {blobItem.Name}");
            }
        }
    }
}