using GameAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GameAPI.Services;

public class UserService
{
    private readonly IMongoCollection<User> _usersCollection;

    public UserService(IOptions<MongoDBSettings> mongoDBSettings)
    {
        var mongoClient = new MongoClient(mongoDBSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(mongoDBSettings.Value.DatabaseName);
        _usersCollection = mongoDatabase.GetCollection<User>("Users");
        
        // Email unique index
        var indexKeysDefinition = Builders<User>.IndexKeys.Ascending(user => user.Email);
        var indexOptions = new CreateIndexOptions { Unique = true };
        var indexModel = new CreateIndexModel<User>(indexKeysDefinition, indexOptions);
        _usersCollection.Indexes.CreateOneAsync(indexModel);
    }

    public async Task<List<User>> GetAsync() =>
        await _usersCollection.
            Find(_ => true).ToListAsync();

    public async Task<User?> GetAsync(string id) =>
        await _usersCollection.
            Find(user => user.Id == id).
            FirstOrDefaultAsync();

    public async Task<User?> GetByEmailAsync(string email) =>
        await _usersCollection.
            Find(user => user.Email == email).
            FirstOrDefaultAsync();

    public async Task<User> CreateAsync(User newUser)
    {
        await _usersCollection.InsertOneAsync(newUser);
        return newUser;
    }

    public async Task UpdateAsync(string id, User updatedUser) =>
        await _usersCollection.
            ReplaceOneAsync(u => u.Id == id, updatedUser);

    public async Task RemoveAsync(string id) =>
        await _usersCollection.DeleteOneAsync(u => u.Id == id);
}
