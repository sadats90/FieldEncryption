using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Auth01.Data;
using Auth01.Models;
using Auth01.Services;
using System.Security.Claims;

namespace Auth01.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;
        private readonly EncryptionService _encryptionService;
        private readonly DummyVaultService _vaultService;

        public ProductsController(
            ApplicationDbContext context,
            ILogger<ProductsController> logger,
            EncryptionService encryptionService,
            DummyVaultService vaultService)
        {
            _context = context;
            _logger = logger;
            _encryptionService = encryptionService;
            _vaultService = vaultService;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId)) return Unauthorized();

            var userRole = User.FindFirstValue(ClaimTypes.Role);
            bool isAdmin = userRole == "Admin";

            IQueryable<Product> query = _context.Products.Include(p => p.CreatedBy);
            if (!isAdmin) query = query.Where(p => p.CreatedById == userId);

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = (p.CreatedById == userId || isAdmin)
                        ? _encryptionService.Decrypt(p.Description ?? string.Empty, _vaultService.GetOrCreateUserKey(p.CreatedById))
                        : "********",
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    CreatedByName = p.CreatedBy != null ? $"{p.CreatedBy.FirstName} {p.CreatedBy.LastName}" : "Unknown"
                })
                .ToListAsync();

            return View(products);
        }

        // GET: Products/Create
        public IActionResult Create() => View();

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,StockQuantity")] CreateProductViewModel viewModel)
        {
            if (!ModelState.IsValid) return View(viewModel);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId)) return Unauthorized();

            var userKey = _vaultService.GetOrCreateUserKey(userId);
            var encryptedDescription = _encryptionService.Encrypt(viewModel.Description ?? string.Empty, userKey);

            var product = new Product
            {
                Name = viewModel.Name,
                Description = encryptedDescription,
                Price = viewModel.Price,
                StockQuantity = viewModel.StockQuantity,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Product created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId)) return Unauthorized();

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.CreatedById == userId);
            if (product == null) return NotFound();

            var decryptedDescription = _encryptionService.Decrypt(product.Description ?? string.Empty, _vaultService.GetOrCreateUserKey(userId));

            var viewModel = new EditProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = decryptedDescription,
                Price = product.Price,
                StockQuantity = product.StockQuantity
            };

            return View(viewModel);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,StockQuantity")] EditProductViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();
            if (!ModelState.IsValid) return View(viewModel);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId)) return Unauthorized();

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.CreatedById == userId);
            if (product == null) return NotFound();

            product.Name = viewModel.Name;
            product.Description = _encryptionService.Encrypt(viewModel.Description ?? string.Empty, _vaultService.GetOrCreateUserKey(userId));
            product.Price = viewModel.Price;
            product.StockQuantity = viewModel.StockQuantity;
            product.UpdatedAt = DateTime.UtcNow;

            _context.Update(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Product updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId)) return Unauthorized();

            var product = await _context.Products
                .Include(p => p.CreatedBy)
                .FirstOrDefaultAsync(p => p.Id == id && p.CreatedById == userId);

            if (product == null) return NotFound();

            var viewModel = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = _encryptionService.Decrypt(product.Description ?? string.Empty, _vaultService.GetOrCreateUserKey(userId)),
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                CreatedByName = product.CreatedBy != null ? $"{product.CreatedBy.FirstName} {product.CreatedBy.LastName}" : "Unknown"
            };

            return View(viewModel);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId)) return Unauthorized();

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.CreatedById == userId);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Product deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
