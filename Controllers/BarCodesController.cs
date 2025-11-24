using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Api.Data;
using POS.Api.DTOs;
using POS.Api.Models;

namespace POS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BarCodesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BarCodesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BarCodeDto>>> GetBarCodes()
        {
            var barCodes = await _context.BarCodes.ToListAsync();
            return Ok(barCodes.Select(b => new BarCodeDto
            {
                BarCodeId = b.BarCodeId,
                BarCode1 = b.BarCode1
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BarCodeDto>> GetBarCode(int id)
        {
            var barCode = await _context.BarCodes.FindAsync(id);
            if (barCode == null)
                return NotFound();

            return Ok(new BarCodeDto
            {
                BarCodeId = barCode.BarCodeId,
                BarCode1 = barCode.BarCode1
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Cashier")]
        public async Task<ActionResult<BarCodeDto>> CreateBarCode([FromBody] CreateBarCodeDto dto)
        {
            var barCode = new BarCode
            {
                BarCode1 = dto.BarCode1
            };

            _context.BarCodes.Add(barCode);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBarCode), new { id = barCode.BarCodeId }, new BarCodeDto
            {
                BarCodeId = barCode.BarCodeId,
                BarCode1 = barCode.BarCode1
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBarCode(int id, [FromBody] CreateBarCodeDto dto)
        {
            var barCode = await _context.BarCodes.FindAsync(id);
            if (barCode == null)
                return NotFound();

            barCode.BarCode1 = dto.BarCode1;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBarCode(int id)
        {
            var barCode = await _context.BarCodes.FindAsync(id);
            if (barCode == null)
                return NotFound();

            _context.BarCodes.Remove(barCode);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("generate")]
        [Authorize(Roles = "Admin,Cashier")]
        public async Task<ActionResult<BarCodeDto>> GenerateBarCode([FromBody] GenerateBarCodeDto dto)
        {
            // Generate barcode based on product ID or custom value
            string barcodeValue = dto.Value ?? Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
            
            var barCode = new BarCode
            {
                BarCode1 = barcodeValue
            };

            _context.BarCodes.Add(barCode);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBarCode), new { id = barCode.BarCodeId }, new BarCodeDto
            {
                BarCodeId = barCode.BarCodeId,
                BarCode1 = barCode.BarCode1
            });
        }
    }

    public class BarCodeDto
    {
        public int BarCodeId { get; set; }
        public string BarCode1 { get; set; } = string.Empty;
    }

    public class CreateBarCodeDto
    {
        public string BarCode1 { get; set; } = string.Empty;
    }

    public class GenerateBarCodeDto
    {
        public string? Value { get; set; }
        public int? ProductId { get; set; }
    }
}

