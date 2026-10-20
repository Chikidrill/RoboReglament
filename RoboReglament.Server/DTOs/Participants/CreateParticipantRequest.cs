using System.ComponentModel.DataAnnotations;
namespace RoboReglament.Server.DTOs.Participants
{
    public class CreateParticipantRequest
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        public DateOnly? BirthDate { get; set; }
    }
}
