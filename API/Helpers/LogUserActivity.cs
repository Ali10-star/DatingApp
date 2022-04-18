using API.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Helpers
{
    public class LogUserActivity : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Get the context after the user does something
            var resultContext = await next();

            if (!resultContext.HttpContext.User.Identity.IsAuthenticated) return;

            int userId = resultContext.HttpContext.User.GetUserId();
            var unitOfWork = resultContext.HttpContext.RequestServices.GetService<IUnitOfWork>();
            var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId);

            user.LastActive = DateTime.UtcNow;

            await unitOfWork.Complete();
        }
    }
}