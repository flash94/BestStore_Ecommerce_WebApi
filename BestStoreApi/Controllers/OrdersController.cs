using BestStoreApi.Models;
using BestStoreApi.Models.DTO;
using BestStoreApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BestStoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public OrdersController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [Authorize]
        [HttpGet]
        public IActionResult GetOrders()
        {
            int userId = JwtReader.GetUserId(User);
            string role = context.Users.Find(userId)?.Role ?? ""; //JwtReader.GetUserRole(User);

            IQueryable<Order> query = context.Orders.Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product);

            if (role != "admin")
            {
                query = query.Where(o => o.UserId == userId);
            }

            query = query.OrderByDescending(o => o.Id);

            //read the orders
            var orders = query.ToList();

            foreach(var order in orders)
            {
                // get rid of the object cycle
                foreach (var item in order.OrderItems)
                {
                    item.Order = null;
                }

                order.User.Password = "";
            }

            return Ok(orders);
        }

        [Authorize]
        [HttpPost]
        public IActionResult CreateOrder(OrderDto orderDto)
        {
            //check if the payment method is valid
            if (!OrderHelper.PaymentMethods.ContainsKey(orderDto.PaymentMethod))
            {
                ModelState.AddModelError("Payment Method", "Please select a valid payment method");
                return BadRequest(ModelState);
            }

            int userId = JwtReader.GetUserId(User);
            var user = context.Users.Find(userId);
            if(user == null)
            {
                ModelState.AddModelError("Order", "Unable to create the order");
                return BadRequest(ModelState);

            }

            var productDictionary = OrderHelper.GetProductDictionary(orderDto.ProductIdentifiers);

            //create a new order
            Order order = new Order();
            order.UserId = userId;
            order.CreatedAt = DateTime.Now;
            order.ShippingFee = OrderHelper.ShippingFee;
            order.DeliveryAddress = orderDto.DeliveryAddress;
            order.PaymentMethod = orderDto.PaymentMethod;
            order.PaymentStatus = OrderHelper.PaymentStatuses[0]; //pending
            order.OrderStatus = OrderHelper.OrderStatuses[0]; // created

            foreach (var pair in productDictionary)
            {
                int productId = pair.Key;
                var product = context.Products.Find(productId);
                if(product == null)
                {
                    ModelState.AddModelError("Product", "Product with id" + productId + "is not available");
                    return BadRequest(ModelState);
                }

                var orderItem = new OrderItem();
                orderItem.ProductId = productId;
                orderItem.Quantity = pair.Value;
                orderItem.UnitPrice = product.Price;

                order.OrderItems.Add(orderItem);
            }

            if (order.OrderItems.Count < 1)
            {
                ModelState.AddModelError("Order", "Unable to create order");  
                return BadRequest(ModelState);
            }

            //save the order in the database
            context.Orders.Add(order);
            context.SaveChanges();

            // get rid of the object cycle
            foreach (var item in order.OrderItems)
            {
                item.Order = null;
            }

            //hide the user password
            order.User.Password = "";
            return Ok(order);
            

        }
    }
}
