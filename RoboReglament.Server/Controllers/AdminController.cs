using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboReglament.Server.DTOs.Admin;
using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = UserRoles.Administrator)]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToListAsync();

        var result = new List<object>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            result.Add(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.MiddleName,
                Roles = roles
            });
        }

        return Ok(result);
    }

    [HttpPut("users/{id}/roles")]
    public async Task<IActionResult> UpdateRoles(
        string id,
        UpdateUserRolesRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound(new
            {
                message = "Пользователь не найден."
            });
        }

        string[] allowedRoles =
        [
            UserRoles.Organizer,
            UserRoles.Judge,
            UserRoles.Coach,
            UserRoles.Participant
        ];

        var requestedRoles = request.Roles
            .Distinct()
            .ToList();

        var invalidRoles = requestedRoles
            .Where(role => !allowedRoles.Contains(role))
            .ToList();

        if (invalidRoles.Count > 0)
        {
            return BadRequest(new
            {
                message = "Указаны недопустимые роли.",
                roles = invalidRoles
            });
        }

        var currentRoles = await _userManager.GetRolesAsync(user);

        // Administrator через этот endpoint не трогаем.
        var removableRoles = currentRoles
            .Where(role => allowedRoles.Contains(role))
            .ToList();

        if (removableRoles.Count > 0)
        {
            var removeResult =
                await _userManager.RemoveFromRolesAsync(
                    user,
                    removableRoles);

            if (!removeResult.Succeeded)
            {
                return BadRequest(new
                {
                    errors = removeResult.Errors
                        .Select(x => x.Description)
                });
            }
        }

        if (requestedRoles.Count > 0)
        {
            var addResult =
                await _userManager.AddToRolesAsync(
                    user,
                    requestedRoles);

            if (!addResult.Succeeded)
            {
                return BadRequest(new
                {
                    errors = addResult.Errors
                        .Select(x => x.Description)
                });
            }
        }

        var updatedRoles =
            await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            user.Id,
            user.Email,
            Roles = updatedRoles
        });
    }
}