using POS.Api.DTOs;

namespace POS.Api.Services
{
    public interface ICategoryService
    {
        // Main Categories
        Task<IEnumerable<MainCategoryDto>> GetAllMainCategoriesAsync();
        Task<MainCategoryDto?> GetMainCategoryByIdAsync(int id);
        Task<MainCategoryDto> CreateMainCategoryAsync(CreateMainCategoryDto dto);
        Task<bool> UpdateMainCategoryAsync(int id, CreateMainCategoryDto dto);
        Task<bool> DeleteMainCategoryAsync(int id);

        // Second Categories
        Task<IEnumerable<SecondCategoryDto>> GetAllSecondCategoriesAsync();
        Task<SecondCategoryDto?> GetSecondCategoryByIdAsync(int id);
        Task<SecondCategoryDto> CreateSecondCategoryAsync(CreateSecondCategoryDto dto);
        Task<bool> UpdateSecondCategoryAsync(int id, CreateSecondCategoryDto dto);
        Task<bool> DeleteSecondCategoryAsync(int id);

        // Third Categories
        Task<IEnumerable<ThirdCategoryDto>> GetAllThirdCategoriesAsync();
        Task<ThirdCategoryDto?> GetThirdCategoryByIdAsync(int id);
        Task<ThirdCategoryDto> CreateThirdCategoryAsync(CreateThirdCategoryDto dto);
        Task<bool> UpdateThirdCategoryAsync(int id, CreateThirdCategoryDto dto);
        Task<bool> DeleteThirdCategoryAsync(int id);

        // Vendors
        Task<IEnumerable<VendorDto>> GetAllVendorsAsync();
        Task<VendorDto?> GetVendorByIdAsync(int id);
        Task<VendorDto> CreateVendorAsync(CreateVendorDto dto);
        Task<bool> UpdateVendorAsync(int id, CreateVendorDto dto);
        Task<bool> DeleteVendorAsync(int id);

        // Brands
        Task<IEnumerable<BrandDto>> GetAllBrandsAsync();
        Task<BrandDto?> GetBrandByIdAsync(int id);
        Task<BrandDto> CreateBrandAsync(CreateBrandDto dto);
        Task<bool> UpdateBrandAsync(int id, CreateBrandDto dto);
        Task<bool> DeleteBrandAsync(int id);
    }
}


