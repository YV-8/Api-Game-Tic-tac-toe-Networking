using GameAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GameAPI.Services;

public class GameplayService
{
    private readonly IMongoCollection<Gameplay> _gameplaysCollection;

    public GameplayService(IOptions<MongoDBSettings> mongoDBSettings)
    {
        var mongoClient = new MongoClient(mongoDBSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(mongoDBSettings.Value.DatabaseName);
        _gameplaysCollection = mongoDatabase.GetCollection<Gameplay>("Gameplays");
    }

    public async Task<List<Gameplay>> GetAsync() =>
        await _gameplaysCollection.
            Find(_ => true).
            ToListAsync();

    public async Task<Gameplay?> GetAsync(string id) =>
        await _gameplaysCollection.
            Find(gameplay => gameplay.Id == id).
            FirstOrDefaultAsync();

    public async Task<List<Gameplay>> GetByPlayerAsync(string playerId) =>
        await _gameplaysCollection.Find(gameplay =>
            gameplay.HostPlayer == playerId || gameplay.GuestPlayer == playerId).
            ToListAsync();

    public async Task<Gameplay> CreateAsync(Gameplay newGameplay)
    {
        await _gameplaysCollection.
            InsertOneAsync(newGameplay);
        return newGameplay;
    }

    public async Task UpdateAsync(string id, Gameplay updatedGameplay)
    {
        updatedGameplay.UpdatedDate = DateTime.UtcNow;
        await _gameplaysCollection.
            ReplaceOneAsync(currentGameplay => currentGameplay.Id == id, updatedGameplay);
    }

    public async Task RemoveAsync(string id) =>
        await _gameplaysCollection.
            DeleteOneAsync(gameplay => gameplay.Id == id);
}
