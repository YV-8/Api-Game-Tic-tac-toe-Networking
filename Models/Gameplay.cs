using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GameAPI.Models;

public class Gameplay
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonElement("board_state")]
    public string CurrentPositions { get; set; } = string.Empty;
    
    [BsonElement("player1")]
    public string HostPlayer { get; set; } = string.Empty;
    
    [BsonElement("player2")]
    public string? GuestPlayer { get; set; }

    [BsonElement("playerTurn")]
    public string PlayerTurn { get; set; } = string.Empty;

    [BsonElement("orderPlay")]
    public int OrderPlay { get; set; } = 1;

    [BsonElement("matchResult")]
    public string? MatchResult { get; set; }
    
    [BsonElement("status")]
    public string Status { get; set; } = "in_progress";

    [BsonElement("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedDate")]
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}