using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;

namespace POS.Api.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
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
                ProductSize = p.ProductSize,
                ProductColor = p.ProductColor,
                ProductWeight = p.ProductWeight,
                ProductUnitStock = p.ProductUnitStock,
                BrandName = p.Brand?.BrandName
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
                ProductSize = product.ProductSize,
                ProductColor = product.ProductColor,
                ProductWeight = product.ProductWeight,
                ProductUnitStock = product.ProductUnitStock,
                BrandName = product.Brand?.BrandName
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
                ProductSize = createProductDto.ProductSize,
                ProductColor = createProductDto.ProductColor,
                ProductWeight = createProductDto.ProductWeight,
                ProductUnitStock = createProductDto.ProductUnitStock
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
            product.ProductSize = updateProductDto.ProductSize;
            product.ProductColor = updateProductDto.ProductColor;
            product.ProductWeight = updateProductDto.ProductWeight;
            product.ProductUnitStock = updateProductDto.ProductUnitStock;

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
    }
}


