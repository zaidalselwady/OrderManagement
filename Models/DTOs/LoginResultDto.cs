namespace Order_Management_System.Models.DTOs
{
    public class LoginResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorType { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public int? CompanyId { get; set; }
    }


}