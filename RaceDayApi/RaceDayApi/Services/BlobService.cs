using Azure.Storage.Blobs;

namespace RaceDayApi.Services
{
    public class BlobService : IBlobService
    {
        private readonly string _connectionString;
        private readonly string _containerName = "event-banners";

        public BlobService(IConfiguration configuration)
        {
            _connectionString = configuration["AzureBlobStorage"];
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return string.Empty;

            // Connect to the Azure Blob Storage (or Azurite emulator)
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            // Create the container if it doesn't exist, and make it public so we can view the images
            await blobContainerClient.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

            // Create a unique name for the file
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = blobContainerClient.GetBlobClient(fileName);

            // Upload the file
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            // Return the URL of the uploaded image
            return blobClient.Uri.ToString();
        }
    }
}