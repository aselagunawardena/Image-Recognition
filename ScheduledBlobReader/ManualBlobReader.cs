using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ScheduledBlobReader
{
    public static class ManualBlobReader
    {
        [FunctionName("ManualBlobReader")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Manual execution triggered.");
            
            try
            {
                await ReadBlobsAsync(log);
                return new OkObjectResult("Blob reading completed.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while reading blobs.");
                return new BadRequestObjectResult("An error occurred while reading blobs.");
            }
        }

        private static async Task ReadBlobsAsync(ILogger log)
        {
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            if (string.IsNullOrEmpty(connectionString))
            {
                log.LogError("AzureWebJobsStorage environment variable is not set.");
                throw new InvalidOperationException("AzureWebJobsStorage environment variable is required.");
            }

            string containerName = "media-files";

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                log.LogInformation($"Blob found: {blobItem.Name}");
            }
        }

        // [FunctionName("BlobTriggerFunction")]
        // public static async Task RunBlobTrigger(
        //     [BlobTrigger("media-files/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream,
        //     string name,
        //     ILogger log)
        // {
        //     log.LogInformation($"Blob trigger function executed for blob: {name}");

        //     try
        //     {
        //         if (blobStream == null)
        //         {
        //             log.LogWarning($"Blob stream is null for blob: {name}");
        //             return;
        //         }

        //         using var reader = new StreamReader(blobStream);
        //         string content = await reader.ReadToEndAsync();
        //         log.LogInformation($"Blob content: {content}");
        //     }
        //     catch (Exception ex)
        //     {
        //         log.LogError(ex, $"An error occurred while processing the blob: {name}");
        //     }
        // }
    }
}