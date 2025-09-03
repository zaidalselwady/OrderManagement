using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Order_Management_System.Models.Domain
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }

        public int OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public int CustomerId { get; set; }

        [MaxLength(100)]
        public string? CustomerName { get; set; }

        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? DeliveryTerms { get; set; }

        [MaxLength(500)]
        public string? PaymentTerms { get; set; }

        public OrderStatus Status { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public int? SalesmanId { get; set; }
        public int? UserId { get; set; }

        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }

    public enum OrderStatus
    {
        Pending = 0,
        Shipped = 1,
        Delivered = 2
    }
}
