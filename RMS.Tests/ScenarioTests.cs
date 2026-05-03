using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using RMS.Web.Data;
using RMS.Web.Models;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace RMS.Tests
{
    public class ScenarioTests
    {
        private ApplicationDbContext GetDbContext(string userId = "test-user")
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var context = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            context.User = claimsPrincipal;
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(context);

            return new ApplicationDbContext(options, mockHttpContextAccessor.Object);
        }

        [Fact]
        public async Task BranchManagement_CreateBranch_VerifiesProperties()
        {
            // Arrange
            using var db = GetDbContext();
            var branch = new Branch
            {
                Name = "Downtown Branch",
                IsOpen = true,
                WorkingHours = "08:00-22:00",
                DeliveryZones = "Zone A, Zone B"
            };

            // Act
            db.Branches.Add(branch);
            await db.SaveChangesAsync();

            // Assert
            var savedBranch = await db.Branches.FirstOrDefaultAsync(b => b.Name == "Downtown Branch");
            Assert.NotNull(savedBranch);
            Assert.Equal("Downtown Branch", savedBranch.Name);
            Assert.True(savedBranch.IsOpen);
            Assert.Equal("08:00-22:00", savedBranch.WorkingHours);
        }

        [Fact]
        public async Task MenuManagement_CreateCategoryAndMenuItem_VerifiesRelationships()
        {
            // Arrange
            using var db = GetDbContext();
            var category = new Category { Name = "Burgers" };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            var menuItem = new MenuItem
            {
                Name = "Cheeseburger",
                Price = 9.99m,
                CategoryId = category.ID,
                Description = "Delicious cheeseburger"
            };

            // Act
            db.MenuItems.Add(menuItem);
            await db.SaveChangesAsync();

            // Assert
            var savedItem = await db.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Name == "Cheeseburger");

            Assert.NotNull(savedItem);
            Assert.Equal(category.ID, savedItem.CategoryId);
            Assert.Equal("Burgers", savedItem.Category.Name);
        }

        [Fact]
        public async Task MenuManagement_ScheduledAvailability_VerifiesFields()
        {
            // Arrange
            using var db = GetDbContext();
            var from = DateTime.Now.AddDays(1);
            var to = DateTime.Now.AddDays(7);
            
            var menuItem = new MenuItem
            {
                Name = "Future Pizza",
                Price = 15.00m,
                CategoryId = 1, // Assume category exists or ignore FK for in-memory if not enforced
                AvailableFrom = from,
                AvailableTo = to
            };

            // Act
            db.MenuItems.Add(menuItem);
            await db.SaveChangesAsync();

            // Assert
            var savedItem = await db.MenuItems.FirstOrDefaultAsync(m => m.Name == "Future Pizza");
            Assert.NotNull(savedItem);
            Assert.Equal(from, savedItem.AvailableFrom);
            Assert.Equal(to, savedItem.AvailableTo);

            // Logic check simulation
            var now = DateTime.Now;
            bool isAvailable = (!savedItem.AvailableFrom.HasValue || now >= savedItem.AvailableFrom.Value) &&
                               (!savedItem.AvailableTo.HasValue || now <= savedItem.AvailableTo.Value);
            
            Assert.False(isAvailable); // Should be false because it starts tomorrow
        }

        [Fact]
        public async Task OrderCreation_Delivery_VerifiesFeeAndItems()
        {
            // Arrange
            using var db = GetDbContext("user-1");
            
            var customer = new Customer { Name = "John Doe", Phone = "1234567890" };
            db.Customers.Add(customer);
            
            var branch = new Branch { Name = "Main" };
            db.Branches.Add(branch);

            var category = new Category { Name = "Food" };
            db.Categories.Add(category);

            var item = new MenuItem { Name = "Pizza", Price = 12.00m, Category = category };
            db.MenuItems.Add(item);

            var user = new ApplicationUser { Id = "user-1", UserName = "testuser" };
            db.Users.Add(user);

            await db.SaveChangesAsync();

            // Act
            var order = new Order
            {
                CustomerId = customer.ID,
                BranchId = branch.ID,
                OrderType = OrderType.Delivery,
                CreatedByUserId = "user-1",
                DeliveryFee = 5.00m,
                Status = OrderStatus.Pending,
                TotalPrice = 17.00m,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { MenuItemId = item.ID, Quantity = 1, UnitPrice = 12.00m }
                }
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            // Assert
            var savedOrder = await db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.ID == order.ID);

            Assert.NotNull(savedOrder);
            Assert.Equal(5.00m, savedOrder.DeliveryFee);
            Assert.Single(savedOrder.OrderItems);
            Assert.Equal(item.ID, savedOrder.OrderItems.First().MenuItemId);
        }

        [Fact]
        public async Task OrderCreation_Pickup_VerifiesZeroFee()
        {
            // Arrange
            using var db = GetDbContext();
            
            var customer = new Customer { Name = "Jane Doe", Phone = "0987654321" };
            db.Customers.Add(customer);
            
            var branch = new Branch { Name = "Main" };
            db.Branches.Add(branch);

            var user = new ApplicationUser { Id = "test-user", UserName = "testuser" };
            db.Users.Add(user);

            await db.SaveChangesAsync();

            // Act
            var order = new Order
            {
                CustomerId = customer.ID,
                BranchId = branch.ID,
                OrderType = OrderType.Pickup,
                CreatedByUserId = "test-user",
                DeliveryFee = 0m,
                Status = OrderStatus.Pending,
                TotalPrice = 10.00m
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            // Assert
            var savedOrder = await db.Orders.FindAsync(order.ID);
            Assert.NotNull(savedOrder);
            Assert.Equal(0m, savedOrder.DeliveryFee);
            Assert.Equal(OrderType.Pickup, savedOrder.OrderType);
        }

        [Fact]
        public async Task OrderStatusTransition_VerifiesStateChanges()
        {
            // Arrange
            using var db = GetDbContext();
            
            var customer = new Customer { Name = "Alice", Phone = "111" };
            var branch = new Branch { Name = "Main" };
            var user = new ApplicationUser { Id = "test-user", UserName = "testuser" };
            
            db.Customers.Add(customer);
            db.Branches.Add(branch);
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var order = new Order
            {
                CustomerId = customer.ID,
                BranchId = branch.ID,
                CreatedByUserId = "test-user",
                Status = OrderStatus.Pending
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            // Act & Assert 1: Pending -> Confirmed
            order.Status = OrderStatus.Confirmed;
            await db.SaveChangesAsync();
            var status1 = (await db.Orders.FindAsync(order.ID))?.Status;
            Assert.Equal(OrderStatus.Confirmed, status1);

            // Act & Assert 2: Confirmed -> Preparing
            order.Status = OrderStatus.Preparing;
            await db.SaveChangesAsync();
            var status2 = (await db.Orders.FindAsync(order.ID))?.Status;
            Assert.Equal(OrderStatus.Preparing, status2);
        }

        [Fact]
        public async Task AuditLogging_UpdateBranchName_CreatesLogEntry()
        {
            // Arrange
            string userId = "audit-user";
            using var db = GetDbContext(userId);
            
            var user = new ApplicationUser { Id = userId, UserName = "audituser" };
            db.Users.Add(user);

            var branch = new Branch { Name = "Old Name" };
            db.Branches.Add(branch);
            await db.SaveChangesAsync();

            // Act
            branch.Name = "New Name";
            await db.SaveChangesAsync();

            // Assert
            var auditLog = await db.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync(a => a.EntityType == nameof(Branch) && a.Action == "Update");

            Assert.NotNull(auditLog);
            Assert.Equal(userId, auditLog.UserId);
            
            // Verify Before/After values
            Assert.NotNull(auditLog.BeforeValue);
            Assert.NotNull(auditLog.AfterValue);

            var before = JsonSerializer.Deserialize<Dictionary<string, object>>(auditLog.BeforeValue);
            var after = JsonSerializer.Deserialize<Dictionary<string, object>>(auditLog.AfterValue);

            Assert.Equal("Old Name", before?["Name"]?.ToString());
            Assert.Equal("New Name", after?["Name"]?.ToString());
        }
    }
}
