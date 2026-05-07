using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sqeez.Api.Data;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.Gamification;
using Sqeez.Api.Models.Users;

namespace Sqeez.Api.Tests.Integration.Postgres
{
    [Collection(PostgresIntegrationTestCollection.CollectionName)]
    public class SchoolClassUserPostgresIntegrationTests
    {
        private readonly PostgresIntegrationTestFixture _fixture;

        public SchoolClassUserPostgresIntegrationTests(PostgresIntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [DockerAvailableFact]
        public async Task CreateClass_AsAdmin_PersistsClassAndTeacherAssignment()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedUsersAsync();
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Admin);

            var response = await client.PostAsJsonAsync("/api/classes", new
            {
                name = "Live Class 4A",
                academicYear = "2025/2026",
                section = "4A",
                teacherId = seed.Teacher.Id
            });

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.Created, response);
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var classId = json.RootElement.GetProperty("id").GetInt64();

            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var schoolClass = await dbContext.SchoolClasses.SingleAsync(c => c.Id == classId);
            var teacher = await dbContext.Teachers.SingleAsync(t => t.Id == seed.Teacher.Id);

            Assert.Equal("Live Class 4A", schoolClass.Name);
            Assert.Equal(classId, teacher.ManagedClassId);
        }

        [DockerAvailableFact]
        public async Task AssignAndRemoveStudents_AsAdmin_UpdatesStudentClassMembership()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedUsersAsync();
            var schoolClass = await AddClassAsync(seed.Teacher);
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Admin);

            var assignResponse = await client.PostAsJsonAsync($"/api/classes/{schoolClass.Id}/students", new
            {
                studentIds = new[] { seed.Student.Id }
            });

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, assignResponse);

            var removeResponse = await client.PostAsJsonAsync($"/api/classes/{schoolClass.Id}/students/remove", new
            {
                studentIds = new[] { seed.Student.Id }
            });

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, removeResponse);

            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var student = await dbContext.Students.SingleAsync(s => s.Id == seed.Student.Id);

            Assert.Null(student.SchoolClassId);
        }

        [DockerAvailableFact]
        public async Task GetClassDetails_ReturnsTeacherStudentsAndSubjects()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedUsersAsync();
            var schoolClass = await AddClassAsync(seed.Teacher, seed.Student, withSubject: true);
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Student);

            var response = await client.GetAsync($"/api/classes/{schoolClass.Id}");

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, response);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Contains(seed.Student.Email, body);
            Assert.Contains(seed.Teacher.Email, body);
            Assert.Contains("Class subject", body);
        }

        [DockerAvailableFact]
        public async Task PatchClass_RemoveTeacher_ClearsTeacherManagedClass()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedUsersAsync();
            var schoolClass = await AddClassAsync(seed.Teacher);
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Admin);

            var response = await client.PatchAsJsonAsync($"/api/classes/{schoolClass.Id}", new
            {
                teacherId = 0
            });

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, response);

            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var teacher = await dbContext.Teachers.SingleAsync(t => t.Id == seed.Teacher.Id);

            Assert.Null(teacher.ManagedClassId);
        }

        [DockerAvailableFact]
        public async Task CreateUser_WithTeacherDiscriminator_PersistsTeacherSubtype()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedUsersAsync();
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Admin);
            var username = PostgresTestHelpers.UniqueValue("created-teacher", 20);
            var email = $"{PostgresTestHelpers.UniqueValue("created-teacher", 24)}@sqeez.test";

            var response = await client.PostAsJsonAsync("/api/users", new
            {
                role = "teacher",
                firstName = "Created",
                lastName = "Teacher",
                username,
                email,
                password = "StrongPassword123!",
                department = "Mathematics"
            });

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.Created, response);

            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var teacher = await dbContext.Teachers.SingleAsync(t => t.Email == email);

            Assert.Equal(UserRole.Teacher, teacher.Role);
            Assert.Equal("Mathematics", teacher.Department);
            Assert.NotEqual("StrongPassword123!", teacher.PasswordHash);
        }

        [DockerAvailableFact]
        public async Task PatchUser_AsOwner_UpdatesUsernameAndAvatar()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedUsersAsync();
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Student);
            var newUsername = PostgresTestHelpers.UniqueValue("updated", 20);

            var response = await client.PatchAsJsonAsync($"/api/users/{seed.Student.Id}", new
            {
                role = "student",
                username = newUsername,
                avatarUrl = "/avatars/live.png"
            });

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, response);

            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var student = await dbContext.Students.SingleAsync(s => s.Id == seed.Student.Id);

            Assert.Equal(newUsername, student.Username);
            Assert.Equal("/avatars/live.png", student.AvatarUrl);
        }

        [DockerAvailableFact]
        public async Task GetDetailedUser_ReturnsClassEnrollmentsAndBadges()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedUsersAsync();
            await AddClassSubjectEnrollmentAndBadgeAsync(seed);
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Admin);

            var response = await client.GetAsync($"/api/users/{seed.Student.Id}/details");

            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, response);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Contains("Detail class", body);
            Assert.Contains("Detail subject", body);
            Assert.Contains("Detail badge", body);
        }

        [DockerAvailableFact]
        public async Task ArchiveUser_AsAdmin_SetsArchivedAtAndFilterFindsArchivedUser()
        {
            await _fixture.ResetDatabaseAsync();
            var seed = await SeedUsersAsync();
            var client = PostgresTestHelpers.CreateAuthenticatedClient(_fixture, seed.Admin);

            var deleteResponse = await client.DeleteAsync($"/api/users/{seed.Student.Id}");
            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.NoContent, deleteResponse);

            var listResponse = await client.GetAsync("/api/users?isArchived=true&pageSize=20");
            await PostgresTestHelpers.AssertStatusCodeAsync(HttpStatusCode.OK, listResponse);
            var body = await listResponse.Content.ReadAsStringAsync();

            Assert.Contains(seed.Student.Username, body);
        }

        private async Task<UserSeed> SeedUsersAsync()
        {
            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();

            var student = await PostgresTestHelpers.SeedStudentAsync(dbContext);
            var teacher = await PostgresTestHelpers.SeedTeacherAsync(dbContext);
            var admin = await PostgresTestHelpers.SeedAdminAsync(dbContext);

            return new UserSeed(student, teacher, admin);
        }

        private async Task<SchoolClass> AddClassAsync(Teacher teacher, Student? student = null, bool withSubject = false)
        {
            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var trackedTeacher = await dbContext.Teachers.SingleAsync(t => t.Id == teacher.Id);

            var schoolClass = new SchoolClass
            {
                Name = withSubject ? "Detail class" : "Live Class",
                AcademicYear = "2025/2026",
                Section = "A",
                Teacher = trackedTeacher
            };

            dbContext.SchoolClasses.Add(schoolClass);
            await dbContext.SaveChangesAsync();

            if (student != null)
            {
                var trackedStudent = await dbContext.Students.SingleAsync(s => s.Id == student.Id);
                trackedStudent.SchoolClassId = schoolClass.Id;
            }

            if (withSubject)
            {
                dbContext.Subjects.Add(new Subject
                {
                    Name = "Class subject",
                    Code = PostgresTestHelpers.UniqueValue("class-subject", 30),
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    SchoolClassId = schoolClass.Id,
                    TeacherId = teacher.Id
                });
            }

            await dbContext.SaveChangesAsync();
            return schoolClass;
        }

        private async Task AddClassSubjectEnrollmentAndBadgeAsync(UserSeed seed)
        {
            using var scope = _fixture.Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();
            var teacher = await dbContext.Teachers.SingleAsync(t => t.Id == seed.Teacher.Id);
            var student = await dbContext.Students.SingleAsync(s => s.Id == seed.Student.Id);

            var schoolClass = new SchoolClass
            {
                Name = "Detail class",
                AcademicYear = "2025/2026",
                Section = "D",
                Teacher = teacher
            };
            dbContext.SchoolClasses.Add(schoolClass);
            await dbContext.SaveChangesAsync();

            student.SchoolClassId = schoolClass.Id;

            var subject = new Subject
            {
                Name = "Detail subject",
                Code = PostgresTestHelpers.UniqueValue("detail-subject", 30),
                StartDate = DateTime.UtcNow.AddDays(-1),
                TeacherId = teacher.Id
            };
            dbContext.Subjects.Add(subject);

            var badge = new Badge
            {
                Name = "Detail badge",
                Description = "Visible in user details.",
                XpBonus = 1
            };
            dbContext.Badges.Add(badge);
            await dbContext.SaveChangesAsync();

            dbContext.Enrollments.Add(new Enrollment
            {
                StudentId = student.Id,
                SubjectId = subject.Id,
                EnrolledAt = DateTime.UtcNow
            });
            dbContext.StudentBadges.Add(new StudentBadge
            {
                StudentId = student.Id,
                BadgeId = badge.Id,
                EarnedAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        private sealed record UserSeed(Student Student, Teacher Teacher, Admin Admin);
    }
}
