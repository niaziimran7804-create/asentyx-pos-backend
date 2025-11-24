using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.DTOs;
using POS.Api.Services;

namespace POS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet("main")]
        public async Task<ActionResult<IEnumerable<MainCategoryDto>>> GetMainCategories()
        {
            var categories = await _categoryService.GetAllMainCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("second")]
        public async Task<ActionResult<IEnumerable<SecondCategoryDto>>> GetSecondCategories()
        {
            var categories = await _categoryService.GetAllSecondCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("third")]
        public async Task<ActionResult<IEnumerable<ThirdCategoryDto>>> GetThirdCategories()
        {
            var categories = await _categoryService.GetAllThirdCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("vendors")]
        public async Task<ActionResult<IEnumerable<VendorDto>>> GetVendors()
        {
            var vendors = await _categoryService.GetAllVendorsAsync();
            return Ok(vendors);
        }

        [HttpGet("brands")]
        public async Task<ActionResult<IEnumerable<BrandDto>>> GetBrands()
        {
            var brands = await _categoryService.GetAllBrandsAsync();
            return Ok(brands);
        }

        // Main Category CRUD
        [HttpGet("main/{id}")]
        public async Task<ActionResult<MainCategoryDto>> GetMainCategory(int id)
        {
            var category = await _categoryService.GetMainCategoryByIdAsync(id);
            if (category == null) return NotFound();
            return Ok(category);
        }

        [HttpPost("main")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MainCategoryDto>> CreateMainCategory([FromBody] CreateMainCategoryDto dto)
        {
            var category = await _categoryService.CreateMainCategoryAsync(dto);
            return CreatedAtAction(nameof(GetMainCategory), new { id = category.MainCategoryId }, category);
        }

        [HttpPut("main/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMainCategory(int id, [FromBody] CreateMainCategoryDto dto)
        {
            var result = await _categoryService.UpdateMainCategoryAsync(id, dto);
            if (!result) return NotFound();
            return NoContent();
        }

        [HttpDelete("main/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMainCategory(int id)
        {
            var result = await _categoryService.DeleteMainCategoryAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }

        // Second Category CRUD
        [HttpGet("second/{id}")]
        public async Task<ActionResult<SecondCategoryDto>> GetSecondCategory(int id)
        {
            var category = await _categoryService.GetSecondCategoryByIdAsync(id);
            if (category == null) return NotFound();
            return Ok(category);
        }

        [HttpPost("second")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SecondCategoryDto>> CreateSecondCategory([FromBody] CreateSecondCategoryDto dto)
        {
            var category = await _categoryService.CreateSecondCategoryAsync(dto);
            return CreatedAtAction(nameof(GetSecondCategory), new { id = category.SecondCategoryId }, category);
        }

        [HttpPut("second/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSecondCategory(int id, [FromBody] CreateSecondCategoryDto dto)
        {
            var result = await _categoryService.UpdateSecondCategoryAsync(id, dto);
            if (!result) return NotFound();
            return NoContent();
        }

        [HttpDelete("second/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSecondCategory(int id)
        {
            var result = await _categoryService.DeleteSecondCategoryAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }

        // Third Category CRUD
        [HttpGet("third/{id}")]
        public async Task<ActionResult<ThirdCategoryDto>> GetThirdCategory(int id)
        {
            var category = await _categoryService.GetThirdCategoryByIdAsync(id);
            if (category == null) return NotFound();
            return Ok(category);
        }

        [HttpPost("third")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ThirdCategoryDto>> CreateThirdCategory([FromBody] CreateThirdCategoryDto dto)
        {
            var category = await _categoryService.CreateThirdCategoryAsync(dto);
            return CreatedAtAction(nameof(GetThirdCategory), new { id = category.ThirdCategoryId }, category);
        }

        [HttpPut("third/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateThirdCategory(int id, [FromBody] CreateThirdCategoryDto dto)
        {
            var result = await _categoryService.UpdateThirdCategoryAsync(id, dto);
            if (!result) return NotFound();
            return NoContent();
        }

        [HttpDelete("third/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteThirdCategory(int id)
        {
            var result = await _categoryService.DeleteThirdCategoryAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }

        // Vendor CRUD
        [HttpGet("vendors/{id}")]
        public async Task<ActionResult<VendorDto>> GetVendor(int id)
        {
            var vendor = await _categoryService.GetVendorByIdAsync(id);
            if (vendor == null) return NotFound();
            return Ok(vendor);
        }

        [HttpPost("vendors")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<VendorDto>> CreateVendor([FromBody] CreateVendorDto dto)
        {
            var vendor = await _categoryService.CreateVendorAsync(dto);
            return CreatedAtAction(nameof(GetVendor), new { id = vendor.VendorId }, vendor);
        }

        [HttpPut("vendors/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateVendor(int id, [FromBody] CreateVendorDto dto)
        {
            var result = await _categoryService.UpdateVendorAsync(id, dto);
            if (!result) return NotFound();
            return NoContent();
        }

        [HttpDelete("vendors/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVendor(int id)
        {
            var result = await _categoryService.DeleteVendorAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }

        // Brand CRUD
        [HttpGet("brands/{id}")]
        public async Task<ActionResult<BrandDto>> GetBrand(int id)
        {
            var brand = await _categoryService.GetBrandByIdAsync(id);
            if (brand == null) return NotFound();
            return Ok(brand);
        }

        [HttpPost("brands")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BrandDto>> CreateBrand([FromBody] CreateBrandDto dto)
        {
            var brand = await _categoryService.CreateBrandAsync(dto);
            return CreatedAtAction(nameof(GetBrand), new { id = brand.BrandId }, brand);
        }

        [HttpPut("brands/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBrand(int id, [FromBody] CreateBrandDto dto)
        {
            var result = await _categoryService.UpdateBrandAsync(id, dto);
            if (!result) return NotFound();
            return NoContent();
        }

        [HttpDelete("brands/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var result = await _categoryService.DeleteBrandAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
    }
}


