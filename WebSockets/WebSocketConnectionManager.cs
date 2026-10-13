using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace GameAPI.WebSockets
{
    public class WebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
        private readonly ConcurrentDictionary<string, string> _userSockets = new();

        public WebSocket? GetSocketById(string id) =>
            _sockets.TryGetValue(id, out var ws) ? ws : null;

        public ConcurrentDictionary<string, WebSocket> GetAll() => _sockets;

        public string? GetId(WebSocket socket) =>
            _sockets.FirstOrDefault(p => p.Value == socket).Key;

        public string AddSocket(WebSocket socket, string userId)
        {
            if (_userSockets.TryGetValue(userId, out var oldSocketId) &&
                _sockets.TryRemove(oldSocketId, out var oldSocket))
            {
                if (oldSocket.State == WebSocketState.Open)
                {
                    _ = oldSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Replaced by new connection",
                        CancellationToken.None);
                }
            }

            var connectionId = Guid.NewGuid().ToString();
            _sockets.TryAdd(connectionId, socket);
            _userSockets[userId] = connectionId;
            return connectionId;
        }

        public string? GetSocketIdByUserId(string userId) =>
            _userSockets.TryGetValue(userId, out var socketId) ? socketId : null;

        public string? GetUserIdBySocketId(string socketId) =>
            _userSockets.FirstOrDefault(p => p.Value == socketId).Key;

        public WebSocket? GetSocketByUserId(string userId)
        {
            var socketId = GetSocketIdByUserId(userId);
            return socketId != null ? GetSocketById(socketId) : null;
        }

        public async Task RemoveSocket(string socketId)
        {
            _sockets.TryRemove(socketId, out var socket);
            var userId = GetUserIdBySocketId(socketId);
            if (userId != null) _userSockets.TryRemove(userId, out _);

            if (socket != null &&
                socket.State != WebSocketState.Closed &&
                socket.State != WebSocketState.Aborted)
            {
                try
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closed by WebSocketManager",
                        CancellationToken.None);
                }
                catch { /* ignore */ }
            }
        }

        public List<string> GetOnlineUserIds() => _userSockets.Keys.ToList();

        public bool IsUserOnline(string userId) => _userSockets.ContainsKey(userId);
    }
}