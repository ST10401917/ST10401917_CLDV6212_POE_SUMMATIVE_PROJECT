using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureFunctions
{
    public class CustomerEntity : ITableEntity
    {
        // Required by Table Storage
        public string PartitionKey { get; set; } = "CustomerPartition";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Your custom properties
        public string? Customer_Name { get; set; }
        public string? Email { get; set; }
        public string? Phone_Number { get; set; }  // string so leading 0's are preserved
        public string? Address { get; set; }
    }
}
