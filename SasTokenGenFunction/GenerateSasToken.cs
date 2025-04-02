using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace SasTokenGenFunction
{
    public static class GenerateSasToken
    {
        private static string StorageAccountName = Environment.GetEnvironmentVariable("StorageAccountName");
        private static string KeyVaultUri = Environment.GetEnvironmentVariable("KeyVaultUri"); // Add Key Vault URI to app settings
        private static string ContainerName = Environment.GetEnvironmentVariable("ContainerName");

        [FunctionName("GenerateSasToken")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string blobName = req.Query["blobName"];
            if (string.IsNullOrEmpty(blobName))
            {
                return new BadRequestObjectResult("Please provide a blob name.");
            }

            // Retrieve the Storage Account Key from Key Vault
            var client = new SecretClient(new Uri(KeyVaultUri), new DefaultAzureCredential());
            KeyVaultSecret secret = await client.GetSecretAsync("uploadStorageAccountKey");
            string storageAccountKey = secret.Value;

            var blobServiceClient = new BlobServiceClient(
                new Uri($"https://{StorageAccountName}.blob.core.windows.net"),
                new StorageSharedKeyCredential(StorageAccountName, storageAccountKey)
            );

            var blobClient = blobServiceClient.GetBlobContainerClient(ContainerName).GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                return new NotFoundObjectResult("Blob not found.");
            }

            // Generate SAS token
            TimeSpan expiry = TimeSpan.FromHours(2); // Set the expiry time for the SAS token
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = ContainerName,
                Resource = "b",
                StartsOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow.AddHours(2), // 2-hour expiry
                Protocol = SasProtocol.Https
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Generate the SAS token
            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);

            var result = new
            {
                downloadUrl = sasUri.ToString(),
                expiresAt = sasBuilder.ExpiresOn
            };

            return new OkObjectResult(result);
        }
    }
}
