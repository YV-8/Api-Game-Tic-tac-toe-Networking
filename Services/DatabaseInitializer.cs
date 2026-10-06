using GameAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GameAPI.Services;

public class DatabaseInitializer : IHostedService
{
    private readonly IMongoDatabase _db;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IOptions<MongoDBSettings> settings,
        ILogger<DatabaseInitializer> logger)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _db = client.GetDatabase(settings.Value.DatabaseName);
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("Inicializando esquema MongoDB...");

        await EnsureCollectionAsync("users", new BsonDocument("$jsonSchema", new BsonDocument
        {
            { "bsonType", "object" },
            { "required", new BsonArray { "username", "email", "password_hash", "createdDate" } },
            { "properties", new BsonDocument
                {
                    { "_id",           new BsonDocument("bsonType", "objectId") },
                    { "username",      new BsonDocument { { "bsonType", "string" }, { "minLength", 3 }, { "maxLength", 50 } } },
                    { "email",         new BsonDocument("bsonType", "string") },
                    { "password_hash", new BsonDocument { { "bsonType", "string" }, { "minLength", 20 } } },
                    { "createdDate",   new BsonDocument("bsonType", "date") }
                }
            }
        }), ct);

        await EnsureCollectionAsync("matches", new BsonDocument("$jsonSchema", new BsonDocument
        {
            { "bsonType", "object" },
            { "required", new BsonArray { "player1", "player2", "board_state", "status", "createdDate", "updatedDate" } },
            { "properties", new BsonDocument
                {
                    { "_id",         new BsonDocument("bsonType", "objectId") },
                    { "player1",     new BsonDocument("bsonType", new BsonArray { "objectId", "string" }) },
                    { "player2",     new BsonDocument("bsonType", new BsonArray { "objectId", "string", "null" }) },
                    { "board_state", new BsonDocument("bsonType", "string") },
                    { "status",      new BsonDocument("enum", new BsonArray { "in_progress", "finished", "abandoned" }) },
                    { "createdDate", new BsonDocument("bsonType", "date") },
                    { "updatedDate", new BsonDocument("bsonType", "date") }
                }
            }
        }), ct);

        var users = _db.GetCollection<BsonDocument>("users");
        await users.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("email"),
            new CreateIndexOptions { Unique = true, Name = "ux_users_email" }), cancellationToken: ct);
        await users.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("username"),
            new CreateIndexOptions { Unique = true, Name = "ux_users_username" }), cancellationToken: ct);

        var matches = _db.GetCollection<BsonDocument>("matches");
        await matches.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("player1").Ascending("player2").Ascending("status"),
            new CreateIndexOptions { Name = "ix_matches_players_status" }), cancellationToken: ct);

        _logger.LogInformation("Esquema MongoDB listo");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private async Task EnsureCollectionAsync(string name, BsonDocument validator, CancellationToken ct)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("name", name);
        using var cursor = await _db.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter }, ct);
        var exists = await cursor.AnyAsync(ct);

        var command = new BsonDocument
        {
            { exists ? "collMod" : "create", name },
            { "validator",        validator },
            { "validationLevel",  "moderate" },
            { "validationAction", "error" }
        };
        await _db.RunCommandAsync<BsonDocument>(command, cancellationToken: ct);
        _logger.LogInformation(exists
            ? "Colección '{Name}' ya existía (validator aplicado)"
            : "Colección '{Name}' creada", name);
    }
}