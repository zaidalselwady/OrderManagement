namespace Order_Management_System.Configuration
{
    public class SoapServiceConfiguration
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string Password { get; set; } = "OptimalPass_optimaljo05";
        public int RetryAttempts { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
    }
}