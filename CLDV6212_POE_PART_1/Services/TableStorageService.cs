using Azure;
using Azure.Data.Tables;
using CLDV6212_POE_PART_1.Models;

namespace CLDV6212_POE_PART_1.Services
{
    public class TableStorageService
    {
        private readonly TableClient _customerTableClient; // for the customer table
        private readonly TableClient _productTableClient; // for the product table
        private readonly TableClient _orderTableClient; // for the order table


        public TableStorageService(string connectionString)
        {
            // Initialize the TableClient for each table
            _customerTableClient = new TableClient(connectionString, "Customer");
            _productTableClient = new TableClient(connectionString, "Product");
            _orderTableClient = new TableClient(connectionString, "Order");
        }

        // all the customer methods

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var customers = new List<Customer>();

            await foreach (var customer in _customerTableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }

            return customers;
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            if (string.IsNullOrWhiteSpace(customer.PartitionKey) || string.IsNullOrWhiteSpace(customer.RowKey))
            {
                throw new ArgumentException("PartitionKey and RowKey must be set");
            }

            try
            {
                await _customerTableClient.AddEntityAsync(customer);
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Error adding entity to Table Storage", ex);
            }
        }

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            await _customerTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // The Product methods

        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();
            await foreach (var product in _productTableClient.QueryAsync<Product>())
            {
                products.Add(product);
            }
            return products;
        }

        public async Task AddProductAsync(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.PartitionKey) || string.IsNullOrWhiteSpace(product.RowKey))
            {
                throw new ArgumentException("PartitionKey and RowKey must be set");
            }
            try
            {
                await _productTableClient.AddEntityAsync(product);
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Error adding entity to Table Storage", ex);
            }
        }

        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            await _productTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }


        // All the order methods

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var orders = new List<Order>();
            await foreach (var order in _orderTableClient.QueryAsync<Order>())
            {
                orders.Add(order);
            }
            return orders;
        }

        public async Task AddOrderAsync(Order order)
        {
            if (string.IsNullOrWhiteSpace(order.PartitionKey) || string.IsNullOrWhiteSpace(order.RowKey))
            {
                throw new ArgumentException("PartitionKey and RowKey must be set");
            }


            try
            {
                await _orderTableClient.AddEntityAsync(order);
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Error adding entity to Table Storage", ex);
            }
        }

        public async Task UpdateOrderAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (string.IsNullOrWhiteSpace(order.PartitionKey) || string.IsNullOrWhiteSpace(order.RowKey))
                throw new ArgumentException("PartitionKey and RowKey must be set.");

            try
            {
                await _orderTableClient.UpdateEntityAsync(order, ETag.All, TableUpdateMode.Replace);
                Console.WriteLine($" Order {order.RowKey} updated successfully with status: {order.Status}");
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($" Azure update failed: {ex.Message}");
                throw;
            }
        }


        public async Task<Order?> GetOrderAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _orderTableClient.GetEntityAsync<Order>(partitionKey, rowKey);
                return response.Value;
            }
            catch
            {
                return null;
            }
        }



        public async Task DeleteOrderAsync(string partitionKey, string rowKey)
        {
            await _orderTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }


    }
}
