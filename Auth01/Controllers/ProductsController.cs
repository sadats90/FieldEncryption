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

        private (int userId, bool isAdmin) GetUserContext()
        {
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
            bool isAdmin = User.FindFirstValue(ClaimTypes.Role) == "Admin";
            return (userId, isAdmin);
        }

        private async Task<Product?> GetOwnedProductAsync(int id, int userId) =>
            await _context.Products.Include(p => p.CreatedBy)
                                   .FirstOrDefaultAsync(p => p.Id == id && p.CreatedById == userId);

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var (userId, isAdmin) = GetUserContext();
            if (userId == 0) return Unauthorized();

            // Materialize first to avoid EF translation issues
            var products = await _context.Products
                .Include(p => p.CreatedBy)
                .Where(p => isAdmin || p.CreatedById == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var productViewModels = products.Select(p => new ProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = (p.CreatedById == userId || isAdmin)
                    ? _encryptionService.Decrypt(p.Description ?? string.Empty,
                        _vaultService.GetOrCreateUserKey(p.CreatedById))
                    : "********",
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                CreatedByName = p.CreatedBy != null
                    ? $"{p.CreatedBy.FirstName} {p.CreatedBy.LastName}"
                    : "Unknown"
            }).ToList();

            return View(productViewModels);
        }

        // GET: Products/Create
        public IActionResult Create() => View();

        // POST: Products/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductViewModel viewModel)
        {
            if (!ModelState.IsValid) return View(viewModel);

            var (userId, _) = GetUserContext();
            if (userId == 0) return Unauthorized();

            var encryptedDescription = _encryptionService.Encrypt(viewModel.Description ?? string.Empty,
                _vaultService.GetOrCreateUserKey(userId));

            _context.Products.Add(new Product
            {
                Name = viewModel.Name,
                Description = encryptedDescription,
                Price = viewModel.Price,
                StockQuantity = viewModel.StockQuantity,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Product created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var (userId, _) = GetUserContext();
            var product = await GetOwnedProductAsync(id, userId);
            if (product == null) return NotFound();

            return View(new EditProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = _encryptionService.Decrypt(product.Description ?? string.Empty,
                    _vaultService.GetOrCreateUserKey(userId)),
                Price = product.Price,
                StockQuantity = product.StockQuantity
            });
        }

        // POST: Products/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProductViewModel viewModel)
        {
            var (userId, _) = GetUserContext();
            var product = await GetOwnedProductAsync(viewModel.Id, userId);
            if (product == null) return NotFound();

            if (!ModelState.IsValid) return View(viewModel);

            product.Name = viewModel.Name;
            product.Description = _encryptionService.Encrypt(viewModel.Description ?? string.Empty,
                _vaultService.GetOrCreateUserKey(userId));
            product.Price = viewModel.Price;
            product.StockQuantity = viewModel.StockQuantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Product updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var (userId, _) = GetUserContext();
            var product = await GetOwnedProductAsync(id, userId);
            if (product == null) return NotFound();

            return View(new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = _encryptionService.Decrypt(product.Description ?? string.Empty,
                    _vaultService.GetOrCreateUserKey(userId)),
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                CreatedByName = product.CreatedBy != null
                    ? $"{product.CreatedBy.FirstName} {product.CreatedBy.LastName}"
                    : "Unknown"
            });
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (userId, _) = GetUserContext();
            var product = await GetOwnedProductAsync(id, userId);
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
