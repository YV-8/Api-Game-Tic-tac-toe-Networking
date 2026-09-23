namespace GameAPI.Models
{
    public class GameplayResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string CurrentPositions { get; set; } = string.Empty;
        public string HostPlayer { get; set; } = string.Empty;
        public string? GuestPlayer { get; set; }
        public string PlayerTurn { get; set; } = string.Empty;
        public string? MatchResult { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
