
using RoboReglament.Server.Models;

namespace RoboReglament.Server.DTOs.Participants;

public sealed record ParticipantTeamItem(
    int TeamId,
    string TeamName,
    string? Organization
);

public sealed record ParticipantApplicationItem(
    int ApplicationId,
    int TournamentId,
    string TournamentName,
    TournamentStatus TournamentStatus,
    int DisciplineId,
    string DisciplineName,
    int TeamId,
    string TeamName,
    ApplicationStatus ApplicationStatus,
    bool IsAccredited
);

public sealed record ParticipantMatchItem(
    int MatchId,
    int TournamentId,
    int DisciplineId,
    int TeamId,
    string? Name,
    int Number,
    DateTime? ScheduledAt,
    MatchStatus Status
);

public sealed record ParticipantOverviewDto(
    List<ParticipantTeamItem> Teams,
    List<ParticipantApplicationItem> Applications,
    List<ParticipantMatchItem> Matches
);
