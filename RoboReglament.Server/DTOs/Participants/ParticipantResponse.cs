namespace RoboReglament.Server.DTOs.Participants
{
    public class ParticipantResponse
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName {  get; set; }
        public DateOnly? BirthDate { get; set; }
    }
}
