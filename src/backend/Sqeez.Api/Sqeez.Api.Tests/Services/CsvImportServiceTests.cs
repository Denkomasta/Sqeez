using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.Import;
using Sqeez.Api.Models.QuizSystem;
using Sqeez.Api.Models.Users;
using Sqeez.Api.Services;
using Sqeez.Api.Services.Interfaces;
using System.Text;
using Xunit;

namespace Sqeez.Api.Tests.Services
{
    public class CsvImportServiceTests
    {
        private async Task<SqeezDbContext> GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<SqeezDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var context = new SqeezDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return context;
        }

        private CsvImportService CreateService(
            SqeezDbContext context,
            Mock<ISchoolClassService> mockClassService,
            Mock<ISubjectService> mockSubjectService,
            Mock<IUserService> mockUserService)
        {
            var mockLogger = new Mock<ILogger<CsvImportService>>();
            return new CsvImportService(
                context,
                mockLogger.Object,
                mockClassService.Object,
                mockSubjectService.Object,
                mockUserService.Object);
        }

        private IFormFile CreateMockFile(string content, string fileName = "test.csv")
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, stream.Length, "file", fileName);
        }

        [Fact]
        public async Task ImportMasterFileAsync_WithNullFile_ReturnsBadRequest()
        {
            var context = await GetInMemoryDbContext();
            var service = CreateService(context, new Mock<ISchoolClassService>(), new Mock<ISubjectService>(), new Mock<IUserService>());

            var result = await service.ImportMasterFileAsync(null!);

            Assert.False(result.Success);
            Assert.Equal(Sqeez.Api.Enums.ServiceError.BadRequest, result.ErrorCode);
        }

        [Fact]
        public async Task ImportMasterFileAsync_WithNonCsvFile_ReturnsBadRequest()
        {
            var context = await GetInMemoryDbContext();
            var service = CreateService(context, new Mock<ISchoolClassService>(), new Mock<ISubjectService>(), new Mock<IUserService>());
            var file = CreateMockFile("content", "test.txt");

            var result = await service.ImportMasterFileAsync(file);

            Assert.False(result.Success);
            Assert.Equal(Sqeez.Api.Enums.ServiceError.BadRequest, result.ErrorCode);
        }

        [Fact]
        public async Task ImportMasterFileAsync_WithValidCsv_CallsDependencies()
        {
            var context = await GetInMemoryDbContext();
            var mockClassService = new Mock<ISchoolClassService>();
            var mockSubjectService = new Mock<ISubjectService>();
            var mockUserService = new Mock<IUserService>();

            // Mock successful class creation
            var classResult = new BulkOperationResult<SchoolClassDto>
            {
                Created = new List<SchoolClassDto> { new SchoolClassDto(1, "ClassA", "2024", "A", null, null, 1, 1) },
                Existing = new List<SchoolClassDto>()
            };
            mockClassService.Setup(s => s.EnsureClassesExistAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(ServiceResult<BulkOperationResult<SchoolClassDto>>.Ok(classResult));

            // Mock successful subject creation
            var subjectResult = new BulkOperationResult<SubjectDto>
            {
                Created = new List<SubjectDto> { new SubjectDto(1, "Mathematics", "Math101", null, DateTime.UtcNow, null, null, null, null, null, 1, 1) },
                Existing = new List<SubjectDto>()
            };
            mockSubjectService.Setup(s => s.CreateSubjectsBulkAsync(It.IsAny<IEnumerable<Subject>>()))
                .ReturnsAsync(ServiceResult<BulkOperationResult<SubjectDto>>.Ok(subjectResult));

            // Mock successful student creation
            var studentResult = new BulkOperationResult<StudentDto>
            {
                Created = new List<StudentDto> { new StudentDto { Id = 1, Username = "john", Email = "john@sqeez.org", FirstName = "John", LastName = "Doe" } },
                Existing = new List<StudentDto>()
            };
            mockUserService.Setup(s => s.CreateStudentsBulkAsync(It.IsAny<IEnumerable<Student>>()))
                .ReturnsAsync(ServiceResult<BulkOperationResult<StudentDto>>.Ok(studentResult));

            var service = CreateService(context, mockClassService, mockSubjectService, mockUserService);

            var csvContent = "Class Name,Academic Year,Subject Name,Subject Code,First Name,Last Name,Email,Password\n" +
                             "ClassA,2024,Mathematics,Math101,John,Doe,john@sqeez.org,Heslo1122*\n";
            var file = CreateMockFile(csvContent);

            var result = await service.ImportMasterFileAsync(file);

            Assert.True(result.Success, result.ErrorMessage);
            Assert.NotNull(result.Data);

            Assert.Equal(3, result.Data!.RecordsImported); 

            mockClassService.Verify(s => s.EnsureClassesExistAsync(It.Is<IEnumerable<string>>(l => l.Contains("ClassA"))), Times.Once);
            mockSubjectService.Verify(s => s.CreateSubjectsBulkAsync(It.Is<IEnumerable<Subject>>(l => l.Any(subj => subj.Code == "Math101"))), Times.Once);
            mockUserService.Verify(s => s.CreateStudentsBulkAsync(It.Is<IEnumerable<Student>>(l => l.Any(stu => stu.Email == "john@sqeez.org"))), Times.Once);
        }

        [Fact]
        public async Task ImportMasterFileAsync_WhenRowsAreInvalid_ReturnsRowErrorsAndSkipsDependencies()
        {
            var context = await GetInMemoryDbContext();
            var mockClassService = new Mock<ISchoolClassService>();
            var mockSubjectService = new Mock<ISubjectService>();
            var mockUserService = new Mock<IUserService>();
            var service = CreateService(context, mockClassService, mockSubjectService, mockUserService);

            var csvContent = "Class Name,Academic Year,Subject Name,Subject Code,First Name,Last Name,Email,Password\n" +
                             "ClassA,2024,Mathematics,Math101,John,Doe,not-an-email,Heslo1122*\n";
            var file = CreateMockFile(csvContent);

            var result = await service.ImportMasterFileAsync(file);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data!.RecordsImported);
            Assert.True(result.Data.HasRowErrors);
            Assert.Contains(result.Data.Errors, e => e.Contains("Row 2") && e.Contains("Invalid email format"));
            mockClassService.Verify(s => s.EnsureClassesExistAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
            mockSubjectService.Verify(s => s.CreateSubjectsBulkAsync(It.IsAny<IEnumerable<Subject>>()), Times.Never);
            mockUserService.Verify(s => s.CreateStudentsBulkAsync(It.IsAny<IEnumerable<Student>>()), Times.Never);
        }

        [Fact]
        public async Task ImportMasterFileAsync_WhenMixedValidity_ImportsValidRowsAndReportsInvalidRows()
        {
            var context = await GetInMemoryDbContext();
            var mockClassService = new Mock<ISchoolClassService>();
            var mockSubjectService = new Mock<ISubjectService>();
            var mockUserService = new Mock<IUserService>();

            mockClassService.Setup(s => s.EnsureClassesExistAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(ServiceResult<BulkOperationResult<SchoolClassDto>>.Ok(new BulkOperationResult<SchoolClassDto>
                {
                    Created = new List<SchoolClassDto> { new SchoolClassDto(1, "ClassA", "2024", "A", null, null, 0, 0) }
                }));

            mockSubjectService.Setup(s => s.CreateSubjectsBulkAsync(It.IsAny<IEnumerable<Subject>>()))
                .ReturnsAsync(ServiceResult<BulkOperationResult<SubjectDto>>.Ok(new BulkOperationResult<SubjectDto>
                {
                    Created = new List<SubjectDto> { new SubjectDto(1, "Mathematics", "Math101", null, DateTime.UtcNow, null, null, null, null, null, 0, 0) }
                }));

            mockUserService.Setup(s => s.CreateStudentsBulkAsync(It.IsAny<IEnumerable<Student>>()))
                .ReturnsAsync(ServiceResult<BulkOperationResult<StudentDto>>.Ok(new BulkOperationResult<StudentDto>
                {
                    Created = new List<StudentDto> { new StudentDto { Id = 1, Username = "jane", Email = "jane@sqeez.org" } }
                }));

            var service = CreateService(context, mockClassService, mockSubjectService, mockUserService);

            var csvContent = "Class Name,Academic Year,Subject Name,Subject Code,First Name,Last Name,Email,Password\n" +
                             "ClassA,2024,Mathematics,Math101,Jane,Doe,jane@sqeez.org,Heslo1122*\n" +
                             "ClassA,2024,Mathematics,Math101,Bad,User,bad-email,Heslo1122*\n";
            var file = CreateMockFile(csvContent);

            var result = await service.ImportMasterFileAsync(file);

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(3, result.Data!.RecordsImported);
            Assert.Single(result.Data.Errors);
            Assert.Contains("Row 3", result.Data.Errors.First());
            mockUserService.Verify(s => s.CreateStudentsBulkAsync(
                It.Is<IEnumerable<Student>>(students => students.Count() == 1 && students.First().Email == "jane@sqeez.org")),
                Times.Once);
        }

        [Fact]
        public async Task ImportQuizFileAsync_WithFreeTextQuestion_CreatesQuizTreeWithoutMedia()
        {
            var context = await GetInMemoryDbContext();
            var teacher = new Teacher { Username = "Teacher", Email = "teacher@sqeez.org", Role = Sqeez.Api.Enums.UserRole.Teacher };
            var subject = new Subject
            {
                Name = "Math",
                Code = "MATH",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddMonths(2),
                Teacher = teacher
            };
            context.Teachers.Add(teacher);
            context.Subjects.Add(subject);
            await context.SaveChangesAsync();

            var service = CreateService(context, new Mock<ISchoolClassService>(), new Mock<ISubjectService>(), new Mock<IUserService>());
            var csvContent = "Quiz Title,Quiz Description,Max Retries,Publish Date,Closing Date,Question Order,Question Title,Difficulty,Time Limit,Has Penalty,Is Strict Multiple Choice,Option Order,Option Text,Is Correct,Is Free Text\n" +
                             "Algebra basics,Intro quiz,2,2026-05-20T08:00:00Z,2026-06-01T08:00:00Z,1,What is 2+2?,1,60,false,false,1,4,true,false\n" +
                             "Algebra basics,Intro quiz,2,2026-05-20T08:00:00Z,2026-06-01T08:00:00Z,1,What is 2+2?,1,60,false,false,2,5,false,false\n" +
                             "Algebra basics,Intro quiz,2,2026-05-20T08:00:00Z,2026-06-01T08:00:00Z,2,Explain distributivity,2,180,false,false,1,a*(b+c)=a*b+a*c,true,true\n";
            var file = CreateMockFile(csvContent, "quiz.csv");

            var result = await service.ImportQuizFileAsync(subject.Id, file, teacher.Id);

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(6, result.Data!.RecordsImported);

            var quiz = await context.Quizzes
                .Include(q => q.QuizQuestions)
                    .ThenInclude(q => q.Options)
                .SingleAsync();
            Assert.Equal("Algebra basics", quiz.Title);
            Assert.Equal(subject.Id, quiz.SubjectId);
            Assert.Equal(2, quiz.QuizQuestions.Count);
            Assert.All(quiz.QuizQuestions, question => Assert.Null(question.MediaAssetId));
            Assert.All(quiz.QuizQuestions.SelectMany(question => question.Options), option => Assert.Null(option.MediaAssetId));

            var freeTextQuestion = quiz.QuizQuestions.Single(question => question.Title == "Explain distributivity");
            var freeTextOption = Assert.Single(freeTextQuestion.Options);
            Assert.True(freeTextOption.IsFreeText);
            Assert.True(freeTextOption.IsCorrect);
            Assert.Equal("a*(b+c)=a*b+a*c", freeTextOption.Text);
        }

        [Fact]
        public async Task ImportQuizFileAsync_WhenFreeTextQuestionHasMultipleOptions_ReturnsErrorsAndSkipsQuiz()
        {
            var context = await GetInMemoryDbContext();
            var teacher = new Teacher { Username = "Teacher", Email = "teacher@sqeez.org", Role = Sqeez.Api.Enums.UserRole.Teacher };
            var subject = new Subject
            {
                Name = "Math",
                Code = "MATH",
                StartDate = DateTime.UtcNow.AddDays(-1),
                Teacher = teacher
            };
            context.Teachers.Add(teacher);
            context.Subjects.Add(subject);
            await context.SaveChangesAsync();

            var service = CreateService(context, new Mock<ISchoolClassService>(), new Mock<ISubjectService>(), new Mock<IUserService>());
            var csvContent = "Quiz Title,Quiz Description,Max Retries,Publish Date,Closing Date,Question Order,Question Title,Difficulty,Time Limit,Has Penalty,Is Strict Multiple Choice,Option Order,Option Text,Is Correct,Is Free Text\n" +
                             "Written quiz,Intro,0,,,1,Explain gravity,1,120,false,false,1,Gravity attracts masses,true,true\n" +
                             "Written quiz,Intro,0,,,1,Explain gravity,1,120,false,false,2,Second solution,false,true\n";
            var file = CreateMockFile(csvContent, "quiz.csv");

            var result = await service.ImportQuizFileAsync(subject.Id, file, teacher.Id);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data!.RecordsImported);
            Assert.Contains(result.Data.Errors, error => error.Contains("free-text question"));
            Assert.False(await context.Quizzes.AnyAsync());
        }

        [Fact]
        public async Task ExportQuizFileAsync_WhenTeacherOwnsSubject_ReturnsQuizCsv()
        {
            var context = await GetInMemoryDbContext();
            var teacher = new Teacher { Username = "Teacher", Email = "teacher@sqeez.org", Role = Sqeez.Api.Enums.UserRole.Teacher };
            var subject = new Subject { Name = "Math", Code = "MATH", StartDate = DateTime.UtcNow, Teacher = teacher };
            var quiz = new Quiz
            {
                Title = "Exported quiz",
                Description = "Ready",
                MaxRetries = 1,
                Subject = subject,
                PublishDate = new DateTime(2026, 5, 20, 8, 0, 0, DateTimeKind.Utc)
            };
            var question = new QuizQuestion
            {
                Quiz = quiz,
                Title = "Explain distributivity",
                Difficulty = 2,
                TimeLimit = 180
            };
            var option = new QuizOption
            {
                QuizQuestion = question,
                Text = "a*(b+c)=a*b+a*c",
                IsCorrect = true,
                IsFreeText = true
            };

            context.Teachers.Add(teacher);
            context.Subjects.Add(subject);
            context.Quizzes.Add(quiz);
            context.QuizQuestions.Add(question);
            context.QuizOptions.Add(option);
            await context.SaveChangesAsync();

            var service = CreateService(context, new Mock<ISchoolClassService>(), new Mock<ISubjectService>(), new Mock<IUserService>());

            var result = await service.ExportQuizFileAsync(quiz.Id, teacher.Id);

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Contains("Quiz Title", result.Data);
            Assert.Contains("Exported quiz", result.Data);
            Assert.Contains("a*(b+c)=a*b+a*c", result.Data);
            Assert.Contains("True", result.Data);
        }
    }
}
