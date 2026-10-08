using GameAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GameAPI.Services;

public class GameplayService
{
    private readonly IMongoCollection<Gameplay> _matchesCollection;

    public GameplayService(IOptions<MongoDBSettings> mongoDBSettings)
    {
        var mongoClient = new MongoClient(mongoDBSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(mongoDBSettings.Value.DatabaseName);
        _matchesCollection = mongoDatabase.GetCollection<Gameplay>("matches");
    }

    public async Task<List<Gameplay>> GetAsync() =>
        await _matchesCollection.
            Find(_ => true).
            ToListAsync();

    public async Task<Gameplay?> GetAsync(string id) =>
        await _matchesCollection.
            Find(gameplay => gameplay.Id == id).
            FirstOrDefaultAsync();

    public async Task<List<Gameplay>> GetByPlayerAsync(string playerId) =>
        await _matchesCollection.Find(gameplay =>
            gameplay.HostPlayer == playerId || gameplay.GuestPlayer == playerId).
            ToListAsync();

    public async Task<Gameplay> CreateAsync(Gameplay newGameplay)
    {
        await _matchesCollection.
            InsertOneAsync(newGameplay);
        return newGameplay;
    }

    public async Task UpdateAsync(string id, Gameplay updatedGameplay)
    {
        updatedGameplay.UpdatedDate = DateTime.UtcNow;
        await _matchesCollection.
            ReplaceOneAsync(currentGameplay => currentGameplay.Id == id, updatedGameplay);
    }

    public async Task RemoveAsync(string id) =>
        await _matchesCollection.
            DeleteOneAsync(gameplay => gameplay.Id == id);
    
    public async Task<List<Gameplay>> GetByStatusAsync(string status) =>
        await _matchesCollection.Find(g => g.Status == status).ToListAsync();
}