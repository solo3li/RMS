using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RMS.Web.Constants;
using RMS.Web.Data;
using RMS.Web.Models;
using System.Text;

namespace RMS.Web.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Customers
        [Authorize(Policy = Permissions.Customers.View)]
        public async Task<IActionResult> Index(string searchString)
        {
            var query = _context.Customers
                .Include(c => c.Orders)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.Name.Contains(searchString) || c.Phone.Contains(searchString));
            }

            var customers = await query
                .Select(c => new CustomerIndexViewModel
                {
                    ID = c.ID,
                    Name = c.Name,
                    Phone = c.Phone,
                    TotalOrders = c.Orders.Count,
                    TotalSpend = c.Orders.Sum(o => o.TotalPrice),
                    Tags = c.Tags
                })
                .ToListAsync();

            ViewData["CurrentFilter"] = searchString;
            return View(customers);
        }

        // GET: Customers/Details/5
        [Authorize(Policy = Permissions.Customers.View)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers
                .Include(c => c.Addresses)
                .Include(c => c.Orders)
                    .ThenInclude(o => o.Branch)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (customer == null) return NotFound();

            var viewModel = new CustomerDetailsViewModel
            {
                Customer = customer,
                TotalOrders = customer.Orders.Count,
                TotalSpend = customer.Orders.Sum(o => o.TotalPrice),
                OrderHistory = customer.Orders.OrderByDescending(o => o.CreatedAt).ToList(),
                Addresses = customer.Addresses.ToList()
            };

            return View(viewModel);
        }

        // GET: Customers/Create
        [Authorize(Policy = Permissions.Customers.Create)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Customers.Create)]
        public async Task<IActionResult> Create([Bind("ID,Name,Phone,Notes,Tags")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(customer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customers/Edit/5
        [Authorize(Policy = Permissions.Customers.Edit)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Customers.Edit)]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Phone,Notes,Tags")] Customer customer)
        {
            if (id != customer.ID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(customer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.ID)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customers/Delete/5
        [Authorize(Policy = Permissions.Customers.Delete)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(m => m.ID == id);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Customers.Delete)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Address Management

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Customers.Edit)]
        public async Task<IActionResult> AddAddress(int customerId, string addressLine, bool isDefault)
        {
            var customer = await _context.Customers.Include(c => c.Addresses).FirstOrDefaultAsync(c => c.ID == customerId);
            if (customer == null) return NotFound();

            if (isDefault)
            {
                foreach (var addr in customer.Addresses) addr.IsDefault = false;
            }

            var address = new CustomerAddress
            {
                CustomerId = customerId,
                AddressLine = addressLine,
                IsDefault = isDefault || !customer.Addresses.Any()
            };

            _context.CustomerAddresses.Add(address);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = customerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Customers.Edit)]
        public async Task<IActionResult> EditAddress(int addressId, string addressLine)
        {
            var address = await _context.CustomerAddresses.FindAsync(addressId);
            if (address == null) return NotFound();

            address.AddressLine = addressLine;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = address.CustomerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Customers.Edit)]
        public async Task<IActionResult> SetDefaultAddress(int addressId)
        {
            var address = await _context.CustomerAddresses.FindAsync(addressId);
            if (address == null) return NotFound();

            var otherAddresses = await _context.CustomerAddresses
                .Where(a => a.CustomerId == address.CustomerId && a.ID != addressId)
                .ToListAsync();

            foreach (var addr in otherAddresses)
            {
                addr.IsDefault = false;
            }

            address.IsDefault = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = address.CustomerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Customers.Edit)]
        public async Task<IActionResult> DeleteAddress(int addressId)
        {
            var address = await _context.CustomerAddresses.FindAsync(addressId);
            if (address == null) return NotFound();

            int customerId = address.CustomerId;
            _context.CustomerAddresses.Remove(address);
            await _context.SaveChangesAsync();

            // If we deleted the default, make another one default if exists
            var remaining = await _context.CustomerAddresses.Where(a => a.CustomerId == customerId).ToListAsync();
            if (remaining.Any() && !remaining.Any(a => a.IsDefault))
            {
                remaining.First().IsDefault = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = customerId });
        }

        [Authorize(Policy = Permissions.Customers.View)]
        public async Task<IActionResult> ExportToCsv()
        {
            var customers = await _context.Customers
                .Include(c => c.Orders)
                .Select(c => new
                {
                    c.Name,
                    c.Phone,
                    TotalOrders = c.Orders.Count,
                    TotalSpend = c.Orders.Sum(o => o.TotalPrice),
                    c.Tags
                })
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Name,Phone,Total Orders,Total Spend,Tags");

            foreach (var c in customers)
            {
                csv.AppendLine($"\"{c.Name}\",\"{c.Phone}\",{c.TotalOrders},{c.TotalSpend},\"{c.Tags}\"");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "customers.csv");
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.ID == id);
        }
    }
}
