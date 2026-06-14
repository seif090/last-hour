using LastHour.Domain.Entities;
using LastHour.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Infrastructure.Data;

public static class ApplicationDbContextSeed
{
    public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        if (await context.Categories.AnyAsync()) return;

        // Roles
        var roles = new[] { "Admin", "Merchant", "Customer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Categories
        var categories = new List<Category>
        {
            new() { Name = "Bakery", ImageUrl = "/images/categories/bakery.jpg", Icon = "bakery_icon", Color = "#F5A623", DisplayOrder = 1, IsActive = true },
            new() { Name = "Groceries", ImageUrl = "/images/categories/groceries.jpg", Icon = "groceries_icon", Color = "#4CAF50", DisplayOrder = 2, IsActive = true },
            new() { Name = "Beverages", ImageUrl = "/images/categories/beverages.jpg", Icon = "beverages_icon", Color = "#2196F3", DisplayOrder = 3, IsActive = true },
            new() { Name = "Snacks", ImageUrl = "/images/categories/snacks.jpg", Icon = "snacks_icon", Color = "#FF5722", DisplayOrder = 4, IsActive = true },
            new() { Name = "Dairy", ImageUrl = "/images/categories/dairy.jpg", Icon = "dairy_icon", Color = "#9C27B0", DisplayOrder = 5, IsActive = true },
            new() { Name = "Produce", ImageUrl = "/images/categories/produce.jpg", Icon = "produce_icon", Color = "#8BC34A", DisplayOrder = 6, IsActive = true },
            new() { Name = "Meals", ImageUrl = "/images/categories/meals.jpg", Icon = "meals_icon", Color = "#E91E63", DisplayOrder = 7, IsActive = true },
            new() { Name = "Desserts", ImageUrl = "/images/categories/desserts.jpg", Icon = "desserts_icon", Color = "#795548", DisplayOrder = 8, IsActive = true },
        };
        context.Categories.AddRange(categories);

        // Admin user
        var adminUser = new ApplicationUser
        {
            UserName = "admin@lasthour.com",
            Email = "admin@lasthour.com",
            FullName = "System Admin",
            EmailConfirmed = true
        };
        if (await userManager.FindByEmailAsync(adminUser.Email) == null)
        {
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                context.UserProfiles.Add(new UserProfile
                {
                    IdentityId = adminUser.Id,
                    FullName = "System Admin",
                    Email = adminUser.Email,
                    IsEmailVerified = true
                });
            }
        }

        // Merchant 1 - Fresh Bakery
        var bakerUser = new ApplicationUser
        {
            UserName = "baker@lasthour.com",
            Email = "baker@lasthour.com",
            FullName = "Ahmed Baker",
            EmailConfirmed = true
        };
        ApplicationUser? bakerIdentity = null;
        if (await userManager.FindByEmailAsync(bakerUser.Email) == null)
        {
            var result = await userManager.CreateAsync(bakerUser, "Merchant@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(bakerUser, "Merchant");
                bakerIdentity = bakerUser;
                context.UserProfiles.Add(new UserProfile
                {
                    IdentityId = bakerUser.Id,
                    FullName = "Ahmed Baker",
                    Email = bakerUser.Email,
                    Phone = "+201001234567",
                    IsEmailVerified = true
                });
            }
        }

        var freshBakery = new Store
        {
            OwnerId = bakerIdentity?.Id ?? "",
            Name = "Fresh Bakery",
            Description = "Artisan breads, pastries, and cakes baked fresh every morning. Last-hour deals on day-end surplus!",
            CoverImageUrl = "/images/stores/fresh-bakery-cover.jpg",
            LogoUrl = "/images/stores/fresh-bakery-logo.jpg",
            Category = "Bakery",
            Rating = 4.5,
            ReviewCount = 128,
            Latitude = 30.0444,
            Longitude = 31.2357,
            Address = "12 Tahrir Street, Downtown, Cairo",
            Phone = "+201001234567",
            Email = "baker@lasthour.com",
            IsOpen = true,
            OpeningHours = "07:00",
            ClosingHours = "22:00",
            ActiveOfferCount = 4,
            IsActive = true,
            AcceptedPaymentMethods = "Cash,Credit Card,Wallet"
        };
        context.Stores.Add(freshBakery);

        // Merchant 2 - Daily Groceries
        var grocerUser = new ApplicationUser
        {
            UserName = "grocer@lasthour.com",
            Email = "grocer@lasthour.com",
            FullName = "Sara Grocer",
            EmailConfirmed = true
        };
        ApplicationUser? grocerIdentity = null;
        if (await userManager.FindByEmailAsync(grocerUser.Email) == null)
        {
            var result = await userManager.CreateAsync(grocerUser, "Merchant@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(grocerUser, "Merchant");
                grocerIdentity = grocerUser;
                context.UserProfiles.Add(new UserProfile
                {
                    IdentityId = grocerUser.Id,
                    FullName = "Sara Grocer",
                    Email = grocerUser.Email,
                    Phone = "+201009876543",
                    IsEmailVerified = true
                });
            }
        }

        var dailyGroceries = new Store
        {
            OwnerId = grocerIdentity?.Id ?? "",
            Name = "Daily Groceries",
            Description = "Your neighborhood grocery store with fresh produce, pantry staples, and daily essentials at unbeatable prices.",
            CoverImageUrl = "/images/stores/daily-groceries-cover.jpg",
            LogoUrl = "/images/stores/daily-groceries-logo.jpg",
            Category = "Groceries",
            Rating = 4.2,
            ReviewCount = 95,
            Latitude = 30.0500,
            Longitude = 31.2400,
            Address = "45 Nile Street, Garden City, Cairo",
            Phone = "+201009876543",
            Email = "grocer@lasthour.com",
            IsOpen = true,
            OpeningHours = "08:00",
            ClosingHours = "23:00",
            ActiveOfferCount = 5,
            IsActive = true,
            AcceptedPaymentMethods = "Cash,Credit Card,Debit Card,Wallet"
        };
        context.Stores.Add(dailyGroceries);

        // Merchant 3 - Cool Drinks
        var beverageUser = new ApplicationUser
        {
            UserName = "beverage@lasthour.com",
            Email = "beverage@lasthour.com",
            FullName = "Omar Drinks",
            EmailConfirmed = true
        };
        ApplicationUser? beverageIdentity = null;
        if (await userManager.FindByEmailAsync(beverageUser.Email) == null)
        {
            var result = await userManager.CreateAsync(beverageUser, "Merchant@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(beverageUser, "Merchant");
                beverageIdentity = beverageUser;
                context.UserProfiles.Add(new UserProfile
                {
                    IdentityId = beverageUser.Id,
                    FullName = "Omar Drinks",
                    Email = beverageUser.Email,
                    Phone = "+201005551234",
                    IsEmailVerified = true
                });
            }
        }

        var coolDrinks = new Store
        {
            OwnerId = beverageIdentity?.Id ?? "",
            Name = "Cool Drinks",
            Description = "Premium beverages, fresh juices, and specialty drinks. Last-hour clearance on today's fresh batches!",
            CoverImageUrl = "/images/stores/cool-drinks-cover.jpg",
            LogoUrl = "/images/stores/cool-drinks-logo.jpg",
            Category = "Beverages",
            Rating = 4.7,
            ReviewCount = 203,
            Latitude = 30.0550,
            Longitude = 31.2450,
            Address = "78 Zamalek Street, Zamalek, Cairo",
            Phone = "+201005551234",
            Email = "beverage@lasthour.com",
            IsOpen = true,
            OpeningHours = "09:00",
            ClosingHours = "02:00",
            ActiveOfferCount = 3,
            IsActive = true,
            AcceptedPaymentMethods = "Cash,Credit Card,Wallet"
        };
        context.Stores.Add(coolDrinks);

        // Merchant 4 - Pizza Corner (no registered user for this, but let's add one)
        var pizzaUser = new ApplicationUser
        {
            UserName = "pizza@lasthour.com",
            Email = "pizza@lasthour.com",
            FullName = "Mona Pizza",
            EmailConfirmed = true
        };
        ApplicationUser? pizzaIdentity = null;
        if (await userManager.FindByEmailAsync(pizzaUser.Email) == null)
        {
            var result = await userManager.CreateAsync(pizzaUser, "Merchant@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(pizzaUser, "Merchant");
                pizzaIdentity = pizzaUser;
                context.UserProfiles.Add(new UserProfile
                {
                    IdentityId = pizzaUser.Id,
                    FullName = "Mona Pizza",
                    Email = pizzaUser.Email,
                    Phone = "+201007778888",
                    IsEmailVerified = true
                });
            }
        }

        var pizzaCorner = new Store
        {
            OwnerId = pizzaIdentity?.Id ?? "",
            Name = "Pizza Corner",
            Description = "Authentic Italian pizzas baked in wood-fired oven. Grab last-hour deals on our daily specials!",
            CoverImageUrl = "/images/stores/pizza-corner-cover.jpg",
            LogoUrl = "/images/stores/pizza-corner-logo.jpg",
            Category = "Meals",
            Rating = 4.3,
            ReviewCount = 167,
            Latitude = 30.0600,
            Longitude = 31.2300,
            Address = "23 Mohammed Mahmoud Street, Abdeen, Cairo",
            Phone = "+201007778888",
            Email = "pizza@lasthour.com",
            IsOpen = true,
            OpeningHours = "11:00",
            ClosingHours = "01:00",
            ActiveOfferCount = 3,
            IsActive = true,
            AcceptedPaymentMethods = "Cash,Credit Card,Debit Card"
        };
        context.Stores.Add(pizzaCorner);

        // Customers
        var customers = new[]
        {
            new { UserName = "john@example.com", Email = "john@example.com", Name = "John Doe", Phone = "+201011111111" },
            new { UserName = "jane@example.com", Email = "jane@example.com", Name = "Jane Smith", Phone = "+201022222222" },
        };

        foreach (var c in customers)
        {
            if (await userManager.FindByEmailAsync(c.Email) == null)
            {
                var identityUser = new ApplicationUser
                {
                    UserName = c.UserName,
                    Email = c.Email,
                    FullName = c.Name,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(identityUser, "Customer@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(identityUser, "Customer");
                    context.UserProfiles.Add(new UserProfile
                    {
                        IdentityId = identityUser.Id,
                        FullName = c.Name,
                        Email = c.Email,
                        Phone = c.Phone,
                        IsEmailVerified = true
                    });
                }
            }
        }

        await context.SaveChangesAsync();

        // Re-fetch saved stores with their IDs
        var bakery = await context.Stores.FirstAsync(s => s.Name == "Fresh Bakery");
        var groceries = await context.Stores.FirstAsync(s => s.Name == "Daily Groceries");
        var drinks = await context.Stores.FirstAsync(s => s.Name == "Cool Drinks");
        var pizza = await context.Stores.FirstAsync(s => s.Name == "Pizza Corner");

        var now = DateTime.UtcNow;
        var todayEnd = now.Date.AddDays(1).AddHours(-1); // 23:00 today

        // Offers - Fresh Bakery
        context.Offers.AddRange(new[]
        {
            new Offer
            {
                StoreId = bakery.Id,
                Title = "Fresh Croissants (Pack of 4)",
                Description = "Buttery, flaky croissants baked this morning. Day-end clearance!",
                OriginalPrice = 80, DiscountPrice = 35,
                OriginalQuantity = 20, RemainingQuantity = 8,
                ExpiryTime = todayEnd,
                Category = "Bakery", ImageUrl = "/images/offers/croissants.jpg",
                IsActive = true, IsFeatured = true,
                Rating = 4.6, ReviewCount = 34,
                PickupStartTime = now.AddHours(1), PickupEndTime = todayEnd
            },
            new Offer
            {
                StoreId = bakery.Id,
                Title = "Sourdough Loaf",
                Description = "Artisan sourdough with 24-hour fermentation. Today's batch must go!",
                OriginalPrice = 60, DiscountPrice = 25,
                OriginalQuantity = 15, RemainingQuantity = 5,
                ExpiryTime = todayEnd,
                Category = "Bakery", ImageUrl = "/images/offers/sourdough.jpg",
                IsActive = true, IsFeatured = true,
                Rating = 4.8, ReviewCount = 56,
                PickupStartTime = now.AddHours(1), PickupEndTime = todayEnd
            },
            new Offer
            {
                StoreId = bakery.Id,
                Title = "Chocolate Cake Slice",
                Description = "Rich Belgian chocolate cake with ganache topping. Limited slices!",
                OriginalPrice = 45, DiscountPrice = 20,
                OriginalQuantity = 10, RemainingQuantity = 3,
                ExpiryTime = todayEnd,
                Category = "Desserts", ImageUrl = "/images/offers/chocolate-cake.jpg",
                IsActive = true, IsFeatured = false,
                Rating = 4.9, ReviewCount = 78,
                PickupStartTime = now.AddHours(1), PickupEndTime = todayEnd
            },
            new Offer
            {
                StoreId = bakery.Id,
                Title = "Mixed Pastry Box (6 pcs)",
                Description = "Assorted Danish pastries - cinnamon, apple, and cheese. Perfect for breakfast!",
                OriginalPrice = 120, DiscountPrice = 50,
                OriginalQuantity = 8, RemainingQuantity = 4,
                ExpiryTime = now.AddDays(1).AddHours(-2),
                Category = "Bakery", ImageUrl = "/images/offers/pastry-box.jpg",
                IsActive = true, IsFeatured = false,
                Rating = 4.4, ReviewCount = 23,
                PickupStartTime = now.AddHours(2), PickupEndTime = now.AddDays(1).AddHours(-2)
            }
        });

        // Offers - Daily Groceries
        context.Offers.AddRange(new[]
        {
            new Offer
            {
                StoreId = groceries.Id,
                Title = "Organic Fruit Basket",
                Description = "Mixed seasonal fruits - apples, oranges, bananas, and grapes. Fresh today!",
                OriginalPrice = 150, DiscountPrice = 65,
                OriginalQuantity = 25, RemainingQuantity = 12,
                ExpiryTime = todayEnd,
                Category = "Produce", ImageUrl = "/images/offers/fruit-basket.jpg",
                IsActive = true, IsFeatured = true,
                Rating = 4.3, ReviewCount = 45,
                PickupStartTime = now.AddHours(1), PickupEndTime = todayEnd
            },
            new Offer
            {
                StoreId = groceries.Id,
                Title = "Greek Yogurt (500g)",
                Description = "Creamy Greek yogurt, 5% fat. Today's batch, best before tomorrow.",
                OriginalPrice = 45, DiscountPrice = 18,
                OriginalQuantity = 30, RemainingQuantity = 15,
                ExpiryTime = todayEnd,
                Category = "Dairy", ImageUrl = "/images/offers/greek-yogurt.jpg",
                IsActive = true, IsFeatured = false,
                Rating = 4.1, ReviewCount = 32,
                PickupStartTime = now.AddHours(1), PickupEndTime = todayEnd
            },
            new Offer
            {
                StoreId = groceries.Id,
                Title = "Mixed Nuts (400g)",
                Description = "Premium roasted almonds, cashews, and walnuts. Great for snacking!",
                OriginalPrice = 95, DiscountPrice = 40,
                OriginalQuantity = 18, RemainingQuantity = 7,
                ExpiryTime = now.AddDays(3).AddHours(-2),
                Category = "Snacks", ImageUrl = "/images/offers/mixed-nuts.jpg",
                IsActive = true, IsFeatured = true,
                Rating = 4.5, ReviewCount = 67,
                PickupStartTime = now.AddHours(2), PickupEndTime = now.AddDays(3).AddHours(-2)
            },
            new Offer
            {
                StoreId = groceries.Id,
                Title = "Fresh Orange Juice (1L)",
                Description = "Freshly squeezed orange juice, no added sugar. Today's batch!",
                OriginalPrice = 35, DiscountPrice = 15,
                OriginalQuantity = 20, RemainingQuantity = 9,
                ExpiryTime = todayEnd,
                Category = "Beverages", ImageUrl = "/images/offers/orange-juice.jpg",
                IsActive = true, IsFeatured = false,
                Rating = 4.6, ReviewCount = 89,
                PickupStartTime = now.AddHours(1), PickupEndTime = todayEnd
            },
            new Offer
            {
                StoreId = groceries.Id,
                Title = "Cheddar Cheese Block (250g)",
                Description = "Aged cheddar, imported. Near best-before date, still delicious!",
                OriginalPrice = 70, DiscountPrice = 30,
                OriginalQuantity = 12, RemainingQuantity = 2,
                ExpiryTime = now.AddDays(5).AddHours(-2),
                Category = "Dairy", ImageUrl = "/images/offers/cheddar.jpg",
                IsActive = true, IsFeatured = false,
                Rating = 4.2, ReviewCount = 18,
                PickupStartTime = now.AddHours(2), PickupEndTime = now.AddDays(5).AddHours(-2)
            }
        });

        // Offers - Cool Drinks
        context.Offers.AddRange(new[]
        {
            new Offer
            {
                StoreId = drinks.Id,
                Title = "Fresh Mango Smoothie (500ml)",
                Description = "Made with real Alphonso mangoes. Today's batch must be sold!",
                OriginalPrice = 55, DiscountPrice = 25,
                OriginalQuantity = 15, RemainingQuantity = 6,
                ExpiryTime = todayEnd,
                Category = "Beverages", ImageUrl = "/images/offers/mango-smoothie.jpg",
                IsActive = true, IsFeatured = true,
                Rating = 4.7, ReviewCount = 112,
                PickupStartTime = now.AddHours(1), PickupEndTime = todayEnd
            },
            new Offer
            {
                StoreId = drinks.Id,
                Title = "Iced Coffee (Large)",
                Description = "Cold-brewed iced coffee with your choice of milk. End-of-day clearance!",
                OriginalPrice = 40, DiscountPrice = 18,
                OriginalQuantity = 20, RemainingQuantity = 10,
                ExpiryTime = todayEnd,
                Category = "Beverages", ImageUrl = "/images/offers/iced-coffee.jpg",
                IsActive = true, IsFeatured = true,
                Rating = 4.5, ReviewCount = 94,
                PickupStartTime = now.AddHours(1), PickupEndTime = todayEnd
            },
            new Offer
            {
                StoreId = drinks.Id,
                Title = "Fruit Punch (1L)",
                Description = "Refreshing mix of tropical fruits - perfect for parties! Today's fresh batch.",
                OriginalPrice = 50, DiscountPrice = 22,
                OriginalQuantity = 10, RemainingQuantity = 4,
                ExpiryTime = now.AddDays(1).AddHours(-3),
                Category = "Beverages", ImageUrl = "/images/offers/fruit-punch.jpg",
                IsActive = true, IsFeatured = false,
                Rating = 4.3, ReviewCount = 45,
                PickupStartTime = now.AddHours(2), PickupEndTime = now.AddDays(1).AddHours(-3)
            }
        });

        // Offers - Pizza Corner
        context.Offers.AddRange(new[]
        {
            new Offer
            {
                StoreId = pizza.Id,
                Title = "Margherita Pizza (Large)",
                Description = "Classic Margherita with fresh mozzarella and basil. Last-hour price!",
                OriginalPrice = 110, DiscountPrice = 55,
                OriginalQuantity = 10, RemainingQuantity = 4,
                ExpiryTime = todayEnd,
                Category = "Meals", ImageUrl = "/images/offers/margherita.jpg",
                IsActive = true, IsFeatured = true,
                Rating = 4.4, ReviewCount = 156,
                PickupStartTime = now.AddHours(1), PickupEndTime = todayEnd
            },
            new Offer
            {
                StoreId = pizza.Id,
                Title = "Pepperoni Pizza (Large)",
                Description = "Loaded with premium pepperoni and melted cheese. A customer favorite!",
                OriginalPrice = 130, DiscountPrice = 60,
                OriginalQuantity = 8, RemainingQuantity = 3,
                ExpiryTime = todayEnd,
                Category = "Meals", ImageUrl = "/images/offers/pepperoni.jpg",
                IsActive = true, IsFeatured = true,
                Rating = 4.6, ReviewCount = 203,
                PickupStartTime = now.AddHours(1), PickupEndTime = todayEnd
            },
            new Offer
            {
                StoreId = pizza.Id,
                Title = "Garlic Bread with Cheese",
                Description = "Toasted garlic bread topped with melted mozzarella. Perfect side dish!",
                OriginalPrice = 40, DiscountPrice = 18,
                OriginalQuantity = 15, RemainingQuantity = 8,
                ExpiryTime = todayEnd,
                Category = "Snacks", ImageUrl = "/images/offers/garlic-bread.jpg",
                IsActive = true, IsFeatured = false,
                Rating = 4.2, ReviewCount = 87,
                PickupStartTime = now.AddHours(1), PickupEndTime = todayEnd
            }
        });

        await context.SaveChangesAsync();

        // Coupons
        context.Coupons.AddRange(new[]
        {
            new Coupon
            {
                Code = "WELCOME10", DiscountPercentage = 10,
                MaxDiscountAmount = 50, MinOrderAmount = 30,
                UsageLimit = 100, UsedCount = 3,
                ValidFrom = now.AddDays(-7), ValidTo = now.AddDays(30),
                IsActive = true
            },
            new Coupon
            {
                Code = "SAVE20", DiscountPercentage = 20,
                MaxDiscountAmount = 100, MinOrderAmount = 50,
                UsageLimit = 50, UsedCount = 5,
                ValidFrom = now.AddDays(-3), ValidTo = now.AddDays(14),
                IsActive = true
            },
            new Coupon
            {
                Code = "HALFOFF", DiscountPercentage = 50,
                MaxDiscountAmount = 150, MinOrderAmount = 200,
                UsageLimit = 20, UsedCount = 1,
                ValidFrom = now.AddDays(-1), ValidTo = now.AddDays(7),
                IsActive = true
            }
        });

        await context.SaveChangesAsync();

        // Reviews
        var john = await context.UserProfiles.FirstAsync(u => u.Email == "john@example.com");
        var jane = await context.UserProfiles.FirstAsync(u => u.Email == "jane@example.com");
        var allOffers = await context.Offers.ToListAsync();

        context.Reviews.AddRange(new[]
        {
            new Review { UserId = john.Id, OfferId = allOffers[0].Id, StoreId = bakery.Id, Rating = 5, Comment = "Amazing croissants, so fresh and buttery!" },
            new Review { UserId = jane.Id, OfferId = allOffers[1].Id, StoreId = bakery.Id, Rating = 4, Comment = "Great sourdough, very flavorful." },
            new Review { UserId = john.Id, OfferId = allOffers[4].Id, StoreId = groceries.Id, Rating = 5, Comment = "The fruit basket was incredible value for the price!" },
            new Review { UserId = jane.Id, OfferId = allOffers[9].Id, StoreId = drinks.Id, Rating = 5, Comment = "Best mango smoothie in town!" },
            new Review { UserId = john.Id, OfferId = allOffers[13].Id, StoreId = pizza.Id, Rating = 4, Comment = "Pizza was delicious, great last-minute deal!" },
        });

        await context.SaveChangesAsync();
    }
}
