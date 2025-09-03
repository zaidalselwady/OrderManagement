namespace Order_Management_System.Models.DTOs
{

    public class OrderCreationResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int? OrderId { get; set; }
        public int? OrderNumber { get; set; }
    }

    
}