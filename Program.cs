using UnoGame.Core;
using UnoGame.Models.Cards;
using UnoGame.Models.States;
using GameCore.Models;
using UnoGame.Backend.Services;
using UnoGame.Backend.WebSocket;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5000");

var app = builder.Build();

// WebSocket middleware
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.UseDefaultFiles();
app.UseStaticFiles();

// ─── Oyun Kurulumu ───

Console.WriteLine("═══════════════════════════════════════════");
Console.WriteLine("     UNO OYUNU - WebSocket Sunucusu");
Console.WriteLine("═══════════════════════════════════════════\n");

var game = new UnoGame.Core.UnoGame("game-123");

game.AddPlayer(new Player("p1", "Ahmet"));
game.AddPlayer(new Player("p2", "Mehmet"));
game.AddPlayer(new Player("p3", "Ayşe"));

Console.WriteLine("✅ 3 oyuncu eklendi\n");

var connectionManager = new WebSocketConnectionManager();
WebSocketHandler? wsHandler = null;

game.StartGame();

// WebSocket Event Listener — oyun eventlerini broadcast eder
var wsEventListener = new WebSocketEventListener(
    connectionManager,
    (UnoGameState)game.State,
    game.Players,
    game.CurrentTurnManager
);
game.AddEventListenerAsync(wsEventListener);

// JSON-RPC Backend Service — event loglaması
var backendService = new JsonRpcBackendService(
    "game-123",
    (UnoGameState)game.State,
    game.Players,
    game.CurrentTurnManager
);
game.AddEventListenerAsync(backendService);

wsHandler = new WebSocketHandler(game, connectionManager);

Console.WriteLine("✅ WebSocket Event Listener aktif!");
Console.WriteLine("✅ JSON-RPC Backend Service aktif!\n");

// ─── WebSocket Endpoint ───

app.Map("/ws", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Bu endpoint sadece WebSocket bağlantıları kabul eder!");
        return;
    }

    var playerId = context.Request.Query["playerId"].ToString();
    
    if (string.IsNullOrEmpty(playerId))
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("playerId parametresi gerekli! Örnek: /ws?playerId=p1");
        return;
    }

    Console.WriteLine($"\n WebSocket bağlantı isteği: {playerId}");

    var socket = await context.WebSockets.AcceptWebSocketAsync();
    await wsHandler.HandleConnectionAsync(socket, playerId);
});

// ─── REST API Endpoints ───

app.MapGet("/api/status", () => new
{
    status = "running",
    game_id = "game-123",
    connected_players = connectionManager.ConnectedPlayerIds,
    connection_count = connectionManager.ConnectionCount
});

app.MapGet("/api/state", () =>
{
    return Results.Content(game.GetStateJson(), "application/json");
});

// ─── Sunucuyu Başlat ───

Console.WriteLine("═══════════════════════════════════════════");
Console.WriteLine("   Sunucu başlatiliyor...");
Console.WriteLine("   WebSocket: ws://localhost:5000/ws?playerId=p1");
Console.WriteLine("    Test UI:   http://localhost:5000");
Console.WriteLine("   API:       http://localhost:5000/api/status");
Console.WriteLine("═══════════════════════════════════════════\n");

app.Run();