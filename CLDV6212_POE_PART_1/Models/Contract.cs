namespace CLDV6212_POE_PART_1.Models
{
    public class Contract
    {
        public string? Name { get; set; }
        public long Size { get; set; }

        public DateTimeOffset lastModified { get; set; }

        public string DisplaySize
        {
            get
            {
                if (Size >= 1024 * 1024)
                    return $"{Size / 1024 / 1024} MB";
                if (Size >= 1024) 
                    return $"{Size / 1024} KB";
                return $"{Size} Bytes";
            }
        }
    }
}
