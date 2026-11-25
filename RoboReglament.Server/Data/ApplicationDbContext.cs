using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RoboReglament.Server.Models;
using RoboReglament.Server.Models.Identity;

namespace RoboReglament.Server.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<TournamentApplication> TournamentApplications => Set<TournamentApplication>();
    public DbSet<Accreditation> Accreditations => Set<Accreditation>();

    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchJudge> MatchJudges => Set<MatchJudge>();
    public DbSet<MatchGroup> MatchGroups => Set<MatchGroup>();
    public DbSet<MatchTeam> MatchTeams => Set<MatchTeam>();

    public DbSet<ProtocolTemplate> ProtocolTemplates => Set<ProtocolTemplate>();
    public DbSet<ProtocolCriterion> ProtocolCriteria => Set<ProtocolCriterion>();

    public DbSet<JudgeProtocol> JudgeProtocols => Set<JudgeProtocol>();
    public DbSet<JudgeScore> JudgeScores => Set<JudgeScore>();

    public DbSet<TournamentOrganizer> TournamentOrganizers
        => Set<TournamentOrganizer>();

    public DbSet<TeamCoach> TeamCoaches => Set<TeamCoach>();
    public DbSet<TournamentApplicationParticipant> TournamentApplicationParticipants
    => Set<TournamentApplicationParticipant>();

    public DbSet<Discipline> Disciplines => Set<Discipline>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);


        builder.HasSequence<long>("UserPublicIdSequence", "dbo")
            .StartsAt(10_000_000L)
            .IncrementsBy(1);

        builder.Entity<ApplicationUser>()
            .Property(x => x.PublicId)
            .HasDefaultValueSql(
                "NEXT VALUE FOR [dbo].[UserPublicIdSequence]");


        builder.Entity<ApplicationUser>()
            .HasIndex(x => x.PublicId)
            .IsUnique();

        builder.Entity<TournamentApplication>()
            .HasIndex(x => new
            {
                x.TournamentId,
                x.DisciplineId,
                x.TeamId
            })
            .IsUnique();

        builder.Entity<TournamentApplication>()
            .HasOne(x => x.Team)
            .WithMany(x => x.Applications)
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Accreditation>()
            .HasOne(x => x.Application)
            .WithOne(x => x.Accreditation)
            .HasForeignKey<Accreditation>(x => x.ApplicationId);

        // Номер матча уникален внутри турнира.
        builder.Entity<Match>()
            .HasIndex(x => new
            {
                x.TournamentId,
                x.DisciplineId,
                x.Number
            })
            .IsUnique();

        // Протокол матча удалять автоматически не нужно.
        builder.Entity<Match>()
            .HasOne(x => x.ProtocolTemplate)
            .WithMany(x => x.Matches)
            .HasForeignKey(x => x.ProtocolTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        // Номер группы уникален внутри матча.
        builder.Entity<MatchGroup>()
            .HasIndex(x => new { x.MatchId, x.Number })
            .IsUnique();

        // Одна команда не должна дважды находиться
        // в одной группе матча.
        builder.Entity<MatchTeam>()
            .HasIndex(x => new { x.MatchGroupId, x.TeamId })
            .IsUnique();

        builder.Entity<MatchTeam>()
            .HasOne(x => x.Team)
            .WithMany(x => x.MatchTeams)
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        // Порядок критериев внутри одного протокола уникален.
        builder.Entity<ProtocolCriterion>()
            .HasIndex(x => new
            {
                x.ProtocolTemplateId,
                x.Order
            })
            .IsUnique();

        // Один судейский протокол не должен иметь
        // две оценки по одному критерию.
        builder.Entity<JudgeScore>()
            .HasIndex(x => new
            {
                x.JudgeProtocolId,
                x.ProtocolCriterionId
            })
            .IsUnique();

        // Судья — пользователь Identity.
        builder.Entity<JudgeProtocol>()
            .HasOne(x => x.Judge)
            .WithMany()
            .HasForeignKey(x => x.JudgeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Не удаляем критерий, если по нему уже есть оценки.
        builder.Entity<JudgeScore>()
            .HasOne(x => x.ProtocolCriterion)
            .WithMany(x => x.JudgeScores)
            .HasForeignKey(x => x.ProtocolCriterionId)
            .OnDelete(DeleteBehavior.Restrict);

        // SQL Server: явно задаём точность decimal.
        builder.Entity<ProtocolCriterion>()
            .Property(x => x.MinValue)
            .HasPrecision(18, 2);

        builder.Entity<ProtocolCriterion>()
            .Property(x => x.MaxValue)
            .HasPrecision(18, 2);

        builder.Entity<ProtocolCriterion>()
            .Property(x => x.Weight)
            .HasPrecision(18, 2);

        builder.Entity<JudgeScore>()
            .Property(x => x.Value)
            .HasPrecision(18, 2);

        builder.Entity<TournamentOrganizer>()
            .HasIndex(x => new { x.TournamentId, x.UserId })
            .IsUnique();

        builder.Entity<TournamentOrganizer>()
            .HasOne(x => x.Tournament)
            .WithMany(x => x.Organizers)
            .HasForeignKey(x => x.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TournamentOrganizer>()
            .HasOne(x => x.User)
            .WithMany(x => x.OrganizedTournaments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TeamCoach>()
            .HasIndex(x => new { x.TeamId, x.UserId })
            .IsUnique();

        builder.Entity<TeamCoach>()
            .HasOne(x => x.Team)
            .WithMany(x => x.Coaches)
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TeamCoach>()
            .HasOne(x => x.User)
            .WithMany(x => x.CoachedTeams)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Participant>()
            .HasOne(x => x.User)
            .WithMany(x => x.TeamMemberships)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);


        builder.Entity<Participant>()
            .HasIndex(x => new { x.TeamId, x.UserId })
            .IsUnique();


        builder.Entity<TournamentApplicationParticipant>()
            .HasIndex(x => new { x.TournamentApplicationId, x.ParticipantId })
            .IsUnique()
            .HasFilter("[ParticipantId] IS NOT NULL");

        builder.Entity<TournamentApplicationParticipant>()
            .Property(x => x.FirstName)
            .HasMaxLength(100);

        builder.Entity<TournamentApplicationParticipant>()
            .Property(x => x.LastName)
            .HasMaxLength(100);

        builder.Entity<TournamentApplicationParticipant>()
            .Property(x => x.MiddleName)
            .HasMaxLength(100);


        builder.Entity<Discipline>()
            .HasOne(x => x.Tournament)
            .WithMany(x => x.Disciplines)
            .HasForeignKey(x => x.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Discipline>()
            .HasIndex(x => new
            {
                x.TournamentId,
                x.Name
            })
            .IsUnique();

        builder.Entity<Discipline>()
            .HasAlternateKey(x => new
            {
                x.TournamentId,
                x.Id
            });



        builder.Entity<Discipline>()
            .Property(x => x.SidesPerEvent)
            .HasDefaultValue(1);

        builder.Entity<Discipline>()
            .Property(x => x.MinTeamsPerSide)
            .HasDefaultValue(1);

        builder.Entity<Discipline>()
            .Property(x => x.MaxTeamsPerSide)
            .HasDefaultValue(1);

        builder.Entity<Discipline>()
            .ToTable(table => table.HasCheckConstraint(
                "CK_Disciplines_CompetitionRules",
                @"(
            (
                ([Format] = 0 AND [SidesPerEvent] = 1)
                OR
                ([Format] = 1 AND [SidesPerEvent] BETWEEN 2 AND 4)
            )
            AND
            (
                (
                    [ScoringMode] = 0
                    AND [MinTeamsPerSide] = 1
                    AND [MaxTeamsPerSide] = 1
                )
                OR
                (
                    [ScoringMode] = 1
                    AND [MinTeamsPerSide] BETWEEN 2 AND 4
                    AND [MaxTeamsPerSide]
                        BETWEEN [MinTeamsPerSide] AND 4
                )
            )
        )"));


        builder.Entity<Discipline>()
            .Property(x => x.JudgeAggregationMethod)
            .HasDefaultValue(JudgeAggregationMethod.Average);

        builder.Entity<Discipline>()
            .ToTable(table => table.HasCheckConstraint(
                "CK_Disciplines_JudgeAggregationMethod",
                "[JudgeAggregationMethod] IN (0, 1, 2)"));


        builder.Entity<Discipline>()
            .Property(x => x.RankingMethod)
            .HasDefaultValue(RankingMethod.BestResult);

        builder.Entity<Discipline>()
            .ToTable(table => table.HasCheckConstraint(
                "CK_Disciplines_RankingMethod",
                @"(
            ([Format] = 0 AND [RankingMethod] IN (0, 1, 2))
            OR
            ([Format] = 1 AND [RankingMethod] = 3)
        )"));


        builder.Entity<Discipline>()
            .Property(x => x.WinPoints)
            .HasDefaultValue(3);

        builder.Entity<Discipline>()
            .Property(x => x.DrawPoints)
            .HasDefaultValue(1);

        builder.Entity<Discipline>()
            .Property(x => x.LossPoints)
            .HasDefaultValue(0);

        builder.Entity<Discipline>()
            .ToTable(table => table.HasCheckConstraint(
                "CK_Disciplines_MatchPoints",
                @"[WinPoints] BETWEEN 0 AND 100
          AND [DrawPoints] BETWEEN 0 AND 100
          AND [LossPoints] BETWEEN 0 AND 100
          AND [WinPoints] > [DrawPoints]
          AND [DrawPoints] >= [LossPoints]"));

        builder.Entity<TournamentApplication>()
            .HasOne(x => x.Discipline)
            .WithMany()
            .HasForeignKey(x => new
            {
                x.TournamentId,
                x.DisciplineId
            })
            .HasPrincipalKey(x => new
            {
                x.TournamentId,
                x.Id
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Match>()
            .HasOne(x => x.Discipline)
            .WithMany()
            .HasForeignKey(x => new
            {
                x.TournamentId,
                x.DisciplineId
            })
            .HasPrincipalKey(x => new
            {
                x.TournamentId,
                x.Id
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProtocolTemplate>()
            .HasOne(x => x.Discipline)
            .WithMany()
            .HasForeignKey(x => new
            {
                x.TournamentId,
                x.DisciplineId
            })
            .HasPrincipalKey(x => new
            {
                x.TournamentId,
                x.Id
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MatchJudge>()
            .HasIndex(x => new { x.MatchId, x.JudgeId })
            .IsUnique();

        builder.Entity<MatchJudge>()
            .HasOne(x => x.Match)
            .WithMany(x => x.Judges)
            .HasForeignKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MatchJudge>()
            .HasOne(x => x.Judge)
            .WithMany()
            .HasForeignKey(x => x.JudgeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<JudgeProtocol>()
            .HasIndex(x => new { x.MatchGroupId, x.JudgeId })
            .IsUnique()
            .HasFilter("[JudgeId] IS NOT NULL");
    }
}