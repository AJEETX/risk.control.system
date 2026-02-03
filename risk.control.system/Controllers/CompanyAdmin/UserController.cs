using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Services.Api;

namespace risk.control.system.Controllers.CompanyAdmin
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{PORTAL_ADMIN.DISPLAY_NAME}")]
    public class UserController : ControllerBase
    {
        private readonly IUserService userService;
        private readonly ILogger<UserController> logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            this.userService = userService;
            this.logger = logger;
        }

        [HttpGet("AllUsers")]
        public async Task<IActionResult> AllUsers()
        {
            var userEmail = HttpContext.User?.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User not authenticated.");
            }
            try
            {
                var activeUsersDetails = await userService.GetUsers(userEmail);

                return Ok(activeUsersDetails);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting notifications for user {UserEmail}", userEmail);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}