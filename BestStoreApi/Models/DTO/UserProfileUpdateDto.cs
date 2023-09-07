namespace BestStoreApi.Models.DTO
{
    public class UserProfileUpdateDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; } // unique in db
        public string Phone { get; set; }
        public string Address { get; set; }
    }
}
