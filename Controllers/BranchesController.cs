using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.DTOs;
using POS.Api.Services;

namespace POS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BranchesController : ControllerBase
    {
        private readonly IBranchService _branchService;

        public BranchesController(IBranchService branchService)
        {
            _branchService = branchService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,CompanyAdmin")]
        public async Task<ActionResult<IEnumerable<BranchDto>>> GetBranches()
        {
            var branches = await _branchService.GetAllBranchesAsync();
            return Ok(branches);
        }

        [HttpGet("company/{companyId}")]
        public async Task<ActionResult<IEnumerable<BranchDto>>> GetBranchesByCompany(int companyId)
        {
            var branches = await _branchService.GetBranchesByCompanyIdAsync(companyId);
            return Ok(branches);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BranchDto>> GetBranch(int id)
        {
            var branch = await _branchService.GetBranchByIdAsync(id);
            if (branch == null)
                return NotFound();

            return Ok(branch);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,CompanyAdmin")]
        public async Task<ActionResult<BranchDto>> CreateBranch([FromBody] CreateBranchDto createBranchDto)
        {
            try
            {
                var branch = await _branchService.CreateBranchAsync(createBranchDto);
                return CreatedAtAction(nameof(GetBranch), new { id = branch.BranchId }, branch);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,CompanyAdmin")]
        public async Task<IActionResult> UpdateBranch(int id, [FromBody] UpdateBranchDto updateBranchDto)
        {
            var result = await _branchService.UpdateBranchAsync(id, updateBranchDto);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,CompanyAdmin")]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            var result = await _branchService.DeleteBranchAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
