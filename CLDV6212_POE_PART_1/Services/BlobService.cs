using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace CLDV6212_POE_PART_1.Services
{
    public class BlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerClient = "products";
        public BlobService(string connectionString, string container)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }
        public async Task<string> UploadBlobAsync(Stream fileStream, string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerClient);
            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(fileStream);
            return blobClient.Uri.ToString();
        }
        public async Task DeleteBlobAsync(string blobUri)
        {
            Uri uri = new Uri(blobUri);
            string blobName = string.Join("", uri.Segments.Skip(2));
            // Skip container name too (Segments[0] = "/", Segments[1] = "container/")

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerClient);
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }

    }
}
