using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Moq;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;

namespace Sqeez.Api.Tests.Integration
{
    public class MediaAssetControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public MediaAssetControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetMocks();
        }

        [Fact]
        public async Task CreateMediaAsset_AsTeacher_CallsMediaAssetService()
        {
            _factory.MediaAssetServiceMock
                .Setup(service => service.CreateMediaAssetAsync(It.Is<CreateMediaAssetDto>(dto =>
                    dto.LocationUrl == "/media/file.png" &&
                    dto.MimeType == MediaType.Image &&
                    dto.OwnerId == 42)))
                .ReturnsAsync(ServiceResult<MediaAssetDto>.Ok(
                    new MediaAssetDto(5, "/media/file.png", MediaType.Image, false, null, 42, "teacher")));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.PostAsJsonAsync("/api/media-assets", new
            {
                locationUrl = "/media/file.png",
                mimeType = "Image",
                isPrivate = false,
                ownerId = 42
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.MediaAssetServiceMock.Verify(
                service => service.CreateMediaAssetAsync(It.IsAny<CreateMediaAssetDto>()),
                Times.Once);
        }

        [Fact]
        public async Task UploadFile_UsesStorageThenCreatesMediaAssetWithAuthenticatedOwner()
        {
            _factory.FileStorageServiceMock
                .Setup(service => service.UploadFileAsync(
                    It.IsAny<IFormFile>(),
                    "media",
                    false))
                .ReturnsAsync(ServiceResult<string>.Ok("/secure/media/upload.png"));

            _factory.MediaAssetServiceMock
                .Setup(service => service.CreateMediaAssetAsync(It.Is<CreateMediaAssetDto>(dto =>
                    dto.LocationUrl == "/secure/media/upload.png" &&
                    dto.MimeType == MediaType.Image &&
                    dto.OwnerId == 42 &&
                    dto.Description == "diagram")))
                .ReturnsAsync(ServiceResult<MediaAssetDto>.Ok(
                    new MediaAssetDto(8, "/secure/media/upload.png", MediaType.Image, true, "diagram", 42, "teacher")));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "file", "upload.png");
            content.Add(new StringContent("true"), "isPrivate");
            content.Add(new StringContent("diagram"), "description");

            var response = await client.PostAsync("/api/media-assets/upload", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.FileStorageServiceMock.Verify(
                service => service.UploadFileAsync(It.IsAny<IFormFile>(), "media", false),
                Times.Once);
            _factory.MediaAssetServiceMock.Verify(
                service => service.CreateMediaAssetAsync(It.IsAny<CreateMediaAssetDto>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateMediaAsset_IgnoresSpoofedOwnerIdAndUsesAuthenticatedUser()
        {
            _factory.MediaAssetServiceMock
                .Setup(service => service.CreateMediaAssetAsync(It.Is<CreateMediaAssetDto>(dto =>
                    dto.OwnerId == 42 &&
                    dto.LocationUrl == "/media/file.png")))
                .ReturnsAsync(ServiceResult<MediaAssetDto>.Ok(
                    new MediaAssetDto(5, "/media/file.png", MediaType.Image, false, null, 42, "teacher")));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.PostAsJsonAsync("/api/media-assets", new
            {
                locationUrl = "/media/file.png",
                mimeType = "Image",
                isPrivate = false,
                ownerId = 99
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.MediaAssetServiceMock.Verify(
                service => service.CreateMediaAssetAsync(It.Is<CreateMediaAssetDto>(dto => dto.OwnerId == 99)),
                Times.Never);
        }

        [Fact]
        public async Task UploadFile_UsesStoredFileExtensionForMediaTypeInsteadOfClientContentType()
        {
            _factory.FileStorageServiceMock
                .Setup(service => service.UploadFileAsync(It.IsAny<IFormFile>(), "media", false))
                .ReturnsAsync(ServiceResult<string>.Ok("/secure/media/upload.pdf"));

            _factory.MediaAssetServiceMock
                .Setup(service => service.CreateMediaAssetAsync(It.Is<CreateMediaAssetDto>(dto =>
                    dto.LocationUrl == "/secure/media/upload.pdf" &&
                    dto.MimeType == MediaType.Document &&
                    dto.OwnerId == 42)))
                .ReturnsAsync(ServiceResult<MediaAssetDto>.Ok(
                    new MediaAssetDto(9, "/secure/media/upload.pdf", MediaType.Document, false, null, 42, "teacher")));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "file", "spoofed.png");

            var response = await client.PostAsync("/api/media-assets/upload", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _factory.MediaAssetServiceMock.Verify(
                service => service.CreateMediaAssetAsync(It.IsAny<CreateMediaAssetDto>()),
                Times.Once);
        }

        [Fact]
        public async Task UploadFile_WhenDatabaseCreateFails_DeletesUploadedFile()
        {
            _factory.FileStorageServiceMock
                .Setup(service => service.UploadFileAsync(It.IsAny<IFormFile>(), "media", false))
                .ReturnsAsync(ServiceResult<string>.Ok("/secure/media/upload.png"));
            _factory.MediaAssetServiceMock
                .Setup(service => service.CreateMediaAssetAsync(It.IsAny<CreateMediaAssetDto>()))
                .ReturnsAsync(ServiceResult<MediaAssetDto>.Failure("Owner not found.", ServiceError.NotFound));
            _factory.FileStorageServiceMock
                .Setup(service => service.DeleteFileAsync("/secure/media/upload.png"))
                .ReturnsAsync(ServiceResult<bool>.Ok(true));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "file", "upload.png");

            var response = await client.PostAsync("/api/media-assets/upload", content);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            _factory.FileStorageServiceMock.Verify(service => service.DeleteFileAsync("/secure/media/upload.png"), Times.Once);
        }

        [Fact]
        public async Task DeleteMediaAsset_AsNonOwnerTeacher_ReturnsForbiddenBeforeDelete()
        {
            _factory.MediaAssetServiceMock
                .Setup(service => service.GetMediaAssetByIdAsync(5))
                .ReturnsAsync(ServiceResult<MediaAssetDto>.Ok(
                    new MediaAssetDto(5, "/secure/media/file.png", MediaType.Image, true, null, 99, "owner")));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.DeleteAsync("/api/media-assets/5");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _factory.MediaAssetServiceMock.Verify(service => service.DeleteMediaAssetAndFileAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task PatchMediaAsset_AsNonOwnerTeacher_ReturnsForbiddenBeforePatch()
        {
            _factory.MediaAssetServiceMock
                .Setup(service => service.GetMediaAssetByIdAsync(5))
                .ReturnsAsync(ServiceResult<MediaAssetDto>.Ok(
                    new MediaAssetDto(5, "/secure/media/file.png", MediaType.Image, true, null, 99, "owner")));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, "42");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Teacher");

            var response = await client.PatchAsJsonAsync("/api/media-assets/5", new
            {
                description = "patched"
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _factory.MediaAssetServiceMock.Verify(
                service => service.PatchMediaAssetAsync(It.IsAny<long>(), It.IsAny<PatchMediaAssetDto>()),
                Times.Never);
        }
    }
}
