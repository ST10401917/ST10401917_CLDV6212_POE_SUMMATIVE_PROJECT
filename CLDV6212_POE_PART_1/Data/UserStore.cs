using CLDV6212_POE_PART_1.Models;

namespace CLDV6212_POE_PART_1.Data
{
    public static class UserStore
    {
        public static List<UserModel> Users = new List<UserModel>
        {
            new UserModel { Username="Admin", Password="Admin123", Role="Admin" },
            new UserModel { Username="Customer", Password="Cust123", Role="Customer" }
        };
    }
}
