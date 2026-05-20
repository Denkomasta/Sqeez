using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sqeez.Api.DTOs;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Controllers
{
    /// <summary>
    /// Exposes runtime system configuration used by the application and administrators.
    /// </summary>
    [Route("api/system-config")]
    public class SystemConfigController : ApiBaseController
    {
        private readonly ISystemConfigService _configService;

        public SystemConfigController(ISystemConfigService configService)
        {
            _configService = configService;
        }

        /// <summary>
        /// Gets the current system configuration.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<SystemConfigDto>> GetConfig()
        {
            var result = await _configService.GetConfigAsync();
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Updates mutable system configuration values. Admin-only.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch]
        public async Task<ActionResult<SystemConfigDto>> UpdateConfig([FromBody] UpdateSystemConfigDto dto)
        {
            var result = await _configService.UpdateConfigAsync(dto);
            return HandleServiceResult(result);
        }
    }
}
