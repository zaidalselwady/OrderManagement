using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using global::Order_Management_System.Configuration;
using Microsoft.Extensions.Options;
using Order_Management_System.Services.Interfaces;
using Polly;
using Polly.Retry;


namespace Order_Management_System.Services.Implementations;

    public class SoapClient : ISoapClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SoapClient> _logger;
        private readonly SoapServiceConfiguration _config;
        private readonly AsyncRetryPolicy _retryPolicy;

        public SoapClient(HttpClient httpClient, ILogger<SoapClient> logger, IOptions<SoapServiceConfiguration> config)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));

            _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(_config.RetryAttempts, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry {RetryCount} after {TimeSpan} due to {Exception}",
                            retryCount, timeSpan, exception.Message);
                    });
        }

        public async Task<object> GetPatientsAsync(string query, string password)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Password and query string cannot be empty");
            }

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    var soapEnvelope = CreateSoapEnvelope("getJson_select", password, query);
                    var response = await SendSoapRequestAsync(soapEnvelope, "http://tempuri.org/getJson_select");

                    var result = ExtractSoapResult(response, "getJson_selectResult");

                    if (string.IsNullOrEmpty(result))
                    {
                        throw new InvalidOperationException("No data returned in SOAP response");
                    }

                    var parsedResult = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(result);
                    return parsedResult ?? new List<Dictionary<string, object>>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in GetPatientsAsync for query: {Query}", query);
                    throw;
                }
            });
        }

        public async Task<object> ExecuteQueryAsync(string query, string password)
        {
            return await InsertUpdateAsync(password, query);
        }

        public async Task<object> ExecuteQueryWithParametersAsync(string query, object parameters, string password)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Password and query string cannot be empty");
            }

            string sanitizedQuery = SanitizeQueryWithParameters(query, parameters);

            _logger.LogDebug("Sanitized query: {Query}", sanitizedQuery);

            // Check if the query is a SELECT statement
            if (sanitizedQuery.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                return await GetPatientsAsync(sanitizedQuery, password);
            }
            else if (sanitizedQuery.Contains("SCOPE_IDENTITY"))
            {
                return await HandleInsertWithIdentityAsync(sanitizedQuery, password);
            }
            else
            {
                int rowsAffected = await InsertUpdateAsync(password, sanitizedQuery);
                return new { success = true, rowsAffected = rowsAffected };
            }
        }

        private string SanitizeQueryWithParameters(string query, object parameters)
        {
            string sanitizedQuery = query;

            if (parameters is IDictionary<string, object> paramDict)
            {
                foreach (var param in paramDict)
                {
                    string placeholder = $"@{param.Key}";
                    sanitizedQuery = sanitizedQuery.Replace(placeholder, FormatParameterValue(param.Value));
                }
            }

            return sanitizedQuery;
        }

        private string FormatParameterValue(object value)
        {
            return value switch
            {
                null => "NULL",
                string stringValue => $"N'{stringValue.Replace("'", "''")}'",
                DateTime dateTimeValue => $"'{dateTimeValue:yyyy-MM-dd HH:mm:ss}'",
                bool boolValue => boolValue ? "1" : "0",
                _ => value.ToString()
            };
        }

        private async Task<object> HandleInsertWithIdentityAsync(string sanitizedQuery, string password)
        {
            // Extract the table name and ID column from the query
            string selectIdentityPart = "SELECT SCOPE_IDENTITY() AS ";
            int identityStartIndex = sanitizedQuery.IndexOf(selectIdentityPart) + selectIdentityPart.Length;
            int identityEndIndex = sanitizedQuery.IndexOf(";", identityStartIndex);
            string idColumnName = sanitizedQuery.Substring(identityStartIndex, identityEndIndex - identityStartIndex).Trim();

            // Extract the table name from the INSERT statement
            string insertIntoPart = "INSERT INTO ";
            int tableStartIndex = sanitizedQuery.IndexOf(insertIntoPart) + insertIntoPart.Length;
            int tableEndIndex = sanitizedQuery.IndexOf("(", tableStartIndex);
            string tableName = sanitizedQuery.Substring(tableStartIndex, tableEndIndex - tableStartIndex).Trim();

            string insertQuery = sanitizedQuery.Substring(0, sanitizedQuery.IndexOf("SELECT SCOPE_IDENTITY()"));
            string fetchIdQuery = $"SELECT MAX({idColumnName}) AS {idColumnName} FROM {tableName}";

            // Step 1: Execute the INSERT
            int rowsAffected = await InsertUpdateAsync(password, insertQuery);
            if (rowsAffected <= 0)
            {
                throw new InvalidOperationException("Insert operation failed; no rows affected.");
            }

            // Step 2: Fetch the ID
            var idResult = await GetPatientsAsync(fetchIdQuery, password);

            if (!(idResult is List<Dictionary<string, object>> idList) || !idList.Any())
            {
                throw new InvalidOperationException($"Failed to retrieve {idColumnName} after insert");
            }

            var idValue = idList.FirstOrDefault()?[idColumnName]?.ToString();
            if (string.IsNullOrEmpty(idValue))
            {
                throw new InvalidOperationException($"{idColumnName} is null or empty in SOAP response");
            }

            return new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { { idColumnName, idValue } }
            };
        }

        private async Task<int> InsertUpdateAsync(string password, string sqlStr)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(sqlStr))
            {
                throw new ArgumentException("Password and SQL string cannot be empty");
            }

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    var soapEnvelope = CreateSoapEnvelope("Insert_Update_cmd", password, sqlStr);
                    var response = await SendSoapRequestAsync(soapEnvelope, "http://tempuri.org/Insert_Update_cmd");

                    var result = ExtractSoapResult(response, "Insert_Update_cmdResult");

                    return int.TryParse(result, out int value)
                        ? value
                        : throw new InvalidOperationException($"Invalid response from SOAP service: {result}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in InsertUpdateAsync for SQL: {SqlStr}", sqlStr);
                    throw;
                }
            });
        }

        private XDocument CreateSoapEnvelope(string methodName, string password, string sqlStr)
        {
            XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace tempUriNs = "http://tempuri.org/";

            return new XDocument(
                new XElement(soapNs + "Envelope",
                    new XAttribute(XNamespace.Xmlns + "soap", soapNs),
                    new XElement(soapNs + "Body",
                        new XElement(tempUriNs + methodName,
                            new XElement(tempUriNs + "password", password),
                            new XElement(tempUriNs + "SQlStr", sqlStr)
                        )
                    )
                )
            );
        }

        private async Task<string> SendSoapRequestAsync(XDocument soapEnvelope, string soapAction)
        {
            var requestContent = new StringContent(
                soapEnvelope.ToString(),
                Encoding.UTF8,
                "text/xml");

            var request = new HttpRequestMessage(HttpMethod.Post, "")
            {
                Content = requestContent
            };
            request.Headers.Add("SOAPAction", soapAction);

            _logger.LogDebug("Sending SOAP request for action: {SoapAction}", soapAction);

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("SOAP response received: {Length} bytes", responseContent.Length);

            return responseContent;
        }

        private string ExtractSoapResult(string responseContent, string resultElementName)
        {
            var soapResponse = XDocument.Parse(responseContent);
            XNamespace tempUriNs = "http://tempuri.org/";

            return soapResponse.Descendants(tempUriNs + resultElementName)
                .FirstOrDefault()?.Value ?? string.Empty;
        }
    }
