using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using AzureFunctions.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;


namespace AzureFunctions
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        private readonly TableClient _customerTable;
        private readonly TableClient _productTable;
        private readonly TableClient _orderTable;
        private readonly BlobContainerClient _productBlobContainer;
        private readonly ShareClient _shareClient;

        private readonly string _connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private readonly string _shareName = "contracts";


        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            // Table Storage
            _customerTable = new TableClient(connectionString, "Customer");
            _customerTable.CreateIfNotExists();

            _productTable = new TableClient(connectionString, "Product");
            _productTable.CreateIfNotExists();

            _orderTable = new TableClient(connectionString, "Order");
            _orderTable.CreateIfNotExists();

            _shareClient = new ShareClient(connectionString, "contracts");
            _shareClient.CreateIfNotExists();

            // Blob Storage 
            var blobServiceClient = new BlobServiceClient(connectionString);
            _productBlobContainer = blobServiceClient.GetBlobContainerClient("product-images");
            _productBlobContainer.CreateIfNotExists();
        }

        [Function("AddCustomer")]
        public async Task<HttpResponseData> AddCustomer(
               [HttpTrigger(AuthorizationLevel.Function, "post", Route = "customer")] HttpRequestData req)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                var dto = System.Text.Json.JsonSerializer.Deserialize<CustomerDto>(body);

                if (dto == null)
                {
                    var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResp.WriteAsJsonAsync("Invalid request body");
                    return badResp;
                }

                var entity = new CustomerEntity
                {
                    PartitionKey = "CustomerPartition",
                    RowKey = Guid.NewGuid().ToString(),
                    Customer_Name = dto.Customer_Name,
                    Email = dto.Email,
                    Phone_Number = dto.Phone_Number,
                    Address = dto.Address
                };

                await _customerTable.AddEntityAsync(entity);

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding customer");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync("An error occurred while adding customer");
                return response;
            }
        }

        //  Retrieve all customers from Azure Table 
        [Function("GetCustomers")]
        public async Task<HttpResponseData> GetCustomers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customer")] HttpRequestData req)

        {
            try
            {
                _logger.LogInformation("Fetching all customers.");
                var customers = await _customerTable.QueryAsync<CustomerEntity>().ToListAsync();
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(customers);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync("An error occurred while retrieving customers");
                return response;
            }
        }


        //  Add Product  
        [Function("AddProduct")]
        public async Task<HttpResponseData> AddProduct(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "product")] HttpRequestData req)
        {
            _logger.LogInformation("Processing AddProduct request.");

            var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault();
            if (string.IsNullOrEmpty(contentType) || !contentType.Contains("boundary="))
            {
                var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResp.WriteAsJsonAsync("Missing or invalid Content-Type header.");
                return badResp;
            }

            var boundary = contentType.Split("boundary=")[1];
            var multipartReader = new MultipartReader(boundary, req.Body);
            var section = await multipartReader.ReadNextSectionAsync();

            var product = new ProductEntity();
            string uploadedImageUrl = null;

            while (section != null)
            {
                var contentDisposition = section.Headers["Content-Disposition"].ToString();

                if (contentDisposition.Contains("form-data;"))
                {
                    var name = contentDisposition.Split(';')[1].Split('=')[1].Trim('"');

                    if (name == "ProductName" || name == "Description" || name == "Price")
                    {
                        var value = await new StreamReader(section.Body).ReadToEndAsync();
                        if (name == "ProductName") product.ProductName = value;
                        else if (name == "Description") product.Description = value;
                        else if (name == "Price" && decimal.TryParse(value, out var price)) product.Price = (double)price;
                    }
                    else if (name == "ImageFile")
                    {
                        var fileName = contentDisposition.Split(';')[2].Split('=')[1].Trim('"');
                        var blobClient = _productBlobContainer.GetBlobClient($"{Guid.NewGuid()}-{fileName}");
                        await blobClient.UploadAsync(section.Body, overwrite: true);
                        uploadedImageUrl = blobClient.Uri.ToString();
                    }
                }

                section = await multipartReader.ReadNextSectionAsync();
            }

            if (string.IsNullOrEmpty(product.ProductName))
            {
                var badResp = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResp.WriteAsJsonAsync("Product name is required.");
                return badResp;
            }

            product.PartitionKey = "ProductPartition";
            product.RowKey = Guid.NewGuid().ToString();
            product.ImageUrl = uploadedImageUrl;

            await _productTable.AddEntityAsync(product);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(product);
            return response;
        }



        //Get all Products 
        [Function("GetProducts")]
        public async Task<HttpResponseData> GetProducts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequestData req)
        {
            _logger.LogInformation("Getting all products.");
            var products = await _productTable.QueryAsync<ProductEntity>().ToListAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(products);
            return response;
        }





        // Order trigger
        [Function("ProcessOrderQueue")]
        public void Run(
            [Microsoft.Azure.Functions.Worker.QueueTrigger("order-queue", Connection = "AzureWebJobsStorage")] string myQueueItem,
            FunctionContext context)
        {
            var logger = context.GetLogger("ProcessOrderQueue");
            logger.LogInformation($"Queue item received: {myQueueItem}");

            var order = JsonConvert.DeserializeObject<OrderEntity>(myQueueItem);
            order.PartitionKey = "OrderPartition";
            order.RowKey = Guid.NewGuid().ToString();

            _orderTable.AddEntity(order);


        }

        // file share function
        [Function("UploadContractFile")]
        public async Task<HttpResponseData> UploadContractFile(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "contracts")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);

            var connectionString = Environment.GetEnvironmentVariable("AzureFilesConnectionString");
            var shareName = "contracts";

            // Read file from request
            var boundary = req.Headers.GetValues("Content-Type").FirstOrDefault()?.Split("boundary=")[1];
            if (string.IsNullOrEmpty(boundary))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Missing boundary in Content-Type.");
                return response;
            }

            var reader = new Microsoft.AspNetCore.WebUtilities.MultipartReader(boundary, req.Body);
            var section = await reader.ReadNextSectionAsync();
            if (section == null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("No file found in request.");
                return response;
            }

            var contentDisposition = section.Headers["Content-Disposition"].ToString();
            var fileName = contentDisposition.Split("filename=")[1].Trim('"');

            var shareClient = new ShareClient(connectionString, shareName);
            await shareClient.CreateIfNotExistsAsync();
            var rootDir = shareClient.GetRootDirectoryClient();
            var fileClient = rootDir.GetFileClient(fileName);

            using (var ms = new MemoryStream())
            {
                await section.Body.CopyToAsync(ms);
                ms.Position = 0;
                await fileClient.CreateAsync(ms.Length);
                await fileClient.UploadRangeAsync(new HttpRange(0, ms.Length), ms);
            }

            await response.WriteStringAsync($"File '{fileName}' uploaded successfully!");
            return response;
        }
    }
}
