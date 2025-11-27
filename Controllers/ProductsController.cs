using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.DTOs;
using POS.Api.Services;

namespace POS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts([FromQuery] string? search = null)
        {
            var products = await _productService.GetAllProductsAsync(search);
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            return Ok(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto createProductDto, [FromForm] IFormFile? image = null)
        {
            // Handle image file upload if provided
            if (image != null && image.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await image.CopyToAsync(memoryStream);
                    createProductDto.ProductImageBase64 = Convert.ToBase64String(memoryStream.ToArray());
                }
            }

            var product = await _productService.CreateProductAsync(createProductDto);
            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto updateProductDto, [FromForm] IFormFile? image = null)
        {
            // Handle image file upload if provided
            if (image != null && image.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await image.CopyToAsync(memoryStream);
                    updateProductDto.ProductImageBase64 = Convert.ToBase64String(memoryStream.ToArray());
                }
            }

            var result = await _productService.UpdateProductAsync(id, updateProductDto);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteProductAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpGet("stats/total")]
        public async Task<ActionResult<int>> GetTotalProducts()
        {
            var count = await _productService.GetTotalProductsAsync();
            return Ok(count);
        }

        [HttpGet("stats/available")]
        public async Task<ActionResult<int>> GetAvailableProducts()
        {
            var count = await _productService.GetAvailableProductsAsync();
            return Ok(count);
        }

        [HttpGet("stats/unavailable")]
        public async Task<ActionResult<int>> GetUnavailableProducts()
        {
            var count = await _productService.GetUnavailableProductsAsync();
            return Ok(count);
        }
    }
}


