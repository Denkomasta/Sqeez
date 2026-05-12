using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Media;
using Sqeez.Api.Models.Users;
using Sqeez.Api.Services;
using Sqeez.Api.Services.Interfaces;
using Sqeez.Api.Services.TokenService;

namespace Sqeez.Api.Tests.Services
{
    public class MediaAssetServiceTests
    {
        private async Task<SqeezDbContext> GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<SqeezDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new SqeezDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return context;
        }

        private MediaAssetService CreateService(SqeezDbContext context)
        {
            var mockLogger = new Mock<ILogger<MediaAssetService>>();
            var mockFileService = new Mock<IFileStorageService>();
            return new MediaAssetService(context, mockLogger.Object, mockFileService.Object);
        }

        [Fact]
        public async Task GetMediaAssetByIdAsync_WhenExists_ReturnsDto()
        {
            var context = await GetInMemoryDbContext();

            // Setup a teacher to satisfy the Owner relationship
            var teacher = new Teacher { Username = "Mr. Snaps", Role = UserRole.Teacher };
            context.Teachers.Add(teacher);

            var asset = new MediaAsset
            {
                LocationUrl = "https://s3.amazonaws.com/image.png",
                MimeType = MediaType.Image,
                IsPrivate = false,
                Owner = teacher
            };
            context.MediaAssets.Add(asset);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetMediaAssetByIdAsync(asset.Id);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("https://s3.amazonaws.com/image.png", result.Data.LocationUrl);
            Assert.Equal("Mr. Snaps", result.Data.OwnerUsername);
        }

        [Fact]
        public async Task GetAllMediaAssetsAsync_WithOwnerFilter_ReturnsFilteredResults()
        {
            var context = await GetInMemoryDbContext();

            var teacher1 = new Teacher { Username = "Teacher1", Role = UserRole.Teacher };
            var teacher2 = new Teacher { Username = "Teacher2", Role = UserRole.Teacher };
            context.Teachers.AddRange(teacher1, teacher2);

            context.MediaAssets.AddRange(
                new MediaAsset { LocationUrl = "url1", Owner = teacher1 },
                new MediaAsset { LocationUrl = "url2", Owner = teacher1 },
                new MediaAsset { LocationUrl = "url3", Owner = teacher2 }
            );
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var filter = new MediaAssetFilterDto { OwnerId = teacher1.Id };
            var result = await service.GetAllMediaAssetsAsync(filter);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.TotalCount);
            Assert.All(result.Data.Data, a => Assert.Equal(teacher1.Id, a.OwnerId));
        }

        [Fact]
        public async Task CreateMediaAssetAsync_WhenValidOwner_CreatesAsset()
        {
            var context = await GetInMemoryDbContext();
            var teacher = new Teacher { Username = "ValidOwner", Role = UserRole.Teacher };
            context.Teachers.Add(teacher);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var dto = new CreateMediaAssetDto(
                "https://domain.com/video.mp4",
                MediaType.Video,
                true,
                teacher.Id,
                "A private video"
            );

            var result = await service.CreateMediaAssetAsync(dto);

            Assert.True(result.Success);
            Assert.Equal("https://domain.com/video.mp4", result.Data!.LocationUrl);
            Assert.Equal("ValidOwner", result.Data.OwnerUsername);

            var dbAsset = await context.MediaAssets.FindAsync(result.Data.Id);
            Assert.NotNull(dbAsset);
            Assert.Equal(teacher.Id, dbAsset.OwnerId);
        }

        [Fact]
        public async Task CreateMediaAssetAsync_WhenInvalidOwner_ReturnsNotFound()
        {
            var context = await GetInMemoryDbContext();
            var service = CreateService(context);

            // 999 is a Teacher ID that does not exist in the in-memory database
            var dto = new CreateMediaAssetDto("url", (MediaType)1, false, 999);

            var result = await service.CreateMediaAssetAsync(dto);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.NotFound, result.ErrorCode);
            Assert.Contains("does not exist", result.ErrorMessage);
        }

        [Fact]
        public async Task ReassignMediaAssetOwnerAsync_WhenValidOwner_TransfersOwnership()
        {
            var context = await GetInMemoryDbContext();
            var originalOwner = new Teacher { Username = "Original", Role = UserRole.Teacher };
            var replacementOwner = new Teacher { Username = "Replacement", Role = UserRole.Teacher };
            var asset = new MediaAsset
            {
                LocationUrl = "/media/file.png",
                MimeType = MediaType.Image,
                Owner = originalOwner
            };

            context.Teachers.AddRange(originalOwner, replacementOwner);
            context.MediaAssets.Add(asset);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.ReassignMediaAssetOwnerAsync(asset.Id, replacementOwner.Id);

            Assert.True(result.Success);
            Assert.Equal(replacementOwner.Id, result.Data!.OwnerId);
            Assert.Equal("Replacement", result.Data.OwnerUsername);
            Assert.Equal(replacementOwner.Id, (await context.MediaAssets.FindAsync(asset.Id))!.OwnerId);
        }

        [Fact]
        public async Task ReassignMediaAssetOwnerAsync_WhenAssetMissing_ReturnsNotFound()
        {
            var context = await GetInMemoryDbContext();
            var replacementOwner = new Teacher { Username = "Replacement", Role = UserRole.Teacher };
            context.Teachers.Add(replacementOwner);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.ReassignMediaAssetOwnerAsync(999, replacementOwner.Id);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.NotFound, result.ErrorCode);
            Assert.Contains("Media asset not found", result.ErrorMessage);
        }

        [Fact]
        public async Task ReassignMediaAssetOwnerAsync_WhenReplacementOwnerIsArchived_ReturnsNotFound()
        {
            var context = await GetInMemoryDbContext();
            var originalOwner = new Teacher { Username = "Original", Role = UserRole.Teacher };
            var archivedOwner = new Teacher
            {
                Username = "Archived",
                Role = UserRole.Teacher,
                ArchivedAt = DateTime.UtcNow
            };
            var asset = new MediaAsset { LocationUrl = "/media/file.png", Owner = originalOwner };

            context.Teachers.AddRange(originalOwner, archivedOwner);
            context.MediaAssets.Add(asset);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.ReassignMediaAssetOwnerAsync(asset.Id, archivedOwner.Id);

            Assert.False(result.Success);
            Assert.Equal(ServiceError.NotFound, result.ErrorCode);
            Assert.Equal(originalOwner.Id, (await context.MediaAssets.FindAsync(asset.Id))!.OwnerId);
        }

        [Fact]
        public async Task DeleteMediaAssetAsync_WhenExists_DeletesAsset()
        {
            var context = await GetInMemoryDbContext();
            var teacher = new Teacher { Username = "Owner", Role = UserRole.Teacher };
            var asset = new MediaAsset { LocationUrl = "url", Owner = teacher };

            context.Teachers.Add(teacher);
            context.MediaAssets.Add(asset);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.DeleteMediaAssetAsync(asset.Id);

            Assert.True(result.Success);
            var dbAsset = await context.MediaAssets.FindAsync(asset.Id);
            Assert.Null(dbAsset);
        }

        [Fact]
        public async Task GetDownloadMetadataAsync_WhenValid_ReturnsDownloadDto()
        {
            var context = await GetInMemoryDbContext();
            var teacher = new Teacher { Username = "Owner", Role = UserRole.Teacher };
            var asset = new MediaAsset { LocationUrl = "url", Owner = teacher, IsPrivate = true };

            context.Teachers.Add(teacher);
            context.MediaAssets.Add(asset);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Accessing own private asset
            var result = await service.GetDownloadMetadataAsync(asset.Id, teacher.Id, UserRole.Teacher.ToString());

            Assert.True(result.Success);
            Assert.Equal("url", result.Data!.LocationUrl);
        }

        [Fact]
        public async Task GetDownloadMetadataAsync_WhenForbidden_ReturnsForbidden()
        {
            var context = await GetInMemoryDbContext();
            var teacher1 = new Teacher { Username = "Owner", Role = UserRole.Teacher };
            var teacher2 = new Teacher { Username = "Intruder", Role = UserRole.Teacher };
            var asset = new MediaAsset { LocationUrl = "url", Owner = teacher1, IsPrivate = true };

            context.Teachers.AddRange(teacher1, teacher2);
            context.MediaAssets.Add(asset);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Intruder trying to access teacher1's private asset
            var result = await service.GetDownloadMetadataAsync(asset.Id, teacher2.Id, UserRole.Teacher.ToString());

            Assert.False(result.Success);
            Assert.Equal(ServiceError.Forbidden, result.ErrorCode);
        }

        [Fact]
        public async Task DeleteMediaAssetAndFileAsync_WhenExists_CallsStorageAndDeletesFromDb()
        {
            var context = await GetInMemoryDbContext();
            var teacher = new Teacher { Username = "Owner", Role = UserRole.Teacher };
            var asset = new MediaAsset { LocationUrl = "/uploads/test.png", Owner = teacher };

            context.Teachers.Add(teacher);
            context.MediaAssets.Add(asset);
            await context.SaveChangesAsync();

            var mockLogger = new Mock<ILogger<MediaAssetService>>();
            var mockFileService = new Mock<IFileStorageService>();
            mockFileService.Setup(fs => fs.DeleteFileAsync(It.IsAny<string>())).ReturnsAsync(ServiceResult<bool>.Ok(true));

            var service = new MediaAssetService(context, mockLogger.Object, mockFileService.Object);

            var result = await service.DeleteMediaAssetAndFileAsync(asset.Id);

            Assert.True(result.Success);
            mockFileService.Verify(fs => fs.DeleteFileAsync("/uploads/test.png"), Times.Once);

            var dbAsset = await context.MediaAssets.FindAsync(asset.Id);
            Assert.Null(dbAsset);
        }

        [Fact]
        public async Task GetDownloadMetadataAsync_WhenPrivateAndAdmin_ReturnsDownloadDto()
        {
            var context = await GetInMemoryDbContext();
            var owner = new Teacher { Username = "Owner", Role = UserRole.Teacher };
            var admin = new Admin { Username = "Admin", Role = UserRole.Admin };
            var asset = new MediaAsset { LocationUrl = "/uploads/lesson.pdf", Owner = owner, IsPrivate = true };

            context.Students.AddRange(owner, admin);
            context.MediaAssets.Add(asset);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetDownloadMetadataAsync(asset.Id, admin.Id, UserRole.Admin.ToString());

            Assert.True(result.Success);
            Assert.Equal("/uploads/lesson.pdf", result.Data!.LocationUrl);
            Assert.Equal("application/pdf", result.Data.MimeType);
        }

        [Fact]
        public async Task GetDownloadMetadataAsync_WhenUnknownExtension_ReturnsOctetStream()
        {
            var context = await GetInMemoryDbContext();
            var teacher = new Teacher { Username = "Owner", Role = UserRole.Teacher };
            var asset = new MediaAsset { LocationUrl = "/uploads/file.unknownext", Owner = teacher, IsPrivate = false };

            context.Teachers.Add(teacher);
            context.MediaAssets.Add(asset);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetDownloadMetadataAsync(asset.Id, 999, UserRole.Student.ToString());

            Assert.True(result.Success);
            Assert.Equal("application/octet-stream", result.Data!.MimeType);
        }

        [Fact]
        public async Task DeleteMediaAssetAndFileAsync_WhenMissing_ReturnsSuccessWithoutStorageCall()
        {
            var context = await GetInMemoryDbContext();
            var mockLogger = new Mock<ILogger<MediaAssetService>>();
            var mockFileService = new Mock<IFileStorageService>();
            var service = new MediaAssetService(context, mockLogger.Object, mockFileService.Object);

            var result = await service.DeleteMediaAssetAndFileAsync(999);

            Assert.True(result.Success);
            mockFileService.Verify(fs => fs.DeleteFileAsync(It.IsAny<string>()), Times.Never);
        }
    }
}
