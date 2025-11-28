using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;
using POS.Api.Models;
using POS.Api.Middleware;

namespace POS.Api.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly TenantContext _tenantContext;

        public CategoryService(ApplicationDbContext context, TenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        public async Task<IEnumerable<MainCategoryDto>> GetAllMainCategoriesAsync()
        {
            // Enforce strict branch isolation - no branchId means no data
            if (!_tenantContext.BranchId.HasValue)
            {
                return Enumerable.Empty<MainCategoryDto>();
            }

            var categories = await _context.MainCategories
                .Where(c => c.BranchId == _tenantContext.BranchId.Value)
                .ToListAsync();
            return categories.Select(c => new MainCategoryDto
            {
                MainCategoryId = c.MainCategoryId,
                MainCategoryName = c.MainCategoryName,
                MainCategoryDescription = c.MainCategoryDescription
            });
        }

        public async Task<IEnumerable<SecondCategoryDto>> GetAllSecondCategoriesAsync()
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return Enumerable.Empty<SecondCategoryDto>();
            }

            var categories = await _context.SecondCategories
                .Include(sc => sc.MainCategory)
                .Where(c => c.BranchId == _tenantContext.BranchId.Value)
                .ToListAsync();
            return categories.Select(c => new SecondCategoryDto
            {
                SecondCategoryId = c.SecondCategoryId,
                MainCategoryId = c.MainCategoryId,
                SecondCategoryName = c.SecondCategoryName,
                SecondCategoryDescription = c.SecondCategoryDescription,
                MainCategoryName = c.MainCategory?.MainCategoryName
            });
        }

        public async Task<IEnumerable<ThirdCategoryDto>> GetAllThirdCategoriesAsync()
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return Enumerable.Empty<ThirdCategoryDto>();
            }

            var categories = await _context.ThirdCategories
                .Include(tc => tc.SecondCategory)
                .Where(c => c.BranchId == _tenantContext.BranchId.Value)
                .ToListAsync();
            return categories.Select(c => new ThirdCategoryDto
            {
                ThirdCategoryId = c.ThirdCategoryId,
                SecondCategoryId = c.SecondCategoryId,
                ThirdCategoryName = c.ThirdCategoryName,
                ThirdCategoryDescription = c.ThirdCategoryDescription,
                SecondCategoryName = c.SecondCategory?.SecondCategoryName
            });
        }

        public async Task<IEnumerable<VendorDto>> GetAllVendorsAsync()
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return Enumerable.Empty<VendorDto>();
            }

            var vendors = await _context.Vendors
                .Include(v => v.ThirdCategory)
                .Where(v => v.BranchId == _tenantContext.BranchId.Value)
                .ToListAsync();
            return vendors.Select(v => new VendorDto
            {
                VendorId = v.VendorId,
                VendorTag = v.VendorTag,
                VendorName = v.VendorName,
                ThirdCategoryId = v.ThirdCategoryId,
                VendorDescription = v.VendorDescription,
                VendorStatus = v.VendorStatus,
                RegisterDate = v.RegisterDate,
                ThirdCategoryName = v.ThirdCategory?.ThirdCategoryName
            });
        }

        public async Task<IEnumerable<BrandDto>> GetAllBrandsAsync()
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return Enumerable.Empty<BrandDto>();
            }

            var brands = await _context.Brands
                .Include(b => b.Vendor)
                .Where(b => b.BranchId == _tenantContext.BranchId.Value)
                .ToListAsync();
            return brands.Select(b => new BrandDto
            {
                BrandId = b.BrandId,
                BrandTag = b.BrandTag,
                BrandName = b.BrandName,
                VendorId = b.VendorId,
                BrandDescription = b.BrandDescription,
                BrandStatus = b.BrandStatus,
                VendorName = b.Vendor?.VendorName
            });
        }

        // Main Category CRUD
        public async Task<MainCategoryDto?> GetMainCategoryByIdAsync(int id)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return null;
            }

            var category = await _context.MainCategories
                .FirstOrDefaultAsync(c => c.MainCategoryId == id && c.BranchId == _tenantContext.BranchId.Value);
            if (category == null) return null;
            return new MainCategoryDto
            {
                MainCategoryId = category.MainCategoryId,
                MainCategoryName = category.MainCategoryName,
                MainCategoryDescription = category.MainCategoryDescription
            };
        }

        public async Task<MainCategoryDto> CreateMainCategoryAsync(CreateMainCategoryDto dto)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                throw new InvalidOperationException("Cannot create category without branch context. User must be assigned to a branch.");
            }

            var category = new MainCategory
            {
                MainCategoryName = dto.MainCategoryName,
                MainCategoryDescription = dto.MainCategoryDescription,
                CompanyId = _tenantContext.CompanyId,
                BranchId = _tenantContext.BranchId.Value
            };
            _context.MainCategories.Add(category);
            await _context.SaveChangesAsync();
            return new MainCategoryDto
            {
                MainCategoryId = category.MainCategoryId,
                MainCategoryName = category.MainCategoryName,
                MainCategoryDescription = category.MainCategoryDescription
            };
        }

        public async Task<bool> UpdateMainCategoryAsync(int id, CreateMainCategoryDto dto)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return false;
            }

            var category = await _context.MainCategories
                .FirstOrDefaultAsync(c => c.MainCategoryId == id && c.BranchId == _tenantContext.BranchId.Value);
            if (category == null) return false;
            category.MainCategoryName = dto.MainCategoryName;
            category.MainCategoryDescription = dto.MainCategoryDescription;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMainCategoryAsync(int id)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return false;
            }

            var category = await _context.MainCategories
                .FirstOrDefaultAsync(c => c.MainCategoryId == id && c.BranchId == _tenantContext.BranchId.Value);
            if (category == null) return false;
            _context.MainCategories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }

        // Second Category CRUD
        public async Task<SecondCategoryDto?> GetSecondCategoryByIdAsync(int id)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return null;
            }

            var category = await _context.SecondCategories
                .Include(sc => sc.MainCategory)
                .FirstOrDefaultAsync(sc => sc.SecondCategoryId == id && sc.BranchId == _tenantContext.BranchId.Value);
            if (category == null) return null;
            return new SecondCategoryDto
            {
                SecondCategoryId = category.SecondCategoryId,
                MainCategoryId = category.MainCategoryId,
                SecondCategoryName = category.SecondCategoryName,
                SecondCategoryDescription = category.SecondCategoryDescription,
                MainCategoryName = category.MainCategory?.MainCategoryName
            };
        }

        public async Task<SecondCategoryDto> CreateSecondCategoryAsync(CreateSecondCategoryDto dto)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                throw new InvalidOperationException("Cannot create category without branch context. User must be assigned to a branch.");
            }

            var category = new SecondCategory
            {
                MainCategoryId = dto.MainCategoryId,
                SecondCategoryName = dto.SecondCategoryName,
                SecondCategoryDescription = dto.SecondCategoryDescription,
                CompanyId = _tenantContext.CompanyId,
                BranchId = _tenantContext.BranchId.Value
            };
            _context.SecondCategories.Add(category);
            await _context.SaveChangesAsync();
            var mainCategory = await _context.MainCategories.FindAsync(dto.MainCategoryId);
            return new SecondCategoryDto
            {
                SecondCategoryId = category.SecondCategoryId,
                MainCategoryId = category.MainCategoryId,
                SecondCategoryName = category.SecondCategoryName,
                SecondCategoryDescription = category.SecondCategoryDescription,
                MainCategoryName = mainCategory?.MainCategoryName
            };
        }

        public async Task<bool> UpdateSecondCategoryAsync(int id, CreateSecondCategoryDto dto)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return false;
            }

            var category = await _context.SecondCategories
                .FirstOrDefaultAsync(c => c.SecondCategoryId == id && c.BranchId == _tenantContext.BranchId.Value);
            if (category == null) return false;
            category.MainCategoryId = dto.MainCategoryId;
            category.SecondCategoryName = dto.SecondCategoryName;
            category.SecondCategoryDescription = dto.SecondCategoryDescription;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSecondCategoryAsync(int id)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return false;
            }

            var category = await _context.SecondCategories
                .FirstOrDefaultAsync(c => c.SecondCategoryId == id && c.BranchId == _tenantContext.BranchId.Value);
            if (category == null) return false;
            _context.SecondCategories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }

        // Third Category CRUD
        public async Task<ThirdCategoryDto?> GetThirdCategoryByIdAsync(int id)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return null;
            }

            var category = await _context.ThirdCategories
                .Include(tc => tc.SecondCategory)
                .FirstOrDefaultAsync(tc => tc.ThirdCategoryId == id && tc.BranchId == _tenantContext.BranchId.Value);
            if (category == null) return null;
            return new ThirdCategoryDto
            {
                ThirdCategoryId = category.ThirdCategoryId,
                SecondCategoryId = category.SecondCategoryId,
                ThirdCategoryName = category.ThirdCategoryName,
                ThirdCategoryDescription = category.ThirdCategoryDescription,
                SecondCategoryName = category.SecondCategory?.SecondCategoryName
            };
        }

        public async Task<ThirdCategoryDto> CreateThirdCategoryAsync(CreateThirdCategoryDto dto)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                throw new InvalidOperationException("Cannot create category without branch context. User must be assigned to a branch.");
            }

            var category = new ThirdCategory
            {
                SecondCategoryId = dto.SecondCategoryId,
                ThirdCategoryName = dto.ThirdCategoryName,
                ThirdCategoryDescription = dto.ThirdCategoryDescription,
                CompanyId = _tenantContext.CompanyId,
                BranchId = _tenantContext.BranchId.Value
            };
            _context.ThirdCategories.Add(category);
            await _context.SaveChangesAsync();
            var secondCategory = await _context.SecondCategories.FindAsync(dto.SecondCategoryId);
            return new ThirdCategoryDto
            {
                ThirdCategoryId = category.ThirdCategoryId,
                SecondCategoryId = category.SecondCategoryId,
                ThirdCategoryName = category.ThirdCategoryName,
                ThirdCategoryDescription = category.ThirdCategoryDescription,
                SecondCategoryName = secondCategory?.SecondCategoryName
            };
        }

        public async Task<bool> UpdateThirdCategoryAsync(int id, CreateThirdCategoryDto dto)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return false;
            }

            var category = await _context.ThirdCategories
                .FirstOrDefaultAsync(c => c.ThirdCategoryId == id && c.BranchId == _tenantContext.BranchId.Value);
            if (category == null) return false;
            category.SecondCategoryId = dto.SecondCategoryId;
            category.ThirdCategoryName = dto.ThirdCategoryName;
            category.ThirdCategoryDescription = dto.ThirdCategoryDescription;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteThirdCategoryAsync(int id)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return false;
            }

            var category = await _context.ThirdCategories
                .FirstOrDefaultAsync(c => c.ThirdCategoryId == id && c.BranchId == _tenantContext.BranchId.Value);
            if (category == null) return false;
            _context.ThirdCategories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }

        // Vendor CRUD
        public async Task<VendorDto?> GetVendorByIdAsync(int id)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return null;
            }

            var vendor = await _context.Vendors
                .Include(v => v.ThirdCategory)
                .FirstOrDefaultAsync(v => v.VendorId == id && v.BranchId == _tenantContext.BranchId.Value);
            if (vendor == null) return null;
            return new VendorDto
            {
                VendorId = vendor.VendorId,
                VendorTag = vendor.VendorTag,
                VendorName = vendor.VendorName,
                ThirdCategoryId = vendor.ThirdCategoryId,
                VendorDescription = vendor.VendorDescription,
                VendorStatus = vendor.VendorStatus,
                RegisterDate = vendor.RegisterDate,
                ThirdCategoryName = vendor.ThirdCategory?.ThirdCategoryName
            };
        }

        public async Task<VendorDto> CreateVendorAsync(CreateVendorDto dto)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                throw new InvalidOperationException("Cannot create vendor without branch context. User must be assigned to a branch.");
            }

            var vendor = new Vendor
            {
                VendorTag = dto.VendorTag,
                VendorName = dto.VendorName,
                ThirdCategoryId = dto.ThirdCategoryId,
                VendorDescription = dto.VendorDescription,
                VendorStatus = dto.VendorStatus,
                RegisterDate = DateTime.UtcNow,
                CompanyId = _tenantContext.CompanyId,
                BranchId = _tenantContext.BranchId.Value
            };
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();
            var thirdCategory = await _context.ThirdCategories.FindAsync(dto.ThirdCategoryId);
            return new VendorDto
            {
                VendorId = vendor.VendorId,
                VendorTag = vendor.VendorTag,
                VendorName = vendor.VendorName,
                ThirdCategoryId = vendor.ThirdCategoryId,
                VendorDescription = vendor.VendorDescription,
                VendorStatus = vendor.VendorStatus,
                RegisterDate = vendor.RegisterDate,
                ThirdCategoryName = thirdCategory?.ThirdCategoryName
            };
        }

        public async Task<bool> UpdateVendorAsync(int id, CreateVendorDto dto)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return false;
            }

            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(v => v.VendorId == id && v.BranchId == _tenantContext.BranchId.Value);
            if (vendor == null) return false;
            vendor.VendorTag = dto.VendorTag;
            vendor.VendorName = dto.VendorName;
            vendor.ThirdCategoryId = dto.ThirdCategoryId;
            vendor.VendorDescription = dto.VendorDescription;
            vendor.VendorStatus = dto.VendorStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteVendorAsync(int id)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return false;
            }

            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(v => v.VendorId == id && v.BranchId == _tenantContext.BranchId.Value);
            if (vendor == null) return false;
            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();
            return true;
        }

        // Brand CRUD
        public async Task<BrandDto?> GetBrandByIdAsync(int id)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return null;
            }

            var brand = await _context.Brands
                .Include(b => b.Vendor)
                .FirstOrDefaultAsync(b => b.BrandId == id && b.BranchId == _tenantContext.BranchId.Value);
            if (brand == null) return null;
            return new BrandDto
            {
                BrandId = brand.BrandId,
                BrandTag = brand.BrandTag,
                BrandName = brand.BrandName,
                VendorId = brand.VendorId,
                BrandDescription = brand.BrandDescription,
                BrandStatus = brand.BrandStatus,
                VendorName = brand.Vendor?.VendorName
            };
        }

        public async Task<BrandDto> CreateBrandAsync(CreateBrandDto dto)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                throw new InvalidOperationException("Cannot create brand without branch context. User must be assigned to a branch.");
            }

            var brand = new Brand
            {
                BrandTag = dto.BrandTag,
                BrandName = dto.BrandName,
                VendorId = dto.VendorId,
                BrandDescription = dto.BrandDescription,
                BrandStatus = dto.BrandStatus,
                CompanyId = _tenantContext.CompanyId,
                BranchId = _tenantContext.BranchId.Value
            };
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();
            var vendor = await _context.Vendors.FindAsync(dto.VendorId);
            return new BrandDto
            {
                BrandId = brand.BrandId,
                BrandTag = brand.BrandTag,
                BrandName = brand.BrandName,
                VendorId = brand.VendorId,
                BrandDescription = brand.BrandDescription,
                BrandStatus = brand.BrandStatus,
                VendorName = vendor?.VendorName
            };
        }

        public async Task<bool> UpdateBrandAsync(int id, CreateBrandDto dto)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return false;
            }

            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.BrandId == id && b.BranchId == _tenantContext.BranchId.Value);
            if (brand == null) return false;
            brand.BrandTag = dto.BrandTag;
            brand.BrandName = dto.BrandName;
            brand.VendorId = dto.VendorId;
            brand.BrandDescription = dto.BrandDescription;
            brand.BrandStatus = dto.BrandStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteBrandAsync(int id)
        {
            // Enforce strict branch isolation
            if (!_tenantContext.BranchId.HasValue)
            {
                return false;
            }

            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.BrandId == id && b.BranchId == _tenantContext.BranchId.Value);
            if (brand == null) return false;
            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}


