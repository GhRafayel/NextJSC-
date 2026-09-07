using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Backend.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected int? GetAuthenticatedUserId () {
        string? res = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
            User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(res, out var id) ? id : null;
    }
}
