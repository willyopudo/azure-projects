using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FileUploadBk.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly CosmosClient _cosmosClient;
        private readonly string _containerName = "uploadapp1001";
        private readonly string _databaseName = "DocumentDB";
        private readonly string _collectionName = "documentContainer01";

        public DocumentController(BlobServiceClient blobServiceClient, CosmosClient cosmosClient)
        {
            _blobServiceClient = blobServiceClient;
            _cosmosClient = cosmosClient;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is not provided or empty.");
            }

            try
            {
                // Upload file to Azure Blob Storage
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await blobContainerClient.CreateIfNotExistsAsync();
                var blobClient = blobContainerClient.GetBlobClient(file.FileName);

                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                // Save metadata to Azure Cosmos DB
                var metadata = new
                {
                    id = Guid.NewGuid().ToString(),
                    fileName = file.FileName,
                    documentType = file.ContentType,
                    blobUrl = blobClient.Uri.ToString(),
                    uploadedAt = DateTime.UtcNow
                };

                var container = _cosmosClient.GetContainer(_databaseName, _collectionName);
                await container.CreateItemAsync(metadata, new PartitionKey(metadata.documentType));

                return Ok(new { Message = "File uploaded successfully", BlobUrl = blobClient.Uri.ToString() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("files")]
        public async Task<IActionResult> GetFiles()
        {
            try
            {
                var container = _cosmosClient.GetContainer(_databaseName, _collectionName);
        
                // Query to get all items from the Cosmos DB container
                var query = "SELECT c.fileName, c.blobUrl FROM c";
                var queryDefinition = new QueryDefinition(query);
                var iterator = container.GetItemQueryIterator<dynamic>(queryDefinition);
        
                var files = new List<FileMetadata>();
        
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    foreach (var item in response)
                    {
                        var file = new FileMetadata
                        {
                            FileName = item.fileName,
                            BlobUrl = item.blobUrl
                        };
                        files.Add(file);
                    }
                }
        
                return Ok(files);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    public class FileMetadata
    {
        public string FileName { get; set; }
        public string BlobUrl { get; set; }
    }
}