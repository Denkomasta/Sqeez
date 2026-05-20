using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sqeez.Api.Models.Import;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Controllers
{
    /// <summary>
    /// Provides administrative CSV import endpoints.
    /// </summary>
    [Authorize]
    [Route("api/import")]
    public class ImportController : ApiBaseController
    {
        private readonly ICsvImportService _csvImportService;

        public ImportController(ICsvImportService csvImportService)
        {
            _csvImportService = csvImportService;
        }

        /// <summary>
        /// Imports the master CSV file and reports created or skipped records. Admin-only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("master")]
        public async Task<ActionResult<ImportResultDto>> ImportMasterFile(IFormFile file)
        {
            var result = await _csvImportService.ImportMasterFileAsync(file);
            return HandleServiceResult(result);
        }
    }
}
