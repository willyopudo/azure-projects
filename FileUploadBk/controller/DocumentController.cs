using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace FileUploadBk.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly CosmosClient _cosmosClient;
        private readonly string _containerName = Environment.GetEnvironmentVariable("AZURE_BLOB_CONTAINER_NAME");
        private readonly string     _databaseName = Environment.GetEnvironmentVariable("COSMOS_DB_DATABASE_NAME");
        private readonly string     _collectionName = Environment.GetEnvironmentVariable("COSMOS_DB_COLLECTION_NAME");

        public DocumentController(BlobServiceClient blobServiceClient, CosmosClient cosmosClient)
        {
            _blobServiceClient = blobServiceClient;
            _cosmosClient = cosmosClient;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string tags)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is not provided or empty.");
            }

            if (string.IsNullOrEmpty(tags) || tags == "[]")
            {
                // Check if tags are provided and not empty
                return BadRequest("Tags are not provided or invalid.");
            }
            

            List<string> tagList;
            try
            {
                tagList = JsonConvert.DeserializeObject<List<string>>(tags);
            }
            catch (Exception JsonException)
            {
                Console.WriteLine(JsonException.Message);
                return BadRequest("Invalid tags format. Expected a JSON array of strings.");
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

                // Call Azure Function to generate SAS URI
                var httpClient = new HttpClient();
                var functionUrl = Environment.GetEnvironmentVariable("AZURE_FUNCTION_GENERATE_SAS_TOKEN_URL"); // Replace with your Azure Function URL
                var response = await httpClient.GetAsync($"{functionUrl}&blobName={file.FileName}");

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, "Failed to generate SAS URI.");
                }

                var sasResponse = JsonConvert.DeserializeObject<SasResponse>(await response.Content.ReadAsStringAsync());

                // Save metadata to Azure Cosmos DB
                var metadata = new
                {
                    id = Guid.NewGuid().ToString(),
                    fileName = file.FileName,
                    documentType = file.ContentType,
                    blobUrl = sasResponse.downloadUrl, // Use SAS URI as blobUrl
                    expiresAt = sasResponse.expiresAt, // Add expiresAt field
                    uploadedAt = DateTime.UtcNow,
                    tags = tagList,
                };

                var container = _cosmosClient.GetContainer(_databaseName, _collectionName);
                await container.CreateItemAsync(metadata, new PartitionKey(metadata.documentType));

                return Ok(new { Message = "File uploaded successfully", BlobUrl = sasResponse.downloadUrl });
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
                var query = "SELECT c.id, c.fileName, c.blobUrl FROM c";
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
                            Id = item.id,
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

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadFile(string id)
        {
            try
            {
                var container = _cosmosClient.GetContainer(_databaseName, _collectionName);

                // Query Cosmos DB for the document with the given ID
                var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                    .WithParameter("@id", id);

                var iterator = container.GetItemQueryIterator<dynamic>(query);
                var response = await iterator.ReadNextAsync();

                if (response.Count == 0)
                {
                    return NotFound("Document not found.");
                }

                // Extract the document metadata
                var document = response.First();
                var expiresAt = (DateTime)document.expiresAt;

                // Check if the document has expired
                if (DateTime.UtcNow > expiresAt)
                {
                    return BadRequest("The document has expired and is no longer available for download.");
                }

                // Return the blob URL for download
                var blobUrl = (string)document.blobUrl;
                return Ok(new { BlobUrl = blobUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteFile(string id)
        {
            try
            {
                var container = _cosmosClient.GetContainer(_databaseName, _collectionName);

                // Query Cosmos DB for the document with the given ID
                var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                    .WithParameter("@id", id);

                var iterator = container.GetItemQueryIterator<dynamic>(query);
                var response = await iterator.ReadNextAsync();

                if (response.Count == 0)
                {
                    return NotFound("Document not found.");
                }

                // Extract the document metadata
                var document = response.First();
                var blobUrl = (string)document.blobUrl;
                var blobName = new Uri(blobUrl).Segments.Last();

                // Delete the file from Azure Blob Storage
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = blobContainerClient.GetBlobClient(blobName);

                if (await blobClient.ExistsAsync())
                {
                    await blobClient.DeleteAsync();
                }

                // Delete the document from Cosmos DB
                await container.DeleteItemAsync<dynamic>(id, new PartitionKey((string)document.documentType));

                return Ok("File deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        
    }

    public class FileMetadata
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string BlobUrl { get; set; }
    }

    public class SasResponse
    {
        public string downloadUrl { get; set; }
        public DateTime expiresAt { get; set; }
    }
}