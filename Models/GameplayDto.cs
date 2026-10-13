namespace GameAPI.Models
{
    public class GameplayDto
    {
        public string CurrentPositions { get; set; } = string.Empty;
        public string HostPlayer { get; set; } = string.Empty;
        public string? GuestPlayer { get; set; }
        public string PlayerTurn { get; set; } = string.Empty;
        public int OrderPlay { get; set; } = 1;
        public string? MatchResult { get; set; }
        public string? Status { get; set; }
    }
}
