using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnoGame.Backend.WebSocket
{
    /// <summary>
    /// WebSocket bağlantılarını yöneten sınıf.
    /// PlayerId bazlı bağlantı takibi, unicast ve broadcast mesajlaşmayı sağlar.
    /// Thread-safe: ConcurrentDictionary kullanır.
    /// </summary>
    public class WebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<string, System.Net.WebSockets.WebSocket> _connections = new();

        public int ConnectionCount => _connections.Count;
        public IReadOnlyList<string> ConnectedPlayerIds => _connections.Keys.ToList();

        /// <summary>
        /// Yeni bağlantıyı kaydeder. Aynı oyuncu tekrar bağlanırsa eski bağlantı kapatılır.
        /// </summary>
        public void AddConnection(string playerId, System.Net.WebSockets.WebSocket socket)
        {
            if (_connections.TryRemove(playerId, out var oldSocket))
            {
                if (oldSocket.State == WebSocketState.Open)
                {
                    _ = oldSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Yeni bağlantı açıldı",
                        CancellationToken.None);
                }
            }

            _connections.TryAdd(playerId, socket);
            Console.WriteLine($"🔌 WebSocket: {playerId} bağlandı. Toplam: {ConnectionCount}");
        }

        public void RemoveConnection(string playerId)
        {
            _connections.TryRemove(playerId, out _);
            Console.WriteLine($"🔌 WebSocket: {playerId} ayrıldı. Toplam: {ConnectionCount}");
        }

        /// <summary>
        /// Belirli bir oyuncuya mesaj gönderir.
        /// </summary>
        public async Task SendToPlayerAsync(string playerId, string message)
        {
            if (_connections.TryGetValue(playerId, out var socket) && 
                socket.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    CancellationToken.None);
            }
        }

        /// <summary>
        /// Tüm bağlı oyunculara mesaj gönderir.
        /// </summary>
        public async Task BroadcastAsync(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(bytes);

            var tasks = new List<Task>();
            foreach (var kvp in _connections)
            {
                if (kvp.Value.State == WebSocketState.Open)
                {
                    tasks.Add(kvp.Value.SendAsync(
                        segment,
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        CancellationToken.None));
                }
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Belirtilen oyuncu dışındaki herkese mesaj gönderir.
        /// </summary>
        public async Task BroadcastExceptAsync(string excludePlayerId, string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(bytes);

            var tasks = new List<Task>();
            foreach (var kvp in _connections)
            {
                if (kvp.Key != excludePlayerId && kvp.Value.State == WebSocketState.Open)
                {
                    tasks.Add(kvp.Value.SendAsync(
                        segment,
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        CancellationToken.None));
                }
            }

            await Task.WhenAll(tasks);
        }

        public bool IsConnected(string playerId)
        {
            return _connections.TryGetValue(playerId, out var socket) && 
                   socket.State == WebSocketState.Open;
        }

        /// <summary>
        /// Kopuk bağlantıları tespit edip temizler.
        /// </summary>
        public void CleanupDisconnected()
        {
            var disconnected = _connections
                .Where(kvp => kvp.Value.State != WebSocketState.Open)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var playerId in disconnected)
            {
                RemoveConnection(playerId);
            }
        }
    }
}
