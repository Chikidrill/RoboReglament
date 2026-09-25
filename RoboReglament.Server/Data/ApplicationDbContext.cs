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
    public DbSet<MatchGroup> MatchGroups => Set<MatchGroup>();
    public DbSet<MatchTeam> MatchTeams => Set<MatchTeam>();

    public DbSet<ProtocolTemplate> ProtocolTemplates => Set<ProtocolTemplate>();
    public DbSet<ProtocolCriterion> ProtocolCriteria => Set<ProtocolCriterion>();

    public DbSet<JudgeProtocol> JudgeProtocols => Set<JudgeProtocol>();
    public DbSet<JudgeScore> JudgeScores => Set<JudgeScore>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Одна команда не может дважды подать заявку
        // на один и тот же турнир.
        builder.Entity<TournamentApplication>()
            .HasIndex(x => new { x.TournamentId, x.TeamId })
            .IsUnique();

        // Одна заявка — одна аккредитация.
        builder.Entity<Accreditation>()
            .HasOne(x => x.Application)
            .WithOne(x => x.Accreditation)
            .HasForeignKey<Accreditation>(x => x.ApplicationId);

        // Номер матча уникален внутри турнира.
        builder.Entity<Match>()
            .HasIndex(x => new { x.TournamentId, x.Number })
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
    }
}