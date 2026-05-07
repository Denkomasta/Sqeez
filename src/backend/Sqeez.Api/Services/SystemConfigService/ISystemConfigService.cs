using Sqeez.Api.DTOs;

namespace Sqeez.Api.Services.Interfaces
{
    /// <summary>
    /// Defines application system configuration retrieval and update operations.
    /// </summary>
    public interface ISystemConfigService
    {
        /// <summary>
        /// Gets the singleton system configuration, creating the default row when it does not yet exist.
        /// </summary>
        /// <returns>The cached or persisted system configuration DTO.</returns>
        Task<ServiceResult<SystemConfigDto>> GetConfigAsync();

        /// <summary>
        /// Patches the singleton system configuration and refreshes the in-memory cache.
        /// </summary>
        /// <param name="dto">Configuration values to update; null values leave existing settings unchanged.</param>
        /// <returns>The updated system configuration DTO.</returns>
        Task<ServiceResult<SystemConfigDto>> UpdateConfigAsync(UpdateSystemConfigDto dto);
    }
}
