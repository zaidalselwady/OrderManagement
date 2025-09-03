using Microsoft.EntityFrameworkCore;
using Order_Management_System.Models.Domain;

namespace Order_Management_System.Data
{
    public class OrderManagementDbContext : DbContext
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderManagementDbContext> _logger;

        public OrderManagementDbContext(DbContextOptions<OrderManagementDbContext> options)
            : base(options)
        {
        }

        // Constructor with configuration for fallback connection string
        public OrderManagementDbContext(
            DbContextOptions<OrderManagementDbContext> options,
            IConfiguration configuration,
            ILogger<OrderManagementDbContext> logger)
            : base(options)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<Unit> Units { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                _logger?.LogWarning("DbContext not configured through DI, attempting fallback configuration");

                // Fallback configuration if DI setup failed
                var connectionString = _configuration?.GetConnectionString("DefaultConnection");
                if (!string.IsNullOrEmpty(connectionString))
                {
                    optionsBuilder.UseSqlServer(connectionString);
                    _logger?.LogInformation("Using fallback connection string configuration");
                }
                else
                {
                    _logger?.LogError("No connection string available for fallback configuration");
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map to existing tables
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Customer>().ToTable("Cust_Sup");
            modelBuilder.Entity<Order>().ToTable("Inv_Orders_Master");
            modelBuilder.Entity<OrderDetail>().ToTable("Inv_Orders_Details");
            modelBuilder.Entity<InventoryItem>().ToTable("Inv_Item_Index_Child");
            modelBuilder.Entity<Unit>().ToTable("Inv_Units");

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).HasColumnName("User_ID").HasConversion<int>();
                entity.Property(e => e.UserName).HasColumnName("User_Name").IsRequired().HasMaxLength(50);
                entity.Property(e => e.Password).HasColumnName("Password").IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordDate).HasColumnName("Password_Date");
                entity.Property(e => e.PasswordPeriod).HasColumnName("Password_Period").HasConversion<int>();
                entity.Property(e => e.CompanyId).HasColumnName("Company_ID").HasConversion<int>();
            });

            // Configure Customer entity
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustSupId);
                entity.Property(e => e.CustSupId).HasColumnName("Cust_Sup_id");
                entity.Property(e => e.NameEnglish).HasColumnName("Name_e").IsRequired().HasMaxLength(100);
                entity.Property(e => e.NameArabic).HasColumnName("Name_a").HasMaxLength(100);
                entity.Property(e => e.CustomerNumber).HasColumnName("Cust_Sup_no").HasMaxLength(50);
                entity.Property(e => e.CountryId).HasColumnName("Country_id");
                entity.Property(e => e.CityId).HasColumnName("City_id");
                entity.Property(e => e.DiscountPercent).HasColumnName("Discount_Percent").HasPrecision(5, 2);
                entity.Property(e => e.ContactPerson).HasColumnName("Contact_Person");
                entity.Property(e => e.Address1).HasColumnName("Address");
                entity.Property(e => e.Phone1).HasColumnName("Phone1").HasMaxLength(20);
                entity.Property(e => e.Phone2).HasColumnName("Phone2").HasMaxLength(20);
                entity.Property(e => e.Fax).HasColumnName("Fax");
                entity.Property(e => e.Email).HasColumnName("E_mail").HasMaxLength(100);
                entity.Property(e => e.Website).HasColumnName("Web_site");
                entity.Property(e => e.ZipCode).HasColumnName("Zip_Code");
                entity.Property(e => e.POBox).HasColumnName("P_O_Box");
                entity.Property(e => e.IsReleaseTax).HasColumnName("Is_Release_Tax");
                entity.Property(e => e.ReleaseNumber).HasColumnName("Release_No");
                entity.Property(e => e.ReleaseExpiryDate).HasColumnName("Release_Expiry_Date");
                entity.Property(e => e.IsProjectAccount).HasColumnName("Is_Project_Account");
                entity.Property(e => e.SalesmanId).HasColumnName("Salesman_id");
            });

            // Configure Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderId);
                entity.Property(e => e.OrderId).HasColumnName("orderId");
                entity.Property(e => e.OrderNumber).HasColumnName("Order_no");
                entity.Property(e => e.OrderDate).HasColumnName("Order_date");
                entity.Property(e => e.ReceivedDate).HasColumnName("rcvd_date");
                entity.Property(e => e.CustomerId).HasColumnName("Customer_id");
                entity.Property(e => e.CustomerName).HasColumnName("Customer_Name").HasMaxLength(100);
                entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2);
                entity.Property(e => e.DeliveryTerms).HasColumnName("Delivery_Terms").HasMaxLength(500);
                entity.Property(e => e.PaymentTerms).HasColumnName("Payment_Terms").HasMaxLength(500);
                entity.Property(e => e.Status).HasColumnName("Status");
                entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(1000);
                entity.Property(e => e.SalesmanId).HasColumnName("Salesman_id");
                entity.Property(e => e.UserId).HasColumnName("User_id");

                // Relationships
                entity.HasMany(e => e.OrderDetails)
                      .WithOne()
                      .HasForeignKey("OrderId")
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure OrderDetail entity
            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.HasKey(e => e.OrderDetailId);
                entity.Property(e => e.OrderDetailId).HasColumnName("Order_Detail_id");
                entity.Property(e => e.OrderId).HasColumnName("Order_id");
                entity.Property(e => e.ItemChildId).HasColumnName("Item_Child_id");
                entity.Property(e => e.ItemDescription).HasColumnName("Item_Desc").IsRequired().HasMaxLength(200);
                entity.Property(e => e.OrderQuantity).HasColumnName("Ord_Qty");
                entity.Property(e => e.BonusQuantity).HasColumnName("Qty_Bonus");
                entity.Property(e => e.Price).HasColumnName("Price").HasPrecision(18, 2);
                entity.Property(e => e.DiscountPercent).HasColumnName("Discount_Percent").HasPrecision(5, 2);
                entity.Property(e => e.BarCode).HasColumnName("Bar_Code").HasMaxLength(50);
                entity.Property(e => e.UnitId).HasColumnName("Unit_id");
                entity.Property(e => e.ItemNotes).HasColumnName("Item_Notes").HasMaxLength(500);
            });

            // Configure InventoryItem entity
            modelBuilder.Entity<InventoryItem>(entity =>
            {
                entity.HasKey(e => e.ItemChildId);
                entity.Property(e => e.ItemChildId).HasColumnName("Item_Child_id");
                entity.Property(e => e.ItemDescription).HasColumnName("Item_Child_Desc_e").IsRequired().HasMaxLength(200);
                entity.Property(e => e.Price).HasColumnName("Price").HasPrecision(18, 2);
                entity.Property(e => e.BarCode).HasColumnName("Bar_Code").IsRequired().HasMaxLength(50);
                entity.Property(e => e.UnitId).HasColumnName("Unit_id");
            });

            // Configure Unit entity
            modelBuilder.Entity<Unit>(entity =>
            {
                entity.HasKey(e => e.UnitId);
                entity.Property(e => e.UnitId).HasColumnName("Unit_id");
                entity.Property(e => e.UnitDescriptionEnglish).HasColumnName("Unit_Desc_e").IsRequired().HasMaxLength(50);
                entity.Property(e => e.UnitDescriptionArabic).HasColumnName("Unit_Desc_a").HasMaxLength(50);
            });

            // Add indexes for better performance
            modelBuilder.Entity<User>()
                .HasIndex(u => u.UserName)
                .IsUnique()
                .HasDatabaseName("IX_Users_UserName");

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.CustomerNumber)
                .HasDatabaseName("IX_Customers_CustomerNumber");

            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.OrderNumber, o.OrderDate })
                .HasDatabaseName("IX_Orders_OrderNumber_OrderDate");

            modelBuilder.Entity<OrderDetail>()
                .HasIndex(od => od.OrderId)
                .HasDatabaseName("IX_OrderDetails_OrderId");
        }

        // Override methods for better logging and error handling
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving changes to database");
                throw;
            }
        }

        public override int SaveChanges()
        {
            try
            {
                return base.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving changes to database");
                throw;
            }
        }
    }
}











//using Microsoft.EntityFrameworkCore;
//using Order_Management_System.Models.Domain;

//namespace Order_Management_System.Data
//{
//    public class OrderManagementDbContext : DbContext
//    {
//        public OrderManagementDbContext(DbContextOptions<OrderManagementDbContext> options)
//            : base(options)
//        {
//        }

//        public DbSet<User> Users { get; set; } = null!;
//        public DbSet<Customer> Customers { get; set; } = null!;
//        public DbSet<Order> Orders { get; set; } = null!;
//        public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
//        public DbSet<InventoryItem> InventoryItems { get; set; } = null!;
//        public DbSet<Unit> Units { get; set; } = null!;

//        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//        {
//            // This ensures the connection string is properly set even if not configured in DI
//            if (!optionsBuilder.IsConfigured)
//            {
//                var configuration = new ConfigurationBuilder()
//                    .SetBasePath(Directory.GetCurrentDirectory())
//                    .AddJsonFile("appsettings.json")
//                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
//                    .Build();

//                var connectionString = configuration.GetConnectionString("DefaultConnection");
//                if (!string.IsNullOrEmpty(connectionString))
//                {
//                    optionsBuilder.UseSqlServer(connectionString);
//                }
//            }

//            base.OnConfiguring(optionsBuilder);
//        }

//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            // Map to existing tables
//            modelBuilder.Entity<User>().ToTable("Users");
//            modelBuilder.Entity<Customer>().ToTable("Cust_Sup");
//            modelBuilder.Entity<Order>().ToTable("Inv_Orders_Master");
//            modelBuilder.Entity<OrderDetail>().ToTable("Inv_Orders_Details");
//            modelBuilder.Entity<InventoryItem>().ToTable("Inv_Item_Index_Child");
//            modelBuilder.Entity<Unit>().ToTable("Inv_Units");

//            // Configure User entity
//            modelBuilder.Entity<User>(entity =>
//            {
//                entity.HasKey(e => e.UserId);
//                entity.Property(e => e.UserId).HasColumnName("User_ID");
//                entity.Property(e => e.UserName).HasColumnName("User_Name").IsRequired().HasMaxLength(50);
//                entity.Property(e => e.Password).HasColumnName("Password").IsRequired().HasMaxLength(100);
//                entity.Property(e => e.PasswordDate).HasColumnName("Password_Date");
//                entity.Property(e => e.PasswordPeriod).HasColumnName("Password_Period");
//                entity.Property(e => e.CompanyId).HasColumnName("Company_ID");
//            });

//            // Configure Customer entity
//            modelBuilder.Entity<Customer>(entity =>
//            {
//                entity.HasKey(e => e.CustSupId);
//                entity.Property(e => e.CustSupId).HasColumnName("Cust_Sup_id");
//                entity.Property(e => e.NameEnglish).HasColumnName("Name_e").IsRequired().HasMaxLength(100);
//                entity.Property(e => e.NameArabic).HasColumnName("Name_a").HasMaxLength(100);
//                entity.Property(e => e.CustomerNumber).HasColumnName("Cust_Sup_no").HasMaxLength(50);
//                entity.Property(e => e.CountryId).HasColumnName("Country_id");
//                entity.Property(e => e.CityId).HasColumnName("City_id");
//                entity.Property(e => e.DiscountPercent).HasColumnName("Discount_Percent").HasPrecision(5, 2);
//                entity.Property(e => e.ContactPerson).HasColumnName("Contact_Person");
//                entity.Property(e => e.Address1).HasColumnName("Address");
//                entity.Property(e => e.Phone1).HasColumnName("Phone1").HasMaxLength(20);
//                entity.Property(e => e.Phone2).HasColumnName("Phone2").HasMaxLength(20);
//                entity.Property(e => e.Fax).HasColumnName("Fax");
//                entity.Property(e => e.Email).HasColumnName("E_mail").HasMaxLength(100);
//                entity.Property(e => e.Website).HasColumnName("Web_site");
//                entity.Property(e => e.ZipCode).HasColumnName("Zip_Code");
//                entity.Property(e => e.POBox).HasColumnName("P_O_Box");
//                entity.Property(e => e.IsReleaseTax).HasColumnName("Is_Release_Tax");
//                entity.Property(e => e.ReleaseNumber).HasColumnName("Release_No");
//                entity.Property(e => e.ReleaseExpiryDate).HasColumnName("Release_Expiry_Date");
//                entity.Property(e => e.IsProjectAccount).HasColumnName("Is_Project_Account");
//                entity.Property(e => e.SalesmanId).HasColumnName("Salesman_id");
//            });

//            // Configure Order entity
//            modelBuilder.Entity<Order>(entity =>
//            {
//                entity.HasKey(e => e.OrderId);
//                entity.Property(e => e.OrderId).HasColumnName("orderId");
//                entity.Property(e => e.OrderNumber).HasColumnName("Order_no");
//                entity.Property(e => e.OrderDate).HasColumnName("Order_date");
//                entity.Property(e => e.ReceivedDate).HasColumnName("rcvd_date");
//                entity.Property(e => e.CustomerId).HasColumnName("Customer_id");
//                entity.Property(e => e.CustomerName).HasColumnName("Customer_Name").HasMaxLength(100);
//                entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2);
//                entity.Property(e => e.DeliveryTerms).HasColumnName("Delivery_Terms").HasMaxLength(500);
//                entity.Property(e => e.PaymentTerms).HasColumnName("Payment_Terms").HasMaxLength(500);
//                entity.Property(e => e.Status).HasColumnName("Status");
//                entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(1000);
//                entity.Property(e => e.SalesmanId).HasColumnName("Salesman_id");
//                entity.Property(e => e.UserId).HasColumnName("User_id");

//                // Relationships
//                entity.HasMany(e => e.OrderDetails)
//                      .WithOne()
//                      .HasForeignKey("OrderId");
//            });

//            // Configure OrderDetail entity
//            modelBuilder.Entity<OrderDetail>(entity =>
//            {
//                entity.HasKey(e => e.OrderDetailId);
//                entity.Property(e => e.OrderDetailId).HasColumnName("Order_Detail_id");
//                entity.Property(e => e.OrderId).HasColumnName("Order_id");
//                entity.Property(e => e.ItemChildId).HasColumnName("Item_Child_id");
//                entity.Property(e => e.ItemDescription).HasColumnName("Item_Desc").IsRequired().HasMaxLength(200);
//                entity.Property(e => e.OrderQuantity).HasColumnName("Ord_Qty");
//                entity.Property(e => e.BonusQuantity).HasColumnName("Qty_Bonus");
//                entity.Property(e => e.Price).HasColumnName("Price").HasPrecision(18, 2);
//                entity.Property(e => e.DiscountPercent).HasColumnName("Discount_Percent").HasPrecision(5, 2);
//                entity.Property(e => e.BarCode).HasColumnName("Bar_Code").HasMaxLength(50);
//                entity.Property(e => e.UnitId).HasColumnName("Unit_id");
//                entity.Property(e => e.ItemNotes).HasColumnName("Item_Notes").HasMaxLength(500);
//            });

//            // Configure InventoryItem entity
//            modelBuilder.Entity<InventoryItem>(entity =>
//            {
//                entity.HasKey(e => e.ItemChildId);
//                entity.Property(e => e.ItemChildId).HasColumnName("Item_Child_id");
//                entity.Property(e => e.ItemDescription).HasColumnName("Item_Child_Desc_e").IsRequired().HasMaxLength(200);
//                entity.Property(e => e.Price).HasColumnName("Price").HasPrecision(18, 2);
//                entity.Property(e => e.BarCode).HasColumnName("Bar_Code").IsRequired().HasMaxLength(50);
//                entity.Property(e => e.UnitId).HasColumnName("Unit_id");
//            });

//            // Configure Unit entity
//            modelBuilder.Entity<Unit>(entity =>
//            {
//                entity.HasKey(e => e.UnitId);
//                entity.Property(e => e.UnitId).HasColumnName("Unit_id");
//                entity.Property(e => e.UnitDescriptionEnglish).HasColumnName("Unit_Desc_e").IsRequired().HasMaxLength(50);
//                entity.Property(e => e.UnitDescriptionArabic).HasColumnName("Unit_Desc_a").HasMaxLength(50);
//            });
//        }
//    }
//}