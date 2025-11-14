using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using CLDV6212_POE_PART_1.Models;

namespace CLDV6212_POE_PART_1.Services
{
    public class AzureFileShareService
    {
        private readonly string _connectionString;
        private readonly string _fileShareName;
        public AzureFileShareService(string connectionString, string fileShareName)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _fileShareName = fileShareName ?? throw new ArgumentNullException(nameof(fileShareName));
        }

        public async Task UploadFileAsync(string directoryName, Stream fileStream, string fileName)
        {
            try
            {
                var shareClient = new ShareClient(_connectionString, _fileShareName);
                await shareClient.CreateIfNotExistsAsync(); // ensure file share exists

                // Use the correct directory
                var directoryClient = shareClient.GetDirectoryClient(directoryName);
                await directoryClient.CreateIfNotExistsAsync(); // ensure folder exists

                var fileClient = directoryClient.GetFileClient(fileName);

                await fileClient.CreateAsync(fileStream.Length);
                await fileClient.UploadRangeAsync(new HttpRange(0, fileStream.Length), fileStream);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading file: {ex.Message}", ex);
            }
        }

        public async Task<List<FileModel>> ListFilesAsync(string directoryName)
        {
            var files = new List<FileModel>();
            var shareClient = new ShareClient(_connectionString, _fileShareName);
            await shareClient.CreateIfNotExistsAsync(); // ensure share exists

            var directoryClient = shareClient.GetDirectoryClient(directoryName);
            await directoryClient.CreateIfNotExistsAsync(); // ensure folder exists

            await foreach (var item in directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory)
                {
                    var fileClient = directoryClient.GetFileClient(item.Name);
                    var properties = await fileClient.GetPropertiesAsync();

                    files.Add(new FileModel
                    {
                        Name = item.Name,
                        Size = properties.Value.ContentLength,
                        lastModified = properties.Value.LastModified
                    });
                }
            }

            return files;
        }


    }   
}
