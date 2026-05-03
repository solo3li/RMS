using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RMS.Web.Constants;
using RMS.Web.Models;
using System.Security.Claims;

namespace RMS.Web.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            await context.Database.MigrateAsync();

            // 1. Seed Roles and Permissions
            string[] roles = { "Admin", "Branch Manager", "Call Center", "Kitchen", "Delivery" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole(roleName);
                    await roleManager.CreateAsync(role);

                    var rolePermissions = new List<string>();
                    if (roleName == "Admin") rolePermissions = Permissions.GetAllPermissions();
                    else if (roleName == "Kitchen")
                    {
                        rolePermissions.Add(Permissions.Orders.View);
                        rolePermissions.Add(Permissions.Orders.UpdateStatus);
                        rolePermissions.Add(Permissions.Menu.View);
                    }
                    else if (roleName == "Delivery")
                    {
                        rolePermissions.Add(Permissions.Orders.View);
                        rolePermissions.Add(Permissions.Orders.UpdateStatus);
                    }
                    else if (roleName == "Call Center")
                    {
                        rolePermissions.Add(Permissions.Orders.View);
                        rolePermissions.Add(Permissions.Orders.Create);
                        rolePermissions.Add(Permissions.Orders.Edit);
                        rolePermissions.Add(Permissions.Customers.View);
                        rolePermissions.Add(Permissions.Customers.Create);
                        rolePermissions.Add(Permissions.Customers.Edit);
                        rolePermissions.Add(Permissions.Menu.View);
                    }
                    else if (roleName == "Branch Manager")
                    {
                        rolePermissions.AddRange(new[] {
                            Permissions.Orders.View, Permissions.Orders.Create, Permissions.Orders.Edit,
                            Permissions.Orders.UpdateStatus, Permissions.Orders.Cancel, Permissions.Orders.AssignDelivery,
                            Permissions.Menu.View, Permissions.Menu.Manage,
                            Permissions.Customers.View, Permissions.Customers.Create, Permissions.Customers.Edit,
                            Permissions.Reports.View
                        });
                    }

                    foreach (var permission in rolePermissions)
                    {
                        await roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, permission));
                    }
                }
            }

            // 2. Seed Branches
            if (!context.Branches.Any())
            {
                context.Branches.AddRange(
                    new Branch { Name = "Main Branch", IsOpen = true, WorkingHours = "09:00 - 23:00", DeliveryZones = "Zone A, Zone B, Zone C" },
                    new Branch { Name = "Downtown Express", IsOpen = true, WorkingHours = "10:00 - 22:00", DeliveryZones = "Zone D, Zone E" },
                    new Branch { Name = "Westside Grill", IsOpen = true, WorkingHours = "11:00 - 00:00", DeliveryZones = "Zone F, Zone G" }
                );
                await context.SaveChangesAsync();
            }

            var mainBranch = await context.Branches.FirstAsync(b => b.Name == "Main Branch");
            var downtownBranch = await context.Branches.FirstAsync(b => b.Name == "Downtown Express");

            // 3. Seed Users
            async Task CreateUser(string email, string name, string role, Branch? branch = null)
            {
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var user = new ApplicationUser { UserName = email, Email = email, FullName = name, IsActive = true, CreatedAt = DateTime.UtcNow };
                    if (branch != null) user.AssignedBranches.Add(branch);
                    var result = await userManager.CreateAsync(user, "Pass@123");
                    if (result.Succeeded) await userManager.AddToRoleAsync(user, role);
                }
            }

            await CreateUser("admin@rms.com", "System Admin", "Admin");
            await CreateUser("manager.main@rms.com", "Main Manager", "Branch Manager", mainBranch);
            await CreateUser("kitchen.main@rms.com", "Main Chef", "Kitchen", mainBranch);
            await CreateUser("delivery.main@rms.com", "Main Rider", "Delivery", mainBranch);
            await CreateUser("callcenter@rms.com", "Agent Smith", "Call Center");
            await CreateUser("kitchen.downtown@rms.com", "Downtown Cook", "Kitchen", downtownBranch);

            var adminUser = await userManager.FindByEmailAsync("admin@rms.com");

            // 4. Seed Categories and Menu Items
            if (!context.Categories.Any())
            {
                var pizzaCat = new Category { Name = "Pizzas" };
                var burgerCat = new Category { Name = "Burgers" };
                var drinkCat = new Category { Name = "Beverages" };
                var saladCat = new Category { Name = "Salads" };
                context.Categories.AddRange(pizzaCat, burgerCat, drinkCat, saladCat);
                await context.SaveChangesAsync();

                var items = new List<MenuItem>
                {
                    new MenuItem { Name = "Margherita Pizza", Description = "Tomato sauce, mozzarella, fresh basil", Price = 12.99m, Category = pizzaCat, IsAvailableGlobal = true },
                    new MenuItem { Name = "Pepperoni Feast", Description = "Double pepperoni and extra mozzarella", Price = 14.99m, Category = pizzaCat, IsAvailableGlobal = true },
                    new MenuItem { Name = "Classic Cheeseburger", Description = "Beef patty, cheddar, lettuce, tomato", Price = 10.99m, Category = burgerCat, IsAvailableGlobal = true },
                    new MenuItem { Name = "BBQ Bacon Burger", Description = "Beef patty, bacon, BBQ sauce, onion rings", Price = 13.49m, Category = burgerCat, IsAvailableGlobal = true },
                    new MenuItem { Name = "Caesar Salad", Description = "Romaine lettuce, croutons, parmesan, dressing", Price = 8.99m, Category = saladCat, IsAvailableGlobal = true },
                    new MenuItem { Name = "Greek Salad", Description = "Cucumber, olives, feta cheese, onions", Price = 9.49m, Category = saladCat, IsAvailableGlobal = true },
                    new MenuItem { Name = "Coca Cola", Description = "330ml can", Price = 2.50m, Category = drinkCat, IsAvailableGlobal = true },
                    new MenuItem { Name = "Orange Juice", Description = "Freshly squeezed", Price = 3.50m, Category = drinkCat, IsAvailableGlobal = true }
                };
                context.MenuItems.AddRange(items);
                await context.SaveChangesAsync();

                // Add some extras and variants
                var marg = items.First(i => i.Name == "Margherita Pizza");
                context.Extras.Add(new Extra { MenuItemId = marg.ID, Name = "Extra Cheese", Price = 1.50m });
                context.Variants.Add(new Variant { MenuItemId = marg.ID, Name = "Large", Price = 16.99m });
                context.Variants.Add(new Variant { MenuItemId = marg.ID, Name = "Small", Price = 10.99m });

                var burger = items.First(i => i.Name == "Classic Cheeseburger");
                context.Extras.Add(new Extra { MenuItemId = burger.ID, Name = "Bacon", Price = 2.00m });
                context.Extras.Add(new Extra { MenuItemId = burger.ID, Name = "Egg", Price = 1.00m });

                await context.SaveChangesAsync();
            }

            // 5. Seed System Settings
            if (!context.SystemSettings.Any())
            {
                context.SystemSettings.AddRange(
                    new SystemSetting { Key = "StoreName", Value = "RMS Enterprise", Description = "The name of the restaurant" },
                    new SystemSetting { Key = "CurrencySymbol", Value = "$", Description = "Currency symbol used for prices" },
                    new SystemSetting { Key = "TaxRate", Value = "10", Description = "Tax rate in percentage" },
                    new SystemSetting { Key = "DeliveryFee", Value = "5.00", Description = "Standard delivery fee" }
                );
                await context.SaveChangesAsync();
            }

            // 6. Seed Customers
            if (!context.Customers.Any())
            {
                var cust1 = new Customer { Name = "John Doe", Phone = "555-0101" };
                var cust2 = new Customer { Name = "Jane Smith", Phone = "555-0202" };
                context.Customers.AddRange(cust1, cust2);
                await context.SaveChangesAsync();

                context.CustomerAddresses.AddRange(
                    new CustomerAddress { CustomerId = cust1.ID, AddressLine = "123 Maple St, Zone A", Zone = "Zone A" },
                    new CustomerAddress { CustomerId = cust1.ID, AddressLine = "456 Oak Ave, Zone B", Zone = "Zone B" },
                    new CustomerAddress { CustomerId = cust2.ID, AddressLine = "789 Pine Rd, Zone D", Zone = "Zone D" }
                );
                await context.SaveChangesAsync();
            }

            // 7. Seed Sample Orders
            if (!context.Orders.Any() && adminUser != null)
            {
                var john = await context.Customers.FirstAsync(c => c.Name == "John Doe");
                var jane = await context.Customers.FirstAsync(c => c.Name == "Jane Smith");
                var johnAddr = await context.CustomerAddresses.FirstAsync(a => a.CustomerId == john.ID);
                var pizza = await context.MenuItems.FirstAsync(m => m.Name == "Margherita Pizza");
                var burger = await context.MenuItems.FirstAsync(m => m.Name == "Classic Cheeseburger");

                // Order 1: Preparing in Main Branch
                var order1 = new Order
                {
                    CustomerId = john.ID,
                    BranchId = mainBranch.ID,
                    OrderType = OrderType.Delivery,
                    DeliveryAddressId = johnAddr.ID,
                    Status = OrderStatus.Preparing,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                    TotalPrice = 25.48m, // pizza + burger + delivery
                    DeliveryFee = 5.00m,
                    CreatedByUserId = adminUser.Id
                };
                context.Orders.Add(order1);
                await context.SaveChangesAsync();
                context.OrderItems.AddRange(
                    new OrderItem { OrderId = order1.ID, MenuItemId = pizza.ID, Quantity = 1, UnitPrice = pizza.Price },
                    new OrderItem { OrderId = order1.ID, MenuItemId = burger.ID, Quantity = 1, UnitPrice = burger.Price }
                );

                // Order 2: Confirmed in Downtown
                var order2 = new Order
                {
                    CustomerId = jane.ID,
                    BranchId = downtownBranch.ID,
                    OrderType = OrderType.Pickup,
                    Status = OrderStatus.Confirmed,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-15),
                    TotalPrice = 12.99m,
                    CreatedByUserId = adminUser.Id
                };
                context.Orders.Add(order2);
                await context.SaveChangesAsync();
                context.OrderItems.Add(new OrderItem { OrderId = order2.ID, MenuItemId = pizza.ID, Quantity = 1, UnitPrice = pizza.Price });

                // Order 3: Ready in Main Branch
                var order3 = new Order
                {
                    CustomerId = john.ID,
                    BranchId = mainBranch.ID,
                    OrderType = OrderType.Pickup,
                    Status = OrderStatus.Ready,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-45),
                    TotalPrice = 10.99m,
                    CreatedByUserId = adminUser.Id
                };
                context.Orders.Add(order3);
                await context.SaveChangesAsync();
                context.OrderItems.Add(new OrderItem { OrderId = order3.ID, MenuItemId = burger.ID, Quantity = 1, UnitPrice = burger.Price });

                await context.SaveChangesAsync();
            }
        }
    }
}
