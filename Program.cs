using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Order_Management_System.Services.Interfaces;
using Order_Management_System.Services.Implementations;
using Order_Management_System.Repositories.Interfaces;
using Order_Management_System.Repositories.Implementations;
using Order_Management_System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add this line to debug
DebugConfiguration(builder);

// Add services to the container
builder.Services.AddControllersWithViews();

// Session configuration
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Get connection string and verify it exists
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");
}

Console.WriteLine($"📝 Using connection string: {MaskPassword(connectionString)}");

// CRITICAL FIX: Add Entity Framework with proper scoped lifetime and explicit connection string
builder.Services.AddDbContext<OrderManagementDbContext>((serviceProvider, options) =>
{
    // Get the connection string again inside the factory to ensure it's always available
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var connStr = config.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connStr))
    {
        throw new InvalidOperationException("Connection string not found when creating DbContext");
    }

    options.UseSqlServer(connStr, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(120); // 2 minutes timeout
    });

    // Only enable in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
        options.LogTo(Console.WriteLine, LogLevel.Information);
    }
}, ServiceLifetime.Scoped); // Explicitly set scoped lifetime

// Register services with proper lifetimes
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Register repositories with proper lifetimes
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    if (builder.Environment.IsDevelopment())
    {
        logging.SetMinimumLevel(LogLevel.Debug);
    }
});

var app = builder.Build();

// Test database connection on startup with proper service scope
await TestDatabaseConnection(app.Services);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Configure for subfolder deployment if needed
var pathBase = builder.Configuration["PathBase"];
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Helper methods
static string MaskPassword(string connectionString)
{
    if (string.IsNullOrEmpty(connectionString)) return connectionString;

    try
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.Password))
        {
            builder.Password = "****";
        }
        return builder.ToString();
    }
    catch
    {
        return connectionString.Replace("sa@2005", "****");
    }
}

static async Task TestDatabaseConnection(IServiceProvider services)
{
    try
    {
        Console.WriteLine("🔍 Testing database connection...");

        // Create a scope to test the DbContext
        using var scope = services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        var configuration = scopedServices.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        Console.WriteLine($"📝 Connection String: {MaskPassword(connectionString)}");

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("❌ Connection string is null or empty!");
            return;
        }

        // Test raw connection first
        Console.WriteLine("🔌 Testing raw SQL connection...");
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            Console.WriteLine("✅ Raw SQL connection successful!");

            using (var command = new SqlCommand("SELECT @@SERVERNAME as ServerName, DB_NAME() as DatabaseName", connection))
            {
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    Console.WriteLine($"🖥️  Server: {reader["ServerName"]}");
                    Console.WriteLine($"💾 Database: {reader["DatabaseName"]}");
                }
            }
        }

        // Test Entity Framework connection with proper scoped context
        Console.WriteLine("🧩 Testing Entity Framework connection...");
        var context = scopedServices.GetRequiredService<OrderManagementDbContext>();

        // Test if we can connect
        var canConnect = await context.Database.CanConnectAsync();
        if (canConnect)
        {
            Console.WriteLine("✅ Entity Framework connection successful!");

            // Test counting records with error handling
            try
            {
                var userCount = await context.Users.CountAsync();
                var customerCount = await context.Customers.CountAsync();
                var orderCount = await context.Orders.CountAsync();

                Console.WriteLine($"📊 Found {userCount} users, {customerCount} customers, {orderCount} orders");
            }
            catch (Exception countEx)
            {
                Console.WriteLine($"⚠️  EF counting failed: {countEx.Message}");

                // Try a simpler query
                try
                {
                    await context.Database.ExecuteSqlRawAsync("SELECT 1");
                    Console.WriteLine("✅ Basic EF query successful");
                }
                catch (Exception basicEx)
                {
                    Console.WriteLine($"❌ Basic EF query failed: {basicEx.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine("❌ Entity Framework cannot connect");
        }

    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database connection test failed: {ex.Message}");
        Console.WriteLine($"🔍 Exception Type: {ex.GetType().Name}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"🔗 Inner Exception: {ex.InnerException.Message}");
        }

        // Don't throw here - let the app start but log the error
        Console.WriteLine("⚠️  Application will start but database operations may fail");
    }
}

// Add this method to Program.cs to debug configuration
static void DebugConfiguration(WebApplicationBuilder builder)
{
    Console.WriteLine("🔍 Debugging Configuration...");

    var config = builder.Configuration;

    Console.WriteLine($"📁 Current Directory: {Directory.GetCurrentDirectory()}");

    // Check if appsettings.json exists
    var appsettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
    Console.WriteLine($"📄 appsettings.json exists: {File.Exists(appsettingsPath)}");

    if (File.Exists(appsettingsPath))
    {
        var content = File.ReadAllText(appsettingsPath);
        Console.WriteLine($"📄 appsettings.json content (first 200 chars): {content.Substring(0, Math.Min(200, content.Length))}...");
    }

    // Check configuration sources
    Console.WriteLine("📚 Configuration Sources:");
    foreach (var source in config.Sources)
    {
        Console.WriteLine($"  - {source.GetType().Name}");
    }

    // Try to get connection string
    var connectionString = config.GetConnectionString("DefaultConnection");
    Console.WriteLine($"🔗 Connection string found: {!string.IsNullOrEmpty(connectionString)}");
    if (!string.IsNullOrEmpty(connectionString))
    {
        Console.WriteLine($"🔗 Connection string: {MaskPassword(connectionString)}");
    }

    // List all configuration keys
    Console.WriteLine("🔑 All configuration keys:");
    foreach (var kvp in config.AsEnumerable())
    {
        if (kvp.Key.Contains("Password"))
        {
            Console.WriteLine($"  {kvp.Key}: ****");
        }
        else
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }
    }
}














//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Data.SqlClient;
//using Order_Management_System.Services.Interfaces;
//using Order_Management_System.Services.Implementations;
//using Order_Management_System.Repositories.Interfaces;
//using Order_Management_System.Repositories.Implementations;
//using Order_Management_System.Data;

//var builder = WebApplication.CreateBuilder(args);


//// Add this line to debug
//DebugConfiguration(builder);


//// Add services to the container
//builder.Services.AddControllersWithViews();

//// Session configuration
//builder.Services.AddDistributedMemoryCache();
//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromMinutes(30);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;
//});

//// Get connection string and verify it exists
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//if (string.IsNullOrEmpty(connectionString))
//{
//    throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");
//}

//Console.WriteLine($"📝 Using connection string: {MaskPassword(connectionString)}");

//// Add Entity Framework with explicit connection string
//builder.Services.AddDbContext<OrderManagementDbContext>(options =>
//{
//    options.UseSqlServer(connectionString);
//    options.EnableSensitiveDataLogging(); // For debugging - remove in production
//    options.LogTo(Console.WriteLine, LogLevel.Information); // For debugging - remove in production
//});

//// Register services
//builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
//builder.Services.AddScoped<IOrderService, OrderService>();
//builder.Services.AddScoped<ICustomerService, CustomerService>();
//builder.Services.AddScoped<IDashboardService, DashboardService>();

//// Register repositories
//builder.Services.AddScoped<IUserRepository, UserRepository>();
//builder.Services.AddScoped<IOrderRepository, OrderRepository>();
//builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
//builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();

//var app = builder.Build();

//// Test database connection on startup
//await TestDatabaseConnection(app.Services);

//// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
//    app.UseDeveloperExceptionPage();
//}
//else
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

//// Configure for subfolder deployment if needed
//var pathBase = builder.Configuration["PathBase"];
//if (!string.IsNullOrEmpty(pathBase))
//{
//    app.UsePathBase(pathBase);
//}

//app.UseStaticFiles();
//app.UseRouting();
//app.UseSession();
//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.Run();

//// Helper methods
//static string MaskPassword(string connectionString)
//{
//    if (string.IsNullOrEmpty(connectionString)) return connectionString;

//    try
//    {
//        var builder = new SqlConnectionStringBuilder(connectionString);
//        if (!string.IsNullOrEmpty(builder.Password))
//        {
//            builder.Password = "****";
//        }
//        return builder.ToString();
//    }
//    catch
//    {
//        return connectionString.Replace("sa@2005", "****");
//    }
//}

//static async Task TestDatabaseConnection(IServiceProvider services)
//{
//    try
//    {
//        using var scope = services.CreateScope();
//        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
//        var connectionString = configuration.GetConnectionString("DefaultConnection");

//        Console.WriteLine("🔍 Testing database connection...");
//        Console.WriteLine($"📝 Connection String: {MaskPassword(connectionString)}");

//        if (string.IsNullOrEmpty(connectionString))
//        {
//            Console.WriteLine("❌ Connection string is null or empty!");
//            return;
//        }

//        // Test raw connection first
//        using (var connection = new SqlConnection(connectionString))
//        {
//            Console.WriteLine("🔌 Testing raw SQL connection...");
//            await connection.OpenAsync();
//            Console.WriteLine("✅ Raw SQL connection successful!");

//            using (var command = new SqlCommand("SELECT @@SERVERNAME as ServerName, DB_NAME() as DatabaseName", connection))
//            {
//                using var reader = await command.ExecuteReaderAsync();
//                if (await reader.ReadAsync())
//                {
//                    Console.WriteLine($"🖥️  Server: {reader["ServerName"]}");
//                    Console.WriteLine($"💾 Database: {reader["DatabaseName"]}");
//                }
//            }
//        }

//        // Test Entity Framework connection
//        var context = scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>();
//        var canConnect = await context.Database.CanConnectAsync();

//        if (canConnect)
//        {
//            Console.WriteLine("✅ Entity Framework connection successful!");

//            // Test counting records
//            try
//            {
//                var userCount = await context.Users.CountAsync();
//                var customerCount = await context.Customers.CountAsync();
//                var orderCount = await context.Orders.CountAsync();

//                Console.WriteLine($"📊 Found {userCount} users, {customerCount} customers, {orderCount} orders");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"⚠️  EF counting failed: {ex.Message}");
//            }
//        }
//        else
//        {
//            Console.WriteLine("❌ Entity Framework cannot connect");
//        }

//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"❌ Database connection test failed: {ex.Message}");
//        Console.WriteLine($"🔍 Exception Type: {ex.GetType().Name}");
//        if (ex.InnerException != null)
//        {
//            Console.WriteLine($"🔗 Inner Exception: {ex.InnerException.Message}");
//        }
//    }
//}

//// Add this method to Program.cs to debug configuration
//static void DebugConfiguration(WebApplicationBuilder builder)
//{
//    Console.WriteLine("🔍 Debugging Configuration...");

//    var config = builder.Configuration;

//    Console.WriteLine($"📁 Current Directory: {Directory.GetCurrentDirectory()}");

//    // Check if appsettings.json exists
//    var appsettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
//    Console.WriteLine($"📄 appsettings.json exists: {File.Exists(appsettingsPath)}");

//    if (File.Exists(appsettingsPath))
//    {
//        var content = File.ReadAllText(appsettingsPath);
//        Console.WriteLine($"📄 appsettings.json content (first 200 chars): {content.Substring(0, Math.Min(200, content.Length))}...");
//    }

//    // Check configuration sources
//    Console.WriteLine("📚 Configuration Sources:");
//    foreach (var source in config.Sources)
//    {
//        Console.WriteLine($"  - {source.GetType().Name}");
//    }

//    // Try to get connection string
//    var connectionString = config.GetConnectionString("DefaultConnection");
//    Console.WriteLine($"🔗 Connection string found: {!string.IsNullOrEmpty(connectionString)}");
//    if (!string.IsNullOrEmpty(connectionString))
//    {
//        Console.WriteLine($"🔗 Connection string: {MaskPassword(connectionString)}");
//    }

//    // List all configuration keys
//    Console.WriteLine("🔑 All configuration keys:");
//    foreach (var kvp in config.AsEnumerable())
//    {
//        if (kvp.Key.Contains("Password"))
//        {
//            Console.WriteLine($"  {kvp.Key}: ****");
//        }
//        else
//        {
//            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
//        }
//    }
//}











//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Order_Management_System.Services.Interfaces;
//using Order_Management_System.Services.Implementations;
//using Order_Management_System.Repositories.Interfaces;
//using Order_Management_System.Repositories.Implementations;
//using Order_Management_System.Configuration;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container
//builder.Services.AddControllersWithViews();

//// Session configuration
//builder.Services.AddDistributedMemoryCache();
//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromMinutes(30);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;
//});

//// Configuration
//builder.Services.Configure<SoapServiceConfiguration>(
//    builder.Configuration.GetSection("SoapService"));

//// Register HttpClient
//builder.Services.AddHttpClient<ISoapClient, SoapClient>(client =>
//{
//    var soapConfig = builder.Configuration.GetSection("SoapService").Get<SoapServiceConfiguration>();
//    client.BaseAddress = new Uri(soapConfig.BaseUrl);
//    client.DefaultRequestHeaders.Add("Accept", "text/xml");
//});

//// Register services
//builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
//builder.Services.AddScoped<IOrderService, OrderService>();
//builder.Services.AddScoped<ICustomerService, CustomerService>();
//builder.Services.AddScoped<IDashboardService, DashboardService>(); // Add this line

//// Register repositories
//builder.Services.AddScoped<IUserRepository, UserRepository>();
//builder.Services.AddScoped<IOrderRepository, OrderRepository>();
//builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
//builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();

//var app = builder.Build();

//// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
//    app.UseDeveloperExceptionPage();
//}
//else
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

//// Configure for subfolder deployment if needed
//var pathBase = builder.Configuration["PathBase"];
//if (!string.IsNullOrEmpty(pathBase))
//{
//    app.UsePathBase(pathBase);
//}

//app.UseStaticFiles();
//app.UseRouting();
//app.UseSession();
//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.Run();