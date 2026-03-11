using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GameAPI.Models;

public class Gameplay
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("currentPositions")]
    public string CurrentPositions { get; set; } = string.Empty; // JSON string

    [BsonElement("hostPlayer")]
    public string HostPlayer { get; set; } = string.Empty;

    [BsonElement("guestPlayer")]
    public string? GuestPlayer { get; set; }

    [BsonElement("playerTurn")]
    public string PlayerTurn { get; set; } = string.Empty;

    [BsonElement("matchResult")]
    public string? MatchResult { get; set; }

    [BsonElement("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedDate")]
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}


