using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Vector.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    // TODO: Implement user management endpoints
    // - GET /api/users/me
    // - PUT /api/users/me
    // - DELETE /api/users/me
    // - GET /api/users/:id (public profile)
    // - PUT /api/users/me/password
    // - POST /api/users/me/profile-picture
    // - DELETE /api/users/me/profile-picture
}

