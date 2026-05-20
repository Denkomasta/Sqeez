using Microsoft.EntityFrameworkCore;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.Gamification;
using Sqeez.Api.Models.Media;
using Sqeez.Api.Models.QuizSystem;
using Sqeez.Api.Models.System;
using Sqeez.Api.Models.Users;
using BC = BCrypt.Net.BCrypt;

namespace Sqeez.Api.Data
{
    /// <summary>
    /// Creates required baseline data and optional demo data for local development environments.
    /// </summary>
    public static class DatabaseSeeder
    {
        /// <summary>
        /// Ensures production-required seed data exists: the singleton system configuration row and superadmin account.
        /// </summary>
        /// <param name="context">Database context to seed.</param>
        /// <param name="config">Application configuration containing required superadmin settings.</param>
        public static async Task SeedAsync(SqeezDbContext context, IConfiguration config)
        {
            await EnsureSystemConfigAsync(context);
            await EnsureSuperAdminAsync(context, config);

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seeds a complete demo dataset with users, classes, subjects, enrollments, quiz content, media, and badges.
        /// </summary>
        /// <remarks>
        /// Demo seeding is idempotent at the database level: if any users already exist, no demo records are added.
        /// </remarks>
        /// <param name="context">Database context to seed.</param>
        /// <param name="config">Application configuration used for demo superadmin credentials.</param>
        public static async Task SeedDemoAsync(SqeezDbContext context, IConfiguration config)
        {
            await EnsureSystemConfigAsync(context);

            if (await context.Students.AnyAsync())
            {
                await context.SaveChangesAsync();
                return;
            }

            string superEmail = config["SUPER_USER_EMAIL"] ?? "test@example.com";
            string superPassword = config["SUPER_USER_DEFAULT_PASSWORD"] ?? "YourSuperSecretPassword123!";

            string salt = BC.GenerateSalt(12);
            string defaultPassword = BC.HashPassword("Heslo1122*", salt);
            string superPasswordHash = BC.HashPassword(superPassword, salt);

            // --- 1. Admin ---
            var superAdmin = new Admin
            {
                FirstName = "System",
                LastName = "Master",
                Username = superEmail.Split('@')[0],
                Email = superEmail,
                PasswordHash = superPasswordHash,
                Role = UserRole.Admin,
                LastSeen = DateTime.UtcNow,
                Department = "Board",
                PhoneNumber = "00420123456789",
                IsEmailVerified = true
            };

            // --- 2. School Classes ---
            var class3B = new SchoolClass
            {
                Name = "3.B",
                AcademicYear = "2025-2026",
                Section = "B"
            };

            var class3A = new SchoolClass
            {
                Name = "3.A",
                AcademicYear = "2025-2026",
                Section = "A"
            };

            context.SchoolClasses.AddRange(class3B, class3A);

            // --- 3. Teachers ---
            var teacherDenda = new Teacher
            {
                FirstName = "Denda",
                LastName = "Valachu",
                Username = "teacher_denda",
                Email = "denda@sqeez.org",
                PasswordHash = defaultPassword,
                Role = UserRole.Teacher,
                LastSeen = DateTime.UtcNow,
                Department = "Mathematics & Sciences",
                ManagedClass = class3B,
                IsEmailVerified = true
            };

            var teacherJana = new Teacher
            {
                FirstName = "Jana",
                LastName = "Hrouzkova",
                Username = "teacher_jana",
                Email = "jana@sqeez.org",
                PasswordHash = defaultPassword,
                Role = UserRole.Teacher,
                LastSeen = DateTime.UtcNow,
                Department = "Languages",
                ManagedClass = class3A,
                IsEmailVerified = true
            };

            var avatarsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
            if (!Directory.Exists(avatarsFolder)) Directory.CreateDirectory(avatarsFolder);

            // A tiny grey 1x1 pixel PNG for the default avatar
            string base64AvatarPng = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";
            byte[] avatarBytes = Convert.FromBase64String(base64AvatarPng);

            if (!File.Exists(Path.Combine(avatarsFolder, "default-avatar.png")))
            {
                await File.WriteAllBytesAsync(Path.Combine(avatarsFolder, "default-avatar.png"), avatarBytes);
            }

            // --- 4. Students ---
            var studentTonda = new Student
            {
                FirstName = "Antonín",
                LastName = "Tučný",
                Username = "student_tonda",
                Email = "tonda@sqeez.org",
                PasswordHash = defaultPassword,
                Role = UserRole.Student,
                LastSeen = DateTime.UtcNow,
                SchoolClass = class3B,
                AvatarUrl = "/avatars/default-avatar.png",
                IsEmailVerified = true,
            };

            var studentPepa = new Student
            {
                FirstName = "Josef",
                LastName = "Nohavica",
                Username = "student_pepa",
                Email = "pepa@sqeez.org",
                PasswordHash = defaultPassword,
                Role = UserRole.Student,
                LastSeen = DateTime.UtcNow,
                SchoolClass = class3B,
                IsEmailVerified = true,
            };

            var studentKarel = new Student
            {
                FirstName = "Karel",
                LastName = "Eisenstadt",
                Username = "student_karel",
                Email = "karel@sqeez.org",
                PasswordHash = defaultPassword,
                Role = UserRole.Student,
                LastSeen = DateTime.UtcNow,
                SchoolClass = class3A,
                IsEmailVerified = true,
            };

            var studentEva = new Student
            {
                FirstName = "Eva",
                LastName = "Tomanová",
                Username = "student_eva",
                Email = "eva@sqeez.org",
                PasswordHash = defaultPassword,
                Role = UserRole.Student,
                LastSeen = DateTime.UtcNow,
                SchoolClass = class3A,
                IsEmailVerified = true,
            };

            context.Students.AddRange(superAdmin, teacherDenda, teacherJana, studentTonda, studentPepa, studentKarel, studentEva);

            // --- 5. Subjects ---
            var mathSubject = new Subject
            {
                Name = "Advanced Mathematics",
                Code = "MATH-3B",
                Description = "Calculus, Algebra, and Geometry",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(10),
                Teacher = teacherDenda,
                SchoolClass = class3B
            };

            var physicsSubject = new Subject
            {
                Name = "Physics",
                Code = "PHYS-3B",
                Description = "Mechanics and Thermodynamics",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(10),
                Teacher = teacherDenda,
                SchoolClass = class3B
            };

            var englishSubject = new Subject
            {
                Name = "English Literature",
                Code = "ENGL-3A",
                Description = "Shakespeare to Modern Era",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(10),
                Teacher = teacherJana,
                SchoolClass = class3A
            };

            context.Subjects.AddRange(mathSubject, physicsSubject, englishSubject);

            // --- 6. Enrollments ---
            var enrollments = new List<Enrollment>
            {
                new Enrollment { Student = studentTonda, Subject = mathSubject, EnrolledAt = DateTime.UtcNow },
                new Enrollment { Student = studentTonda, Subject = physicsSubject, EnrolledAt = DateTime.UtcNow },
                new Enrollment { Student = studentPepa, Subject = mathSubject, EnrolledAt = DateTime.UtcNow },
                new Enrollment { Student = studentKarel, Subject = englishSubject, EnrolledAt = DateTime.UtcNow },
                new Enrollment { Student = studentEva, Subject = englishSubject, EnrolledAt = DateTime.UtcNow },
                new Enrollment { Student = studentEva, Subject = mathSubject, EnrolledAt = DateTime.UtcNow }
            };

            context.Enrollments.AddRange(enrollments);

            // --- 7. Physical Media Files & Media Assets ---
            var mediaFolder = Path.Combine(Directory.GetCurrentDirectory(), "SecureStorage", "media");

            if (!Directory.Exists(mediaFolder))
            {
                Directory.CreateDirectory(mediaFolder);
            }

            var sampleFileName = "seed-sample-image.png";
            var physicalPath = Path.Combine(mediaFolder, sampleFileName);

            if (!File.Exists(physicalPath))
            {
                string base64Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";
                byte[] imageBytes = Convert.FromBase64String(base64Png);
                await File.WriteAllBytesAsync(physicalPath, imageBytes);
            }

            var sampleMedia = new MediaAsset
            {
                LocationUrl = $"/secure/media/{sampleFileName}",
                MimeType = MediaType.Image,
                IsPrivate = false,
                Description = "A helpful diagram for the Algebra midterm.",
                Owner = teacherDenda
            };

            context.MediaAssets.Add(sampleMedia);

            // --- 8. Quizzes, Questions, and Options ---
            var mathQuiz = new Quiz
            {
                Title = "Algebra Midterm",
                Description = "Testing your basic algebra skills.",
                MaxRetries = 2,
                CreatedAt = DateTime.UtcNow,
                PublishDate = DateTime.UtcNow,
                Subject = mathSubject,
                QuizQuestions = new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        Title = "What is the value of x if 2x = 10? (See attached diagram)",
                        Difficulty = 1,
                        TimeLimit = 30,
                        Media = sampleMedia,
                        Options = new List<QuizOption>
                        {
                            new QuizOption { Text = "4", IsCorrect = false, IsFreeText = false },
                            new QuizOption { Text = "5", IsCorrect = true, IsFreeText = false },
                            new QuizOption { Text = "10", IsCorrect = false, IsFreeText = false }
                        }
                    },
                    new QuizQuestion
                    {
                        Title = "Type the formula for the area of a circle:",
                        Difficulty = 2,
                        TimeLimit = 60,
                        Options = new List<QuizOption>
                        {
                            new QuizOption { Text = "pi*r^2", IsCorrect = true, IsFreeText = true }
                        }
                    }
                }
            };

            var englishQuiz = new Quiz
            {
                Title = "Shakespeare Pop Quiz",
                Description = "A quick check on our recent reading.",
                MaxRetries = 1,
                CreatedAt = DateTime.UtcNow,
                PublishDate = DateTime.UtcNow.AddDays(1),
                Subject = englishSubject,
                QuizQuestions = new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        Title = "Which of the following is a tragedy by William Shakespeare?",
                        Difficulty = 1,
                        TimeLimit = 45,
                        Options = new List<QuizOption>
                        {
                            new QuizOption { Text = "A Midsummer Night's Dream", IsCorrect = false, IsFreeText = false },
                            new QuizOption { Text = "Hamlet", IsCorrect = true, IsFreeText = false },
                            new QuizOption { Text = "The Comedy of Errors", IsCorrect = false, IsFreeText = false }
                        }
                    }
                }
            };

            context.Quizzes.AddRange(mathQuiz, englishQuiz);

            // --- 9. Gamification Badges ---

            var badgesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "badges");
            if (!Directory.Exists(badgesFolder)) Directory.CreateDirectory(badgesFolder);

            string base64BadgePng = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=";
            byte[] badgeBytes = Convert.FromBase64String(base64BadgePng);

            if (!File.Exists(Path.Combine(badgesFolder, "perfect-score.png"))) await File.WriteAllBytesAsync(Path.Combine(badgesFolder, "perfect-score.png"), badgeBytes);
            if (!File.Exists(Path.Combine(badgesFolder, "high-scorer.png"))) await File.WriteAllBytesAsync(Path.Combine(badgesFolder, "high-scorer.png"), badgeBytes);
            if (!File.Exists(Path.Combine(badgesFolder, "participant.png"))) await File.WriteAllBytesAsync(Path.Combine(badgesFolder, "participant.png"), badgeBytes);

            var perfectScoreBadge = new Badge
            {
                Name = "Perfect Score",
                Description = "You scored 100% on a quiz! Outstanding!",
                IconUrl = "/badges/perfect-score.png",
                XpBonus = 100,
                Rules = new List<BadgeRule>
                {
                    new BadgeRule { Metric = BadgeMetric.ScorePercentage, Operator = BadgeOperator.Equals, TargetValue = 100 }
                }
            };

            var highScorerBadge = new Badge
            {
                Name = "High Scorer",
                Description = "You scored at least 80% on a quiz. Great job!",
                IconUrl = "/badges/high-scorer.png",
                XpBonus = 50,
                Rules = new List<BadgeRule>
                {
                    new BadgeRule { Metric = BadgeMetric.ScorePercentage, Operator = BadgeOperator.GreaterThanOrEqual, TargetValue = 80 }
                }
            };

            var participantBadge = new Badge
            {
                Name = "First Steps",
                Description = "You completed a quiz and earned your first points!",
                IconUrl = "/badges/participant.png",
                XpBonus = 25,
                Rules = new List<BadgeRule>
                {
                    new BadgeRule { Metric = BadgeMetric.TotalScore, Operator = BadgeOperator.GreaterThan, TargetValue = 0 }
                }
            };

            context.Badges.AddRange(perfectScoreBadge, highScorerBadge, participantBadge);

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Ensures the singleton system configuration row exists.
        /// </summary>
        private static async Task EnsureSystemConfigAsync(SqeezDbContext context)
        {
            if (!await context.SystemConfigs.AnyAsync(systemConfig => systemConfig.Id == 1))
            {
                context.SystemConfigs.Add(new SystemConfig { Id = 1 });
            }
        }

        /// <summary>
        /// Ensures the configured superadmin email belongs to an admin account.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when required superadmin configuration is missing or the configured email already belongs to a non-admin user.
        /// </exception>
        private static async Task EnsureSuperAdminAsync(SqeezDbContext context, IConfiguration config)
        {
            string superEmail = GetRequiredConfig(config, "SUPER_USER_EMAIL").Trim().ToLowerInvariant();
            string superPassword = GetRequiredConfig(config, "SUPER_USER_DEFAULT_PASSWORD");

            var existingUser = await context.Students.FirstOrDefaultAsync(user => user.Email.ToLower() == superEmail);

            if (existingUser is Admin)
            {
                return;
            }

            if (existingUser != null)
            {
                throw new InvalidOperationException("SUPER_USER_EMAIL already belongs to a non-admin user.");
            }

            var username = await BuildUniqueUsernameAsync(context, superEmail);

            context.Admins.Add(new Admin
            {
                FirstName = config["SUPER_USER_FIRST_NAME"] ?? "System",
                LastName = config["SUPER_USER_LAST_NAME"] ?? "Administrator",
                Username = username,
                Email = superEmail,
                PasswordHash = BC.HashPassword(superPassword, BC.GenerateSalt(12)),
                Role = UserRole.Admin,
                LastSeen = DateTime.UtcNow,
                Department = config["SUPER_USER_DEPARTMENT"] ?? "Administration",
                PhoneNumber = config["SUPER_USER_PHONE_NUMBER"] ?? string.Empty,
                IsEmailVerified = true
            });
        }

        /// <summary>
        /// Reads a required configuration value and fails fast when it is missing.
        /// </summary>
        private static string GetRequiredConfig(IConfiguration config, string key)
        {
            var value = config[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{key} must be configured before seeding the database.");
            }

            return value;
        }

        /// <summary>
        /// Builds a username from the email local part and appends a numeric suffix until it is unique.
        /// </summary>
        private static async Task<string> BuildUniqueUsernameAsync(SqeezDbContext context, string email)
        {
            var baseUsername = email.Split('@')[0].Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(baseUsername))
            {
                baseUsername = "admin";
            }

            var username = baseUsername;
            var suffix = 1;

            while (await context.Students.AnyAsync(user => user.Username == username))
            {
                username = $"{baseUsername}{suffix}";
                suffix++;
            }

            return username;
        }
    }
}
