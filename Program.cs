using UnoGame.Core;
using UnoGame.Models.Cards;
using UnoGame.Models.States;
using GameCore.Models;
using UnoGame.Backend.Services;
using UnoGame.Backend.WebSocket;
using System.Net.WebSockets;

// ═══════════════════════════════════════════
//  UNO OYUNU - WebSocket Sunucusu
// ═══════════════════════════════════════════

var builder = WebApplication.CreateBuilder(args);

// Kestrel'i 5000 portunda çalıştır
builder.WebHost.UseUrls("http://localhost:5000");

var app = builder.Build();

// WebSocket middleware'ini etkinleştir
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30) // Her 30 saniyede ping gönder
});

// Statik dosyalar için (test HTML sayfası)
app.UseDefaultFiles();
app.UseStaticFiles();

// ─────────────────────────────────────────
//  OYUN KURULUMU
// ─────────────────────────────────────────

Console.WriteLine("═══════════════════════════════════════════");
Console.WriteLine("     UNO OYUNU - WebSocket Sunucusu");
Console.WriteLine("═══════════════════════════════════════════\n");

// Oyunu oluştur
var game = new UnoGame.Core.UnoGame("game-123");

// Oyuncuları ekle (şimdilik sabit — ileride WebSocket ile dinamik olacak)
game.AddPlayer(new Player("p1", "Ahmet"));
game.AddPlayer(new Player("p2", "Mehmet"));
game.AddPlayer(new Player("p3", "Ayşe"));

Console.WriteLine("✅ 3 oyuncu eklendi\n");

// WebSocket bileşenleri
var connectionManager = new WebSocketConnectionManager();
WebSocketHandler? wsHandler = null;

// Oyunu başlat  
game.StartGame();

// WebSocket Event Listener — oyun eventlerini broadcast eder
var wsEventListener = new WebSocketEventListener(
    connectionManager,
    (UnoGameState)game.State,
    game.Players,
    game.CurrentTurnManager
);
game.AddEventListenerAsync(wsEventListener);

// Konsol çıktısı için JSON-RPC Backend Service (opsiyonel)
var backendService = new JsonRpcBackendService(
    "game-123",
    (UnoGameState)game.State,
    game.Players,
    game.CurrentTurnManager
);
game.AddEventListenerAsync(backendService);

// WebSocket Handler — client komutlarını işleyecek
wsHandler = new WebSocketHandler(game, connectionManager);

Console.WriteLine("✅ WebSocket Event Listener aktif!");
Console.WriteLine("✅ JSON-RPC Backend Service aktif!\n");

// ─────────────────────────────────────────
//  WEBSOCKET ENDPOINT
// ─────────────────────────────────────────

app.Map("/ws", async (HttpContext context) =>
{
    // WebSocket isteği mi kontrol et
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Bu endpoint sadece WebSocket bağlantıları kabul eder!");
        return;
    }

    // PlayerId'yi query string'den al: /ws?playerId=p1
    var playerId = context.Request.Query["playerId"].ToString();
    
    if (string.IsNullOrEmpty(playerId))
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("playerId parametresi gerekli! Örnek: /ws?playerId=p1");
        return;
    }

    Console.WriteLine($"\n WebSocket bağlantı isteği: {playerId}");

    // WebSocket bağlantısını kabul et
    var socket = await context.WebSockets.AcceptWebSocketAsync();

    // Handler'a bağlantıyı ver — bağlantı kapanana kadar bekler
    await wsHandler.HandleConnectionAsync(socket, playerId);
});

// ─────────────────────────────────────────
//  API ENDPOINTS (REST)
// ─────────────────────────────────────────

// Basit durum kontrolü
app.MapGet("/api/status", () => new
{
    status = "running",
    game_id = "game-123",
    connected_players = connectionManager.ConnectedPlayerIds,
    connection_count = connectionManager.ConnectionCount
});

// Oyun durumu
app.MapGet("/api/state", () =>
{
    return Results.Content(game.GetStateJson(), "application/json");
});

// ─────────────────────────────────────────
//  SUNUCUYU BAŞLAT
// ─────────────────────────────────────────

Console.WriteLine("═══════════════════════════════════════════");
Console.WriteLine("   Sunucu başlatiliyor...");
Console.WriteLine("   WebSocket: ws://localhost:5000/ws?playerId=p1");
Console.WriteLine("    Test UI:   http://localhost:5000");
Console.WriteLine("   API:       http://localhost:5000/api/status");
Console.WriteLine("═══════════════════════════════════════════\n");

app.Run();