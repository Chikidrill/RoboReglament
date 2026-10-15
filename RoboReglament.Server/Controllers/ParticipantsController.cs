using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboReglament.Server.Data;
using RoboReglament.Server.DTOs.Participants;
using RoboReglament.Server.Models;
using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Controllers
{
    [ApiController]
    [Route("api/teams/{teamId:int}/participants")]
    [Authorize(Roles = UserRoles.Coach)]
    public class ParticipantsController: ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ParticipantsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll(int teamId)
        {
            var accessError = await CheckTeamAccessAsync(teamId);

            if (accessError is not null)
                return accessError;

            var participants = await _context.Participants
                .AsNoTracking()
                .Where(x => x.TeamId == teamId)
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Select(x => new ParticipantResponse
                {
                    Id = x.Id,
                    TeamId = x.TeamId,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    MiddleName = x.MiddleName,
                    BirthDate = x.BirthDate
                })
                .ToListAsync();

            return Ok(participants);
        }

        // GET /api/teams/{teamId}/participants/{participantId}
        [HttpGet("{participantId:int}")]
        public async Task<IActionResult> GetById(
            int teamId,
            int participantId)
        {
            var accessError = await CheckTeamAccessAsync(teamId);

            if (accessError is not null)
                return accessError;

            var participant = await _context.Participants
                .AsNoTracking()
                .Where(x =>
                    x.Id == participantId &&
                    x.TeamId == teamId)
                .Select(x => new ParticipantResponse
                {
                    Id = x.Id,
                    TeamId = x.TeamId,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    MiddleName = x.MiddleName,
                    BirthDate = x.BirthDate
                })
                .FirstOrDefaultAsync();

            if (participant is null)
            {
                return NotFound(new
                {
                    message = "Участник не найден."
                });
            }

            return Ok(participant);
        }

        // POST /api/teams/{teamId}/participants
        [HttpPost]
        public async Task<IActionResult> Create(
            int teamId,
            CreateParticipantRequest request)
        {
            var accessError = await CheckTeamAccessAsync(teamId);

            if (accessError is not null)
                return accessError;

            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest(new
                {
                    message = "Имя и фамилия обязательны."
                });
            }

            var participant = new Participant
            {
                TeamId = teamId,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                MiddleName = request.MiddleName?.Trim(),
                BirthDate = request.BirthDate
            };

            _context.Participants.Add(participant);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    teamId,
                    participantId = participant.Id
                },
                ToResponse(participant));
        }

        // PUT /api/teams/{teamId}/participants/{participantId}
        [HttpPut("{participantId:int}")]
        public async Task<IActionResult> Update(
            int teamId,
            int participantId,
            UpdateParticipantRequest request)
        {
            var accessError = await CheckTeamAccessAsync(teamId);

            if (accessError is not null)
                return accessError;

            var participant = await _context.Participants
                .FirstOrDefaultAsync(x =>
                    x.Id == participantId &&
                    x.TeamId == teamId);

            if (participant is null)
            {
                return NotFound(new
                {
                    message = "Участник не найден."
                });
            }

            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest(new
                {
                    message = "Имя и фамилия обязательны."
                });
            }

            participant.FirstName = request.FirstName.Trim();
            participant.LastName = request.LastName.Trim();
            participant.MiddleName = request.MiddleName?.Trim();
            participant.BirthDate = request.BirthDate;

            await _context.SaveChangesAsync();

            return Ok(ToResponse(participant));
        }

        // DELETE /api/teams/{teamId}/participants/{participantId}
        [HttpDelete("{participantId:int}")]
        public async Task<IActionResult> Delete(
            int teamId,
            int participantId)
        {
            var accessError = await CheckTeamAccessAsync(teamId);

            if (accessError is not null)
                return accessError;

            var participant = await _context.Participants
                .FirstOrDefaultAsync(x =>
                    x.Id == participantId &&
                    x.TeamId == teamId);

            if (participant is null)
            {
                return NotFound(new
                {
                    message = "Участник не найден."
                });
            }

            var hasApplications = await _context.TournamentApplications
                .AnyAsync(x => x.TeamId == teamId);

            if (hasApplications)
            {
                return Conflict(new
                {
                    message = "Нельзя удалить участника команды, " +
                              "для которой уже существуют заявки на турниры."
                });
            }

            _context.Participants.Remove(participant);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Проверка прав доступа к команде.
        private async Task<IActionResult?> CheckTeamAccessAsync(int teamId)
        {
            var teamExists = await _context.Teams
                .AnyAsync(x => x.Id == teamId);

            if (!teamExists)
            {
                return NotFound(new
                {
                    message = "Команда не найдена."
                });
            }

            var userId = _userManager.GetUserId(User);

            if (userId is null)
                return Unauthorized();

            var canManage = await _context.TeamCoaches
                .AnyAsync(x =>
                    x.TeamId == teamId &&
                    x.UserId == userId);

            if (!canManage)
                return Forbid();

            return null;
        }

        // Преобразование сущности БД в DTO ответа.
        private static ParticipantResponse ToResponse(Participant participant)
        {
            return new ParticipantResponse
            {
                Id = participant.Id,
                TeamId = participant.TeamId,
                FirstName = participant.FirstName,
                LastName = participant.LastName,
                MiddleName = participant.MiddleName,
                BirthDate = participant.BirthDate
            };
        }
    }
}
