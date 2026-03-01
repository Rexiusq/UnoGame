using GameCore.Interfaces;  // ⭐ GAMECORE KULLANIMI 2: IGameRules interface'i
using GameCore.Enums;       // ⭐ GAMECORE KULLANIMI 3: PlayerStatus enum'ı
using UnoGame.Models.Cards;
using UnoGame.Models.States;
using System.Collections.Generic;
using System.Linq;

namespace UnoGame.Rules
{
    /// <summary>
    /// UNO oyununun kurallarını tanımlayan sınıf
    /// 
    /// ⭐ GAMECORE KULLANIMI: IGameRules interface'ini implement ediyor!
    /// 
    /// IGameRules nedir?
    /// - GameCore'un tanımladığı bir "sözleşme" (contract)
    /// - Her oyun bu interface'i implement etmek zorunda
    /// - Böylece GameCore, hangi oyun olursa olsun aynı metodları çağırabilir
    /// 
    /// SOLID Prensibi: Interface Segregation & Dependency Inversion
    /// - Concrete class yerine interface'e bağımlıyız
    /// - İleride farklı kural setleri ekleyebiliriz (kolay vs zor mod)
    /// </summary>
    public class UnoGameRules : IGameRules  // ⭐ GameCore interface'i implement etme
    {
        /// <summary>
        /// Minimum kaç oyuncu gerekli?
        /// UNO için 2-10 oyuncu
        /// </summary>
        public int MinPlayers => 2;
        public int MaxPlayers => 10;

        /// <summary>
        /// Oyun başlayabilir mi kontrolü
        /// 
        /// ⭐ GAMECORE KULLANIMI: PlayerStatus enum'ı kullanılıyor
        /// 
        /// GameCore neden PlayerStatus sağlıyor?
        /// - Her oyunda oyuncu durumları benzer (Active, Waiting, Eliminated)
        /// - Tekrar yazmak yerine GameCore'dan kullanıyoruz (DRY)
        /// </summary>
        public bool CanStartGame(IReadOnlyList<IPlayer> players)  // ⭐ IPlayer interface'i
        {
            // Aktif veya bekleyen oyuncuları say
            var activeCount = players.Count(p => 
                p.Status == PlayerStatus.Active ||   // ⭐ GameCore enum'ı
                p.Status == PlayerStatus.Waiting);   // ⭐ GameCore enum'ı
            
            // Min-Max aralığında olmalı
            return activeCount >= MinPlayers && activeCount <= MaxPlayers;
        }

        /// <summary>
        /// Bir aksiyonun geçerli olup olmadığını kontrol eder
        /// Şimdilik basit tutuyoruz, ileride genişletilebilir
        /// </summary>
        public bool ValidateAction(IGameAction action, IGameState state)  // ⭐ GameCore interface'leri
        {
            // Gelecekte buraya özel validasyonlar eklenebilir
            return true;
        }

        /// <summary>
        /// Oyun bitti mi kontrolü
        /// Birinin eli boşsa (0 kart) oyun bitmiş demektir
        /// </summary>
        public bool IsGameOver(IGameState state)  // ⭐ GameCore IGameState
        {
            if (state is UnoGameState unoState)
            {
                // Herhangi birinin kart sayısı 0 mı?
                return unoState.PlayerCardCounts.Any(p => p.Value == 0);
            }
            return false;
        }

        /// <summary>
        /// Kazananı belirle (IGameRules interface'i için)
        /// Players listesi olmadan sadece PlayerId döndürebilir
        /// </summary>
        public IPlayer? GetWinner(IGameState state)
        {
            // Interface implementasyonu — Players olmadan tam çalışamaz
            // FindWinner overload'ını kullan
            return null;
        }

        /// <summary>
        /// Kazananı belirle — Players listesi ile birlikte
        /// İlk eli biten oyuncu kazanır
        /// </summary>
        public IPlayer? FindWinner(IGameState state, IReadOnlyList<IPlayer> players)
        {
            if (state is UnoGameState unoState)
            {
                var winnerEntry = unoState.PlayerCardCounts
                    .FirstOrDefault(p => p.Value == 0);

                if (winnerEntry.Key != null)
                {
                    return players.FirstOrDefault(p => p.Id == winnerEntry.Key);
                }
            }
            return null;
        }

        /// <summary>
        /// UNO'ya özel: Bir kartın atılıp atılamayacağını kontrol eder
        /// 
        /// Kurallar:
        /// 1. Wild (Joker) kartlar her zaman atılabilir
        /// 2. Eğer önceki kart Wild ise, seçilen renge göre kontrol et
        /// 3. Renk eşleşmesi varsa atılabilir
        /// 4. Sayı eşleşmesi varsa atılabilir
        /// 5. Özel kart tipi eşleşmesi varsa atılabilir
        /// </summary>
        public bool CanPlayCard(UnoCard cardToPlay, UnoCard lastCard)
        {
            // Kural 1: Wild kartlar her zaman geçerli
            if (cardToPlay.Color == UnoCard.CardColor.Wild)
                return true;

            // Kural 2: Önceki kart Wild ise, seçilen renge bak
            if (lastCard.Color == UnoCard.CardColor.Wild)
            {
                if (lastCard.ChosenColor.HasValue)
                    return cardToPlay.Color == lastCard.ChosenColor.Value;
                else
                    return true; // ChosenColor yoksa her kart atılabilir
            }

            // Kural 3: Renk eşleşmesi
            if (cardToPlay.Color == lastCard.Color)
                return true;

            // Kural 4: Sayı kartlarında numara eşleşmesi
            if (cardToPlay.Type == UnoCard.CardType.Number && 
                lastCard.Type == UnoCard.CardType.Number)
            {
                return cardToPlay.Number == lastCard.Number;
            }

            // Kural 5: Özel kart tipi eşleşmesi (Skip-Skip, Reverse-Reverse vb.)
            if (cardToPlay.Type == lastCard.Type)
                return true;

            // Hiçbir kural uymazsa atılamaz
            return false;
        }
    }
}