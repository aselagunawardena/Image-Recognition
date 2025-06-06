using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic; // Added for IEnumerable<>
using System.Linq; // Added for LINQ methods like Select
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class BlobTriggerFunction
{
    private static readonly HttpClient httpClient = new HttpClient();

    [FunctionName("MediaBlobTriggerFunction")]
    public async Task Run(
        [BlobTrigger("media-files/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream,
        string name,
        ILogger log)
    {
        log.LogInformation($"Processing blob\n Name: {name} \n Size: {blobStream.Length} Bytes");

        try
        {
            if (name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessImage(blobStream, name, log);
            }
            else if (name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                     name.EndsWith(".mov", StringComparison.OrdinalIgnoreCase))
            {
                log.LogInformation("Video processing placeholder.");
                // You can add Video Indexer integration here
            }
            else
            {
                log.LogInformation("Unsupported file type.");
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"An error occurred while processing the blob: {name}");
        }
    }

    private async Task ProcessImage(Stream imageStream, string name, ILogger log)
    {
        string visionEndpoint = Environment.GetEnvironmentVariable("VISION_ENDPOINT");
        string visionKey = Environment.GetEnvironmentVariable("VISION_KEY");

        if (string.IsNullOrEmpty(visionEndpoint) || string.IsNullOrEmpty(visionKey))
        {
            log.LogError("VISION_ENDPOINT or VISION_KEY environment variables are not set.");
            throw new InvalidOperationException("VISION_ENDPOINT and VISION_KEY environment variables are required.");
        }

        var requestUrl = $"{visionEndpoint}/vision/v3.2/analyze?visualFeatures=Tags,Description,Faces,Objects";

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", visionKey);

        try
        {
            // Reset the stream position to ensure it can be read
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }

            using var content = new StreamContent(imageStream);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            var response = await httpClient.PostAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                dynamic metadata = JsonConvert.DeserializeObject(responseContent);
                log.LogInformation($"Image metadata for {name}: {JsonConvert.SerializeObject(metadata, Formatting.Indented)}");

                // TODO: Save metadata to Azure Search or Cosmos DB
                await SaveToAzureSearch(metadata, name, log);
                log.LogInformation($"Metadata for {name} saved successfully.");

            }
            else
            {
                log.LogError($"Vision API error for {name}: {responseContent}");
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, $"An error occurred while calling the Vision API for {name}");
        }
    }

    private async Task SaveToAzureSearch(dynamic metadata, string name, ILogger log)
    {
        string searchServiceName = Environment.GetEnvironmentVariable("SEARCH_SERVICE_NAME");
        string searchApiKey = Environment.GetEnvironmentVariable("SEARCH_API_KEY");
        string indexName = Environment.GetEnvironmentVariable("SEARCH_INDEX_NAME");
        string storageAccountName = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_NAME");
        string storageAccountKey = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_KEY");
        string containerName = "media-files";

        if (string.IsNullOrEmpty(searchServiceName) || string.IsNullOrEmpty(searchApiKey) ||
            string.IsNullOrEmpty(indexName) || string.IsNullOrEmpty(storageAccountName) ||
            string.IsNullOrEmpty(storageAccountKey))
        {
            throw new InvalidOperationException("One or more required environment variables are not set.");
        }

        // Generate SAS token for the blob
        //string sasToken = GenerateSasToken(storageAccountName, storageAccountKey, containerName, name);

        // Cast metadata.tags to a strongly typed collection
        var tags = metadata.tags != null
            ? ((IEnumerable<dynamic>)metadata.tags).Select(t => (string)t.name).ToArray()
            : null;

        var searchUrl = $"https://{searchServiceName}.search.windows.net/indexes/{indexName}/docs/index?api-version=2023-07-01-Preview";

        var document = new
        {
            value = new[] {
                new {
                    //@searchAction = "upload",
                    id = Guid.NewGuid().ToString(),
                    fileName = name,
                    tags = tags,
                    description = metadata.description?.captions?[0]?.text,
                    url = $"https://{storageAccountName}.blob.core.windows.net/{containerName}/{name}"
                    //url = $"https://{storageAccountName}.blob.core.windows.net/{containerName}/{name}?{sasToken}"
                }
            }
        };

        var json = JsonConvert.SerializeObject(document);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("api-key", searchApiKey);

        var response = await httpClient.PostAsync(searchUrl, content);
        var result = await response.Content.ReadAsStringAsync();

        log.LogInformation($"Azure Search Response: {result}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to index document: {result}");
        }
    }

    private string GenerateSasToken(string accountName, string accountKey, string containerName, string blobName)
    {
        var sasBuilder = new Azure.Storage.Sas.BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b", // Indicates the resource is a blob
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) // SAS token valid for 1 hour
        };

        // Set permissions for the SAS token
        sasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Read);

        // Create the storage shared key credential
        var storageSharedKeyCredential = new Azure.Storage.StorageSharedKeyCredential(accountName, accountKey);

        // Generate the SAS token
        return sasBuilder.ToSasQueryParameters(storageSharedKeyCredential).ToString();
    }

}