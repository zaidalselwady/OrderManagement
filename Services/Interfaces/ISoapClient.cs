namespace Order_Management_System.Services.Interfaces;

public interface ISoapClient
{
    Task<object> GetPatientsAsync(string query, string password);
    Task<object> ExecuteQueryAsync(string query, string password);
    Task<object> ExecuteQueryWithParametersAsync(string query, object parameters, string password);
}

