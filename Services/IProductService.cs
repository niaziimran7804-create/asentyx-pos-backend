using POS.Api.DTOs;

namespace POS.Api.Services
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllProductsAsync(string? searchKey = null);
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto);
        Task<bool> UpdateProductAsync(int id, UpdateProductDto updateProductDto);
        Task<bool> DeleteProductAsync(int id);
        Task<int> GetTotalProductsAsync();
        Task<int> GetAvailableProductsAsync();
        Task<int> GetUnavailableProductsAsync();
        Task<bool> DeductInventoryAsync(int productId, int quantity);
        Task<bool> RestoreInventoryAsync(int productId, int quantity);
    }
}


