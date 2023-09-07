namespace BestStoreApi.Models.DTO
{
    public class CartItemDto
    {
        public Product Product { get; set; } = new Product();
        public int Quantity { get; set; }
    }
}
