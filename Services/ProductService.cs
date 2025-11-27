using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;

namespace POS.Api.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public ProductService(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync(string? searchKey = null)
        {
            var query = _context.Products
                .Include(p => p.Brand)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchKey))
            {
                query = query.Where(p =>
                    p.ProductIdTag != null && p.ProductIdTag.Contains(searchKey) ||
                    p.ProductName.Contains(searchKey) ||
                    (p.Brand != null && p.Brand.BrandName.Contains(searchKey)));
            }

            var products = await query.ToListAsync();

            return products.Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                ProductIdTag = p.ProductIdTag,
                ProductName = p.ProductName,
                BrandId = p.BrandId,
                ProductDescription = p.ProductDescription,
                ProductQuantityPerUnit = p.ProductQuantityPerUnit,
                ProductPerUnitPrice = p.ProductPerUnitPrice,
                ProductMSRP = p.ProductMSRP,
                ProductStatus = p.ProductStatus,
                ProductDiscountRate = p.ProductDiscountRate,
              
                ProductUnitStock = p.ProductUnitStock,
                StockThreshold = p.StockThreshold,
                BrandName = p.Brand?.BrandName,
                ProductImageBase64 = p.ProductImage != null ? Convert.ToBase64String(p.ProductImage) : null
            });
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return null;

            return new ProductDto
            {
                ProductId = product.ProductId,
                ProductIdTag = product.ProductIdTag,
                ProductName = product.ProductName,
                BrandId = product.BrandId,
                ProductDescription = product.ProductDescription,
                ProductQuantityPerUnit = product.ProductQuantityPerUnit,
                ProductPerUnitPrice = product.ProductPerUnitPrice,
                ProductMSRP = product.ProductMSRP,
                ProductStatus = product.ProductStatus,
                ProductDiscountRate = product.ProductDiscountRate,
               
                ProductUnitStock = product.ProductUnitStock,
                StockThreshold = product.StockThreshold,
                BrandName = product.Brand?.BrandName,
                ProductImageBase64 = product.ProductImage != null ? Convert.ToBase64String(product.ProductImage) : null
            };
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto)
        {
            var product = new Models.Product
            {
                ProductIdTag = createProductDto.ProductIdTag,
                ProductName = createProductDto.ProductName,
                BrandId = createProductDto.BrandId,
                ProductDescription = createProductDto.ProductDescription,
                ProductQuantityPerUnit = createProductDto.ProductQuantityPerUnit,
                ProductPerUnitPrice = createProductDto.ProductPerUnitPrice,
                ProductMSRP = createProductDto.ProductMSRP,
                ProductStatus = createProductDto.ProductStatus,
                ProductDiscountRate = createProductDto.ProductDiscountRate,
              
                ProductUnitStock = createProductDto.ProductUnitStock,
                StockThreshold = createProductDto.StockThreshold,
                ProductImage = !string.IsNullOrEmpty(createProductDto.ProductImageBase64) 
                    ? Convert.FromBase64String(createProductDto.ProductImageBase64) 
                    : null
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return await GetProductByIdAsync(product.ProductId) ?? new ProductDto();
        }

        public async Task<bool> UpdateProductAsync(int id, UpdateProductDto updateProductDto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return false;

            product.ProductName = updateProductDto.ProductName;
            product.BrandId = updateProductDto.BrandId;
            product.ProductDescription = updateProductDto.ProductDescription;
            product.ProductQuantityPerUnit = updateProductDto.ProductQuantityPerUnit;
            product.ProductPerUnitPrice = updateProductDto.ProductPerUnitPrice;
            product.ProductMSRP = updateProductDto.ProductMSRP;
            product.ProductStatus = updateProductDto.ProductStatus;
            product.ProductDiscountRate = updateProductDto.ProductDiscountRate;
          
            product.ProductUnitStock = updateProductDto.ProductUnitStock;
            product.StockThreshold = updateProductDto.StockThreshold;

            // Only update image if a new one is provided
            if (!string.IsNullOrEmpty(updateProductDto.ProductImageBase64))
            {
                product.ProductImage = Convert.FromBase64String(updateProductDto.ProductImageBase64);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetTotalProductsAsync()
        {
            return await _context.Products.CountAsync();
        }

        public async Task<int> GetAvailableProductsAsync()
        {
            return await _context.Products.CountAsync(p => p.ProductStatus == "YES");
        }

        public async Task<int> GetUnavailableProductsAsync()
        {
            return await _context.Products.CountAsync(p => p.ProductStatus == "NO");
        }

        public async Task<bool> DeductInventoryAsync(int productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return false;

            // Check if sufficient stock is available
            if (product.ProductUnitStock < quantity)
            {
                throw new InvalidOperationException($"Insufficient stock for product '{product.ProductName}'. Available: {product.ProductUnitStock}, Requested: {quantity}");
            }

            // Store previous stock for comparison
            var previousStock = product.ProductUnitStock;

            product.ProductUnitStock -= quantity;

            // Update product status if stock reaches zero
            if (product.ProductUnitStock == 0)
            {
                product.ProductStatus = "NO";
            }

            await _context.SaveChangesAsync();

            // Check if stock has fallen below threshold and send email notification
            if (product.ProductUnitStock <= product.StockThreshold)
            {
                // Stock just crossed the threshold, send alert
                try
                {
                    await _emailService.SendLowStockAlertAsync(product.ProductName, product.ProductUnitStock, product.StockThreshold);
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the inventory deduction
                    System.Diagnostics.Debug.WriteLine($"Failed to send low stock alert for product {product.ProductName}: {ex.Message}");
                }
            }

            return true;
        }

        public async Task<bool> RestoreInventoryAsync(int productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return false;

            product.ProductUnitStock += quantity;

            // Update product status if it was out of stock
            if (product.ProductStatus == "NO" && product.ProductUnitStock > 0)
            {
                product.ProductStatus = "YES";
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}

























