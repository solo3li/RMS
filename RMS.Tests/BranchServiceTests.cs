using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using RMS.Web.Data;
using RMS.Web.Models;
using RMS.Web.Services;
using System.Security.Claims;
using Xunit;

namespace RMS.Tests
{
    public class BranchServiceTests
    {
        private (ApplicationDbContext db, Mock<UserManager<ApplicationUser>> mockUserManager) GetDependencies()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());

            var db = new ApplicationDbContext(options, mockHttpContextAccessor.Object);

            var store = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);

            return (db, mockUserManager);
        }

        [Fact]
        public async Task GetUserBranchIdsAsync_AdminUser_ReturnsAllBranchIds()
        {
            // Arrange
            var (db, mockUserManager) = GetDependencies();
            db.Branches.AddRange(new List<Branch>
            {
                new Branch { ID = 1, Name = "Branch 1" },
                new Branch { ID = 2, Name = "Branch 2" }
            });
            await db.SaveChangesAsync();

            var service = new BranchService(mockUserManager.Object, db);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Name, "admin")
            }, "Test"));

            // Act
            var result = await service.GetUserBranchIdsAsync(user);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(1, result);
            Assert.Contains(2, result);
        }

        [Fact]
        public async Task GetUserBranchIdsAsync_StaffUser_ReturnsAssignedBranchIds()
        {
            // Arrange
            var (db, mockUserManager) = GetDependencies();
            var branch1 = new Branch { ID = 1, Name = "Branch 1" };
            var branch2 = new Branch { ID = 2, Name = "Branch 2" };
            db.Branches.AddRange(branch1, branch2);
            await db.SaveChangesAsync();

            var staff = new ApplicationUser 
            { 
                UserName = "staff", 
                AssignedBranches = new List<Branch> { branch1 } 
            };

            mockUserManager.Setup(m => m.Users)
                .Returns(new List<ApplicationUser> { staff }.AsQueryable().BuildMock());

            var service = new BranchService(mockUserManager.Object, db);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "staff")
            }, "Test"));

            // Act
            var result = await service.GetUserBranchIdsAsync(user);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0]);
        }

        [Fact]
        public async Task FilterOrdersByBranchAsync_StaffUser_FiltersQuery()
        {
            // Arrange
            var (db, mockUserManager) = GetDependencies();
            var branch1 = new Branch { ID = 1, Name = "Branch 1" };
            var branch2 = new Branch { ID = 2, Name = "Branch 2" };
            db.Branches.AddRange(branch1, branch2);

            db.Orders.AddRange(new List<Order>
            {
                new Order { ID = 101, BranchId = 1 },
                new Order { ID = 102, BranchId = 2 }
            });
            await db.SaveChangesAsync();

            var staff = new ApplicationUser 
            { 
                UserName = "staff", 
                AssignedBranches = new List<Branch> { branch1 } 
            };

            mockUserManager.Setup(m => m.Users)
                .Returns(new List<ApplicationUser> { staff }.AsQueryable().BuildMock());

            var service = new BranchService(mockUserManager.Object, db);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "staff")
            }, "Test"));

            // Act
            var query = db.Orders.AsQueryable();
            var result = await service.FilterOrdersByBranchAsync(user, query);
            var filteredOrders = await result.ToListAsync();

            // Assert
            Assert.Single(filteredOrders);
            Assert.Equal(101, filteredOrders[0].ID);
        }
    }

    // Helper to mock IQueryable
    public static class MockHelper
    {
        public static IQueryable<T> BuildMock<T>(this IQueryable<T> source) where T : class
        {
            var mock = new Mock<IQueryable<T>>();
            mock.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(default))
                .Returns(new TestAsyncEnumerator<T>(source.GetEnumerator()));

            mock.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(source.Provider));

            mock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(source.Expression);
            mock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(source.ElementType);
            mock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(source.GetEnumerator());

            return mock.Object;
        }
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return new ValueTask();
        }
    }

    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(System.Linq.Expressions.Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethods()
                .First(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
                .MakeGenericMethod(expectedResultType)
                .Invoke(_inner, new object[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, new[] { executionResult });
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(System.Linq.Expressions.Expression expression)
            : base(expression)
        { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }
}
