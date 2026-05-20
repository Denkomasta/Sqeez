using Sqeez.Api.Models.Academics;
using Sqeez.Api.Models.Media;

namespace Sqeez.Api.Models.Users
{
    /// <summary>
    /// User account with teacher capabilities, subject ownership, and optional class management.
    /// </summary>
    public class Teacher : Student
    {
        /// <summary>
        /// Teacher's department or organizational unit.
        /// </summary>
        public string? Department { get; set; }

        /// <summary>
        /// Identifier of the class managed by this teacher, when assigned.
        /// </summary>
        public long? ManagedClassId { get; set; }

        /// <summary>
        /// Class managed by this teacher.
        /// </summary>
        public SchoolClass? ManagedClass { get; set; }

        /// <summary>
        /// Subjects taught by this teacher.
        /// </summary>
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();

        /// <summary>
        /// Media assets owned by this teacher or admin account.
        /// </summary>
        public ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();
    }
}
