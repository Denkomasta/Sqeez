using Microsoft.Extensions.Caching.Memory;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Models.System;
using Sqeez.Api.Services.Interfaces;

namespace Sqeez.Api.Services
{
    /// <summary>
    /// Reads and updates mutable application system configuration.
    /// </summary>
    public class SystemConfigService : BaseService<SystemConfigService>, ISystemConfigService
    {
        // uses RAM as there exists only one instance of System Config
        private readonly IMemoryCache _cache;
        private const string ConfigCacheKey = "GlobalSystemConfig";

        public SystemConfigService(SqeezDbContext context, ILogger<SystemConfigService> logger, IMemoryCache cache)
            : base(context, logger)
        {
            _cache = cache;
        }

        public async Task<ServiceResult<SystemConfigDto>> GetConfigAsync()
        {
            if (_cache.TryGetValue(ConfigCacheKey, out SystemConfigDto? cachedConfig))
            {
                return ServiceResult<SystemConfigDto>.Ok(cachedConfig!);
            }

            var config = await _context.Set<SystemConfig>().FindAsync(1);

            if (config == null)
            {
                config = new SystemConfig { Id = 1 };
                _context.Add(config);
                await _context.SaveChangesAsync();
            }

            var dto = MapToDto(config);

            _cache.Set(ConfigCacheKey, dto, new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.NeverRemove
            });

            //_cache.Set(ConfigCacheKey, dto, new MemoryCacheEntryOptions
            //{
            //    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            //});

            return ServiceResult<SystemConfigDto>.Ok(dto);
        }

        public async Task<ServiceResult<SystemConfigDto>> UpdateConfigAsync(UpdateSystemConfigDto dto)
        {
            // Always fetch Id = 1
            var config = await _context.Set<SystemConfig>().FindAsync(1);

            if (config == null)
            {
                config = new SystemConfig { Id = 1 };
                _context.Add(config);
            }

            if (dto.SchoolName != null) config.SchoolName = dto.SchoolName;
            if (dto.LogoUrl != null) config.LogoUrl = dto.LogoUrl;
            if (dto.SupportEmail != null) config.SupportEmail = dto.SupportEmail;
            if (dto.DefaultLanguage != null) config.DefaultLanguage = dto.DefaultLanguage;
            if (dto.CurrentAcademicYear != null) config.CurrentAcademicYear = dto.CurrentAcademicYear;

            if (dto.AllowPublicRegistration.HasValue) config.AllowPublicRegistration = dto.AllowPublicRegistration.Value;
            if (dto.RequireEmailVerification.HasValue) config.RequireEmailVerification = dto.RequireEmailVerification.Value;
            if (dto.MaxAvatarAndBadgeUploadSizeMB.HasValue) config.MaxAvatarAndBadgeUploadSizeMB = dto.MaxAvatarAndBadgeUploadSizeMB.Value;
            if (dto.MaxQuizMediaUploadSizeMB.HasValue) config.MaxQuizMediaUploadSizeMB = dto.MaxQuizMediaUploadSizeMB.Value;
            if (dto.MaxActiveSessionsPerUser.HasValue) config.MaxActiveSessionsPerUser = dto.MaxActiveSessionsPerUser.Value;

            await _context.SaveChangesAsync();

            var updatedDto = MapToDto(config);

            _cache.Set(ConfigCacheKey, updatedDto, new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.NeverRemove
            });

            return ServiceResult<SystemConfigDto>.Ok(updatedDto);
        }

        private static SystemConfigDto MapToDto(SystemConfig config)
        {
            return new SystemConfigDto(
                config.SchoolName, config.LogoUrl, config.SupportEmail, config.DefaultLanguage,
                config.CurrentAcademicYear, config.AllowPublicRegistration,
                config.RequireEmailVerification,
                config.MaxAvatarAndBadgeUploadSizeMB,
                config.MaxQuizMediaUploadSizeMB,
                config.MaxActiveSessionsPerUser
            );
        }
    }
}
