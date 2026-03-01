using GameCore.Interfaces;
using UnoGame.Events;
using System;
using System.Text.Json;

namespace UnoGame.Listeners
{
    public class ConsoleEventListener : IGameEventListener
    {
        public void OnGameEvent(IGameAction action)
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════╗");
            Console.WriteLine("║           📡 EVENT YAKALANDI                  ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝");
            
            switch (action)
            {
                case UnoGameStartedEvent startEvent:
                    Console.WriteLine($"🎮 Oyun Başladı - GameId: {startEvent.GameId}");
                    break;
                
                case CardPlayedEvent cardPlayedEvent:
                    Console.WriteLine($"🃏 Kart Atıldı - {cardPlayedEvent.PlayerId}: {cardPlayedEvent.Card}");
                    break;
                
                case TurnChangedEvent turnChangedEvent:
                    Console.WriteLine($"🔄 Tur Değişti - Sıra: {turnChangedEvent.NewCurrentPlayerId}");
                    break;
            }
            
            var json = JsonSerializer.Serialize(action, action.GetType(), new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine("\n🔹 JSON:");
            Console.WriteLine(json);
            Console.WriteLine("════════════════════════════════════════════════\n");
        }
    }
}