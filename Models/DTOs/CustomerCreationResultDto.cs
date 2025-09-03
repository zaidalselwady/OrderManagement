namespace Order_Management_System.Models.DTOs
{
   

   

    public class CustomerCreationResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int? CustomerId { get; set; }
    }
}