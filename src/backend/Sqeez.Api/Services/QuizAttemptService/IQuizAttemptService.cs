using Sqeez.Api.DTOs;

namespace Sqeez.Api.Services.Interfaces
{
    /// <summary>
    /// Defines quiz attempt lifecycle, response submission, grading, and attempt cleanup operations.
    /// </summary>
    public interface IQuizAttemptService
    {
        /// <summary>
        /// Creates a new quiz attempt for an enrolled student and returns the first question to answer.
        /// </summary>
        /// <param name="studentId">The student starting the attempt.</param>
        /// <param name="dto">Quiz and enrollment identifiers.</param>
        /// <returns>
        /// The created attempt. Returns forbidden when the student is not enrolled, not found when the quiz is
        /// missing or belongs to a different subject, validation failed when the quiz is unpublished or closed,
        /// or conflict when the retry limit is reached.
        /// </returns>
        Task<ServiceResult<QuizAttemptDto>> StartAttemptAsync(long studentId, StartQuizAttemptDto dto);

        /// <summary>
        /// Saves a student's answer for one question during an active attempt.
        /// </summary>
        /// <param name="attemptId">The attempt id.</param>
        /// <param name="studentId">The student submitting the answer.</param>
        /// <param name="dto">Question response payload, including selected options or free-text answer.</param>
        /// <returns>
        /// The submitted response, correct-answer metadata, and next question id. Returns not found for missing
        /// attempt or question, forbidden for another student's attempt, or conflict when the attempt is no longer
        /// in progress or the question was already answered.
        /// </returns>
        Task<ServiceResult<QuestionAnsweredDto>> SubmitAnswerAsync(long attemptId, long studentId, SubmitQuestionResponseDto dto);

        /// <summary>
        /// Returns the next unanswered question id for a created or started attempt.
        /// </summary>
        /// <param name="attemptId">The attempt id.</param>
        /// <param name="studentId">The student who owns the attempt.</param>
        /// <returns>
        /// The next question id, or null when no pending questions remain. Returns not found, forbidden, or
        /// conflict when the attempt cannot continue.
        /// </returns>
        Task<ServiceResult<long?>> GetNextPendingQuestionIdAsync(long attemptId, long studentId);

        /// <summary>
        /// Finalizes an in-progress attempt and calculates score, XP, and badge rewards when possible.
        /// </summary>
        /// <param name="attemptId">The attempt id.</param>
        /// <param name="studentId">The student completing the attempt.</param>
        /// <returns>
        /// The completed or pending-correction attempt. Attempts containing ungraded free-text answers are set to
        /// pending correction; fully auto-graded attempts are completed and may award XP and badges.
        /// </returns>
        Task<ServiceResult<QuizAttemptDto>> CompleteAttemptAsync(long attemptId, long studentId);

        /// <summary>
        /// Gets full attempt details, including submitted answers.
        /// </summary>
        /// <param name="attemptId">The attempt id.</param>
        /// <param name="currentUserId">The requesting user id.</param>
        /// <param name="currentUserRole">The requesting role.</param>
        /// <returns>
        /// Attempt details. Students can view only their own attempts, teachers only attempts for quizzes in
        /// their subjects, and admins can view any attempt. Returns not found or forbidden when access is not allowed.
        /// </returns>
        Task<ServiceResult<QuizAttemptDetailDto>> GetAttemptDetailsAsync(long attemptId, long currentUserId, string currentUserRole);

        /// <summary>
        /// Gets paged attempts for a quiz.
        /// </summary>
        /// <param name="quizId">The quiz id.</param>
        /// <param name="teacherId">The requesting user id; quiz owners receive all attempts, students receive only their own.</param>
        /// <param name="pageNumber">One-based page number.</param>
        /// <param name="pageSize">Number of attempts per page; clamped to the configured maximum page size.</param>
        /// <returns>A paged attempt list, or not found when the quiz does not exist.</returns>
        Task<ServiceResult<PagedResponse<QuizAttemptDto>>> GetAttemptsForQuizAsync(long quizId, long teacherId, int pageNumber = 1, int pageSize = 20);


        // --- TEACHER ACTIONS ---

        /// <summary>
        /// Grades a free-text response and updates the parent attempt score.
        /// </summary>
        /// <param name="responseId">The question response id.</param>
        /// <param name="teacherId">The teacher grading the response.</param>
        /// <param name="dto">Score and liked-state values.</param>
        /// <returns>
        /// The updated response. When the last ungraded response is graded, the attempt becomes completed and
        /// rewards are processed. Returns not found or forbidden when the teacher does not own the subject.
        /// </returns>
        Task<ServiceResult<QuestionResponseDto>> GradeFreeTextResponseAsync(long responseId, long teacherId, GradeQuestionResponseDto dto);

        /// <summary>
        /// Deletes a single attempt for a quiz owned by the teacher.
        /// </summary>
        /// <param name="attemptId">The attempt id.</param>
        /// <param name="teacherId">The teacher requesting deletion.</param>
        /// <returns>A successful result, not found for a missing attempt, or forbidden for non-owner teachers.</returns>
        Task<ServiceResult<bool>> DeleteAttemptAsync(long attemptId, long teacherId);

        /// <summary>
        /// Deletes all attempts for a quiz.
        /// </summary>
        /// <param name="quizId">The quiz id.</param>
        /// <param name="teacherId">The requesting teacher id.</param>
        /// <param name="isAdmin">Whether the requester can bypass teacher ownership checks.</param>
        /// <returns>
        /// A successful result, including when no attempts exist. Returns not found for a missing quiz or
        /// forbidden when a non-admin requester is not the subject teacher.
        /// </returns>
        Task<ServiceResult<bool>> DeleteAllAttemptsForQuizAsync(long quizId, long teacherId, bool isAdmin = false);
    }
}
