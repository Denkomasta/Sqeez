using Sqeez.Api.Models.Users;

namespace Sqeez.Api.Models.Academics
{
    /// <summary>
    /// School class grouping students and subjects for an academic year.
    /// </summary>
    public class SchoolClass
    {
        /// <summary>
        /// Primary identifier of the class.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Display name of the class.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Academic year associated with the class.
        /// </summary>
        public string AcademicYear { get; set; } = string.Empty;

        /// <summary>
        /// Section or subgroup label within the year.
        /// </summary>
        public string Section { get; set; } = string.Empty;

        /// <summary>
        /// Teacher responsible for managing this class, when assigned.
        /// </summary>
        public Teacher? Teacher { get; set; }

        /// <summary>
        /// Students currently assigned to the class.
        /// </summary>
        public ICollection<Student> Students { get; set; } = new List<Student>();

        /// <summary>
        /// Subjects taught to this class.
        /// </summary>
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    }
}
