﻿using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BestStoreApi.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        [Precision(16, 2)]
        public decimal ShippingFee { get; set; }
        [MaxLength(60)]
        public string DeliveryAddress { get; set; } = string.Empty;
        [MaxLength(30)]
        public string PaymentMethod { get; set; } = "";
        [MaxLength(30)]
        public string PaymentStatus { get; set; } = String.Empty;
        [MaxLength(30)]
        public string OrderStatus { get; set; } = String.Empty;

        //navigation properties
        public User User { get; set; } = null!;
        public List<OrderItem> OrderItems { get; set; } = new();


    }
}
