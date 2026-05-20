using Microsoft.EntityFrameworkCore;
using Sqeez.Api.Enums;
using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.Gamification;
using Sqeez.Api.Models.Media;
using Sqeez.Api.Models.QuizSystem;
using Sqeez.Api.Models.System;
using Sqeez.Api.Models.Users;

namespace Sqeez.Api.Data
{
    /// <summary>
    /// Entity Framework Core database context for the Sqeez backend.
    /// </summary>
    /// <remarks>
    /// The context stores users, academics, quiz content, quiz attempts, media assets, badges, sessions,
    /// and the singleton system configuration. Relationship delete behavior is configured explicitly to avoid
    /// accidental removal of historical quiz and enrollment data.
    /// </remarks>
    public class SqeezDbContext : DbContext
    {
        /// <summary>
        /// Creates a Sqeez database context using the configured EF Core options.
        /// </summary>
        /// <param name="options">Database provider and connection options.</param>
        public SqeezDbContext(DbContextOptions<SqeezDbContext> options) : base(options) { }

        /// <summary>
        /// Singleton application configuration row.
        /// </summary>
        public DbSet<SystemConfig> SystemConfigs { get; set; } = null!;

        /// <summary>
        /// Base user set for all student-shaped accounts. Teachers and admins are stored in the same Users table.
        /// </summary>
        public DbSet<Student> Students { get; set; } = null!;

        /// <summary>
        /// Teacher subtype view over the Users table.
        /// </summary>
        public DbSet<Teacher> Teachers { get; set; } = null!;

        /// <summary>
        /// Admin subtype view over the Users table.
        /// </summary>
        public DbSet<Admin> Admins { get; set; } = null!;

        /// <summary>
        /// School classes that can group students, subjects, and an optional managing teacher.
        /// </summary>
        public DbSet<SchoolClass> SchoolClasses { get; set; } = null!;

        /// <summary>
        /// Subjects taught by optional teachers and optionally assigned to school classes.
        /// </summary>
        public DbSet<Subject> Subjects { get; set; } = null!;

        /// <summary>
        /// Student-to-subject enrollments used as the access root for quiz attempts.
        /// </summary>
        public DbSet<Enrollment> Enrollments { get; set; } = null!;

        /// <summary>
        /// Badge definitions that can award XP and contain badge rules.
        /// </summary>
        public DbSet<Badge> Badges { get; set; } = null!;

        /// <summary>
        /// Join records for badges earned by students.
        /// </summary>
        public DbSet<StudentBadge> StudentBadges { get; set; } = null!;

        /// <summary>
        /// Quizzes attached to subjects.
        /// </summary>
        public DbSet<Quiz> Quizzes { get; set; } = null!;

        /// <summary>
        /// Questions that belong to quizzes.
        /// </summary>
        public DbSet<QuizQuestion> QuizQuestions { get; set; } = null!;

        /// <summary>
        /// Answer options that belong to quiz questions.
        /// </summary>
        public DbSet<QuizOption> QuizOptions { get; set; } = null!;

        /// <summary>
        /// Student attempts for quizzes through enrollments.
        /// </summary>
        public DbSet<QuizAttempt> QuizAttempts { get; set; } = null!;

        /// <summary>
        /// Stored responses submitted for quiz questions.
        /// </summary>
        public DbSet<QuizQuestionResponse> QuizQuestionResponses { get; set; } = null!;

        /// <summary>
        /// Metadata for uploaded media files used by quiz questions and options.
        /// </summary>
        public DbSet<MediaAsset> MediaAssets { get; set; } = null!;

        /// <summary>
        /// Rule rows that define badge-award conditions.
        /// </summary>
        public DbSet<BadgeRule> BadgeRules { get; set; } = null!;

        /// <summary>
        /// Refresh-token sessions for authenticated users.
        /// </summary>
        public DbSet<UserSession> UserSessions { get; set; } = null!;

        /// <summary>
        /// Configures inheritance, indexes, many-to-many joins, delete behavior, and default seed data.
        /// </summary>
        /// <param name="modelBuilder">EF Core model builder.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Student, Teacher, and Admin are stored in one Users table with Role as the discriminator.
            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("Users");

                entity.HasIndex(u => u.Email)
                      .IsUnique();

                entity.HasIndex(u => u.Username)
                      .IsUnique();

                entity.HasDiscriminator(s => s.Role)
                      .HasValue<Student>(UserRole.Student)
                      .HasValue<Teacher>(UserRole.Teacher)
                      .HasValue<Admin>(UserRole.Admin);
            });

            // A student can earn a badge only once.
            modelBuilder.Entity<StudentBadge>()
                .HasKey(sb => new { sb.StudentId, sb.BadgeId });

            modelBuilder.Entity<StudentBadge>()
                .HasOne(sb => sb.Student)
                .WithMany(s => s.StudentBadges)
                .HasForeignKey(sb => sb.StudentId);

            modelBuilder.Entity<StudentBadge>()
                .HasOne(sb => sb.Badge)
                .WithMany(b => b.StudentBadges)
                .HasForeignKey(sb => sb.BadgeId);

            // Enrollment is the root of quiz history, so student/subject deletion must be explicit in services.
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Subject)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QuizAttempt>()
                .HasOne(qa => qa.Enrollment)
                .WithMany(e => e.QuizAttempts)
                .HasForeignKey(qa => qa.EnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Choice responses can select multiple options, and options can be selected by many responses.
            modelBuilder.Entity<QuizOption>()
                .HasMany(qo => qo.Responses)
                .WithMany(qqr => qqr.Options)
                .UsingEntity(j => j.ToTable("QuizOptionResponses"));

            // Each attempt can store only one response per question.
            modelBuilder.Entity<QuizQuestionResponse>()
                .HasIndex(r => new { r.QuizAttemptId, r.QuizQuestionId })
                .IsUnique();

            // Removing a class keeps users and clears their student-side class assignment.
            modelBuilder.Entity<SchoolClass>()
                .HasMany(sc => sc.Students)
                .WithOne(s => s.SchoolClass)
                .HasForeignKey(s => s.SchoolClassId)
                .OnDelete(DeleteBehavior.SetNull);

            // The managed-class foreign key lives on Teacher because one teacher can manage at most one class.
            modelBuilder.Entity<SchoolClass>()
                .HasOne(sc => sc.Teacher)
                .WithOne(t => t.ManagedClass)
                .HasForeignKey<Teacher>(t => t.ManagedClassId)
                .OnDelete(DeleteBehavior.SetNull);

            // Badge rules are owned by the badge definition and should not outlive it.
            modelBuilder.Entity<Badge>()
                .HasMany(b => b.Rules)
                .WithOne(r => r.Badge)
                .HasForeignKey(r => r.BadgeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // System configuration is treated as a singleton with a stable primary key.
            modelBuilder.Entity<SystemConfig>().HasData(
                new SystemConfig { Id = 1 }
            );
        }
    }
}
