
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Controllers;

[ApiController]
[Route("api/platform-owner")]
[Authorize(Roles = UserRoles.PlatformOwner)]
public class PlatformOwnerController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public PlatformOwnerController(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // POST /api/platform-owner/administrators/{id}
    // Назначить пользователя администратором.
    [HttpPost("administrators/{id}")]
    public async Task<IActionResult> AssignAdministrator(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound(new
            {
                message = "Пользователь не найден."
            });
        }

        if (await _userManager.IsInRoleAsync(
            user, UserRoles.Administrator))
        {
            return Conflict(new
            {
                message = "Пользователь уже является администратором."
            });
        }

        var result = await _userManager.AddToRoleAsync(
            user, UserRoles.Administrator);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(x => x.Description)
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            user.Id,
            user.Email,
            Roles = roles
        });
    }

    // DELETE /api/platform-owner/administrators/{id}
    // Отозвать права администратора.
    [HttpDelete("administrators/{id}")]
    public async Task<IActionResult> RevokeAdministrator(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound(new
            {
                message = "Пользователь не найден."
            });
        }

        // Нельзя снимать права администратора
        // с владельца платформы.
        if (await _userManager.IsInRoleAsync(
            user, UserRoles.PlatformOwner))
        {
            return Conflict(new
            {
                message =
                    "Нельзя отозвать права администратора " +
                    "у владельца платформы."
            });
        }

        if (!await _userManager.IsInRoleAsync(
            user, UserRoles.Administrator))
        {
            return Conflict(new
            {
                message = "Пользователь не является администратором."
            });
        }

        var result = await _userManager.RemoveFromRoleAsync(
            user, UserRoles.Administrator);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(x => x.Description)
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            user.Id,
            user.Email,
            Roles = roles
        });
    }
}
