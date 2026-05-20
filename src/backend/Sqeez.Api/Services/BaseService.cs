using Sqeez.Api.Data;

namespace Sqeez.Api.Services
{
    /// <summary>
    /// Base type for services that need database access and structured logging.
    /// </summary>
    /// <typeparam name="TService">Concrete service type used as the logger category.</typeparam>
    public abstract class BaseService<TService>
    {
        protected readonly SqeezDbContext _context;
        protected readonly ILogger<TService> _logger;

        /// <summary>
        /// Initializes shared database and logging dependencies.
        /// </summary>
        /// <param name="context">Application database context.</param>
        /// <param name="logger">Logger for the concrete service type.</param>
        protected BaseService(SqeezDbContext context, ILogger<TService> logger)
        {
            _context = context;
            _logger = logger;
        }
    }
}
