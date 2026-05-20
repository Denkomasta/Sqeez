namespace Sqeez.Api.Enums
{
    /// <summary>
    /// Application role used for authorization and user discriminator behavior.
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Learner account that can enroll in subjects and take quizzes.
        /// </summary>
        Student,

        /// <summary>
        /// Teaching account that can own subjects, manage quiz content, and grade attempts.
        /// </summary>
        Teacher,

        /// <summary>
        /// Administrative account with system management permissions.
        /// </summary>
        Admin
    }

    /// <summary>
    /// Lifecycle state of a quiz attempt.
    /// </summary>
    public enum AttemptStatus
    {
        /// <summary>
        /// Attempt record exists but no answer has been submitted yet.
        /// </summary>
        Created,

        /// <summary>
        /// Attempt is in progress and accepts answers.
        /// </summary>
        Started,

        /// <summary>
        /// Attempt has been fully evaluated and closed.
        /// </summary>
        Completed,

        /// <summary>
        /// Attempt is waiting for manual correction, usually because it contains free-text answers.
        /// </summary>
        PendingCorrection,

        /// <summary>
        /// Attempt was abandoned before completion.
        /// </summary>
        Abandoned
    }

    /// <summary>
    /// High-level media category used for upload validation, storage, and response metadata.
    /// </summary>
    public enum MediaType
    {
        /// <summary>
        /// Image file such as JPEG, PNG, or GIF.
        /// </summary>
        Image,

        /// <summary>
        /// Video file.
        /// </summary>
        Video,

        /// <summary>
        /// Audio file.
        /// </summary>
        Audio,

        /// <summary>
        /// Document or other supported non-media file.
        /// </summary>
        Document
    }

    /// <summary>
    /// Service-layer error category mapped by controllers to HTTP responses.
    /// </summary>
    public enum ServiceError
    {
        /// <summary>
        /// No error occurred.
        /// </summary>
        None = 0,

        /// <summary>
        /// Requested resource was not found.
        /// </summary>
        NotFound = 1,

        /// <summary>
        /// Input failed validation.
        /// </summary>
        ValidationFailed = 2,

        /// <summary>
        /// Operation conflicts with existing data or state.
        /// </summary>
        Conflict = 3,

        /// <summary>
        /// Request lacks valid authentication.
        /// </summary>
        Unauthorized = 4,

        /// <summary>
        /// Authenticated user is not allowed to perform the operation.
        /// </summary>
        Forbidden = 5,

        /// <summary>
        /// Unexpected server-side failure.
        /// </summary>
        InternalError = 6,

        /// <summary>
        /// Request is malformed or cannot be processed in its current shape.
        /// </summary>
        BadRequest = 7,

        /// <summary>
        /// Request rate or session limit has been exceeded.
        /// </summary>
        TooManyRequests = 8,
    }

    /// <summary>
    /// Metric evaluated by badge rules after quiz attempt completion or grading.
    /// </summary>
    public enum BadgeMetric
    {
        /// <summary>
        /// Percentage score achieved on a quiz attempt.
        /// </summary>
        ScorePercentage = 1,

        /// <summary>
        /// Raw total score achieved on a quiz attempt.
        /// </summary>
        TotalScore = 2,

        /// <summary>
        /// Count of perfectly answered questions in a quiz attempt.
        /// </summary>
        PerfectAnswersCount = 3,

        /// <summary>
        /// Number of quiz attempts completed by the student.
        /// </summary>
        TotalAttempts = 4,
    }

    /// <summary>
    /// Comparison operator used by badge rule evaluation.
    /// </summary>
    public enum BadgeOperator
    {
        /// <summary>
        /// Metric must equal the target value.
        /// </summary>
        Equals = 1,

        /// <summary>
        /// Metric must be greater than the target value.
        /// </summary>
        GreaterThan = 2,

        /// <summary>
        /// Metric must be greater than or equal to the target value.
        /// </summary>
        GreaterThanOrEqual = 3,

        /// <summary>
        /// Metric must be less than the target value.
        /// </summary>
        LessThan = 4,

        /// <summary>
        /// Metric must be less than or equal to the target value.
        /// </summary>
        LessThanOrEqual = 5,

        /// <summary>
        /// Metric must not equal the target value.
        /// </summary>
        NotEquals = 6
    }
}
