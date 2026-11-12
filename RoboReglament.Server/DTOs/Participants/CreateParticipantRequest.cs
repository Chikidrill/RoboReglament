
using System.ComponentModel.DataAnnotations;

namespace RoboReglament.Server.DTOs.Participants;

public class CreateParticipantRequest
{
    [Range(1, long.MaxValue)]
    public long PublicId { get; set; }
}
