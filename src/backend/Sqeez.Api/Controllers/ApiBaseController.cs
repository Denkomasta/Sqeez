using Microsoft.AspNetCore.Mvc;
using Sqeez.Api.DTOs;
using Sqeez.Api.Enums;

namespace Sqeez.Api.Controllers
{
    /// <summary>
    /// Shared controller helpers for reading authenticated user claims and converting service results to HTTP responses.
    /// </summary>
    [ApiController]
    public class ApiBaseController : ControllerBase
    {
        /// <summary>
        /// Gets the current user's id claim, or 0 when the claim is missing or malformed.
        /// </summary>
        protected long CurrentUserId
        {
            get
            {
                var idString = GetUserIdFromClaims();
                return long.TryParse(idString, out long id) ? id : 0;
            }
        }

        /// <summary>
        /// Gets whether the current request carries the Admin role claim.
        /// </summary>
        protected bool IsCurrentUserAdmin => GetUserRoleFromClaims() == "Admin";

        /// <summary>
        /// Reads the authenticated user's identifier claim.
        /// </summary>
        protected string? GetUserIdFromClaims()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Reads the authenticated user's role claim.
        /// </summary>
        protected string? GetUserRoleFromClaims()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        }

        /// <summary>
        /// Checks whether the supplied user id belongs to the authenticated user.
        /// </summary>
        /// <param name="userId">The user id to compare.</param>
        /// <param name="claimedId">Optional already parsed claim value.</param>
        protected bool IsIdLoggedUser(long userId, long? claimedId = null)
        {
            if (claimedId.HasValue)
            {
                return userId == claimedId.Value;
            }

            var idString = GetUserIdFromClaims();

            if (long.TryParse(idString, out long parsedClaimedId))
            {
                return parsedClaimedId == userId;
            }

            return false;
        }

        /// <summary>
        /// Converts a service-layer result into a consistent HTTP response.
        /// </summary>
        /// <typeparam name="T">The result payload type.</typeparam>
        /// <param name="result">The service result to map.</param>
        protected ActionResult HandleServiceResult<T>(ServiceResult<T> result)
        {
            if (result.Success)
            {
                return result.Data == null ? NotFound() : Ok(result.Data);
            }

            return result.ErrorCode switch
            {
                ServiceError.NotFound => NotFound(new { error = result.ErrorMessage }),
                ServiceError.Conflict => Conflict(new { error = result.ErrorMessage }),
                ServiceError.Unauthorized => Unauthorized(new { error = result.ErrorMessage }),
                ServiceError.ValidationFailed => BadRequest(new { error = result.ErrorMessage }),
                ServiceError.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.ErrorMessage }),
                ServiceError.InternalError => StatusCode(StatusCodes.Status500InternalServerError, new { error = result.ErrorMessage }),
                ServiceError.TooManyRequests => StatusCode(StatusCodes.Status429TooManyRequests, new { error = result.ErrorMessage }),
                _ => BadRequest(new { error = result.ErrorMessage })
            };
        }
    }
}
