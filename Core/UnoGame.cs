using GameCore.Models;     
using GameCore.Interfaces;    
using GameCore.Managers;       
using GameCore.Exceptions;    
using UnoGame.Models.Cards;
using UnoGame.Models.States;
using UnoGame.Rules;
using UnoGame.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnoGame.Core
{
    /// <summary>
    /// UNO oyununun ana motoru — Resmi UNO kurallarına uygun
    /// 
    /// Kurallar:
    /// - Kart çekme: 1 kart çek, atılabiliyorsa at veya pas geç
    /// - DrawTwo: +2 çek VE sıra atlanır
    /// - WildDrawFour: Renk seç + sonraki +4 çeker VE sıra atlanır
    /// - Skip: Sonraki oyuncu atlanır
    /// - Reverse: Yön değişir
    /// - UNO!: 1 kart kaldığında UNO demezsen +2 ceza
    /// </summary>
    public class UnoGame : BaseGame
    {
        private readonly UnoGameState _unoState;
        private readonly UnoGameRules _unoRules;
        private readonly GameEventDispatcher _eventDispatcher;

        // Gerçek kart yönetimi
        private UnoDeck? _deck;
        private readonly Dictionary<string, PlayerHand> _playerHands;

        // UNO! mekanizması
        // Oyuncu 1 kart kaldığında buraya eklenir
        // UNO çağırmazsa ve sıra geçerse +2 ceza
        private readonly Dictionary<string, bool> _pendingUnoCall;

        // Kart çekme sonrası durum
        // Oyuncu kart çekti mi? (Çektiyse ikinci kez çekemez)
        private bool _hasDrawnThisTurn;
        // Çekilen kart atılabilir mi? (atılabilirse oyuncuya bildirilir)
        private UnoCard? _lastDrawnCard;

        public ITurnManager? CurrentTurnManager => TurnManager;

        /// <summary>Oyun bitti mi? (WebSocketHandler tarafından kontrol edilebilir)</summary>
        public bool IsGameFinished => State.Status == GameCore.Enums.GameStatus.Completed;

        public UnoGame(string gameId) 
            : base(gameId, new UnoGameRules())
        {
            _unoState = new UnoGameState(gameId);
            _unoRules = (UnoGameRules)Rules;
            
            State = _unoState;
            _eventDispatcher = new GameEventDispatcher();
            _playerHands = new Dictionary<string, PlayerHand>();
            _pendingUnoCall = new Dictionary<string, bool>();
        }

        // ═══════════════════════════════════════════
        //  OYUN YAŞAM DÖNGÜSÜ
        // ═══════════════════════════════════════════

        protected override void OnGameStarted()
        {
            Console.WriteLine("UNO Oyunu Basliyor...");
            
            _deck = new UnoDeck();

            foreach (var player in Players)
            {
                var hand = new PlayerHand();
                hand.AddCards(_deck.Draw(7));
                _playerHands[player.Id] = hand;
                _unoState.PlayerCardCounts[player.Id] = hand.Count;
            }

            _unoState.LastPlayedCard = _deck.DrawInitialCard();

            TurnManager = new TurnManager(Players);
            TurnManager.StartTurn();
            _hasDrawnThisTurn = false;
            _lastDrawnCard = null;

            Console.WriteLine($"Oyun basladi! Ilk kart: {_unoState.LastPlayedCard}");
            Console.WriteLine($"Sira: {TurnManager.CurrentPlayer.Name}");

            foreach (var player in Players)
            {
                var hand = _playerHands[player.Id];
                Console.WriteLine($"  {player.Name}: {hand.Count} kart - [{string.Join(", ", hand.Cards)}]");
            }

            var gameStartedEvent = new UnoGameStartedEvent(
                GameId,
                Players.Select(p => p.Id).ToList(),
                _unoState.LastPlayedCard.ToString()
            );
            _eventDispatcher.DispatchAsync(gameStartedEvent);
            Console.WriteLine("Event: Oyun basladi eventi gonderildi");
        }

        protected override void OnGameEnded()
        {
            Console.WriteLine("Oyun bitti!");
            
            var winner = _unoRules.FindWinner(State, Players);
            if (winner != null)
            {
                Console.WriteLine($"Kazanan: {winner.Name}");
            }

            int totalTurns = TurnManager?.CurrentTurnNumber ?? 0;
            var duration = (DateTime.UtcNow - _unoState.CreatedAt).TotalSeconds;

            var gameEndedEvent = new UnoGameEndedEvent(
                GameId,
                winner?.Id,
                duration,
                totalTurns
            );
            _eventDispatcher.DispatchAsync(gameEndedEvent);
        }

        // ═══════════════════════════════════════════
        //  KART ATMA
        // ═══════════════════════════════════════════

        /// <summary>
        /// Oyuncu kart atar (Wild kartlar için renk seçimi opsiyonel)
        /// </summary>
        public void PlayCard(string playerId, UnoCard card, UnoCard.CardColor? chosenColor = null)
        {
            // ADIM 0: Oyun bitti mi kontrolü
            if (IsGameFinished)
            {
                throw new GameRuleViolationException(
                    "Oyun bitti! Yeni hamle yapamazsın.",
                    "GameOver",
                    GameId);
            }

            // Önceki oyuncunun UNO kontrolü
            CheckPendingUnoCalls();

            // ADIM 1: Sıra kontrolü
            if (TurnManager == null || !TurnManager.IsPlayerTurn(playerId))
            {
                throw new InvalidPlayerActionException(
                    "Senin sıran değil!", 
                    playerId, 
                    GameId);
            }

            // ADIM 2: DrawPenalty varsa — sadece Draw kartı atabilir (stacking)
            if (_unoState.DrawPenalty > 0)
            {
                // Draw kartı üstüne Draw kartı atılabilir (stacking)
                bool isDrawCard = card.Type == UnoCard.CardType.DrawTwo || 
                                  card.Type == UnoCard.CardType.WildDrawFour;
                
                if (!isDrawCard)
                {
                    throw new GameRuleViolationException(
                        $"Önce {_unoState.DrawPenalty} kart çekmelisin! (Ya da üstüne Draw kartı at!)", 
                        "DrawPenaltyRequired", 
                        GameId);
                }
                // Draw kartı — stacking devam eder, normal validasyona geç
            }

            // ADIM 3: Kartın elde var mı kontrolü
            if (_playerHands.ContainsKey(playerId) && !_playerHands[playerId].HasCard(card))
            {
                throw new GameRuleViolationException(
                    "Bu kart elinde yok!", 
                    "CardNotInHand", 
                    GameId);
            }

            // ADIM 4: Kart geçerliliği kontrolü
            if (_unoState.LastPlayedCard != null && 
                !_unoRules.CanPlayCard(card, _unoState.LastPlayedCard))
            {
                throw new GameRuleViolationException(
                    "Bu kartı atamazsın!", 
                    "CardValidation", 
                    GameId);
            }

            // ADIM 5: State güncelleme
            _unoState.LastPlayedCard = card;

            // Wild kart renk seçimi
            // chosen_color varsa onu kullan, yoksa eldeki en çok renkten seç
            if (card.Color == UnoCard.CardColor.Wild)
            {
                if (chosenColor.HasValue)
                {
                    _unoState.LastPlayedCard.ChosenColor = chosenColor.Value;
                }
                else if (_playerHands.ContainsKey(playerId) && _playerHands[playerId].Count > 0)
                {
                    // Otomatik: eldeki en çok bulunan rengi seç
                    _unoState.LastPlayedCard.ChosenColor = _playerHands[playerId].Cards
                        .Where(c => c.Color != UnoCard.CardColor.Wild)
                        .GroupBy(c => c.Color)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key ?? UnoCard.CardColor.Red;
                }
                else
                {
                    _unoState.LastPlayedCard.ChosenColor = UnoCard.CardColor.Red;
                }
                Console.WriteLine($"Secilen renk: {_unoState.LastPlayedCard.ChosenColor}");
            }
            
            // Kartı elden çıkar ve desteye at
            if (_playerHands.ContainsKey(playerId))
            {
                _playerHands[playerId].RemoveCard(card);
                _unoState.PlayerCardCounts[playerId] = _playerHands[playerId].Count;
            }
            else
            {
                _unoState.PlayerCardCounts[playerId]--;
            }

            _deck?.Discard(card);
            _unoState.MarkAsUpdated();

            Console.WriteLine($"{GetPlayerName(playerId)} karti atti: {card}");

            // UNO kontrolü — 1 kart kaldıysa UNO bekle
            int remainingCards = _unoState.PlayerCardCounts[playerId];
            if (remainingCards == 1)
            {
                _pendingUnoCall[playerId] = true;
            }

            // EVENT: Kart atıldı
            var cardPlayedEvent = new CardPlayedEvent(
                playerId,
                GameId,
                card,
                remainingCards
            );
            _eventDispatcher.DispatchAsync(cardPlayedEvent);

            // ADIM 6: Özel kart efektleri
            bool skipNextPlayer = ApplyCardEffect(card);

            // ADIM 7: Oyun bitiş kontrolü
            if (_unoRules.IsGameOver(State))
            {
                EndGame();
                return;
            }

            // ADIM 8: Sonraki tura geçiş
            AdvanceToNextTurn(skipNextPlayer);
        }

        // ═══════════════════════════════════════════
        //  KART ÇEKME (Resmi Kural: Sadece 1 kart)
        // ═══════════════════════════════════════════

        /// <summary>
        /// Oyuncu kart çeker
        /// 
        /// Resmi kural:
        /// - DrawPenalty varsa: penalty kadar çek, sıra otomatik geçer
        /// - DrawPenalty yoksa: sadece 1 kart çek
        ///   - Çekilen kart atılabiliyorsa: oyuncu atabilir veya pas geçebilir
        ///   - Çekilen kart atılamıyorsa: sıra otomatik geçer
        /// - Bir turda sadece 1 kez çekilebilir
        /// </summary>
        public void DrawCard(string playerId)
        {
            if (TurnManager == null || !TurnManager.IsPlayerTurn(playerId))
            {
                throw new InvalidPlayerActionException(
                    "Senin sıran değil!", 
                    playerId, 
                    GameId);
            }

            // Oyun bitti mi kontrolü
            if (IsGameFinished)
            {
                throw new GameRuleViolationException(
                    "Oyun bitti! Yeni hamle yapamazsın.",
                    "GameOver",
                    GameId);
            }

            // Önceki oyuncunun UNO kontrolü
            CheckPendingUnoCalls();

            // Penalty çekme
            if (_unoState.DrawPenalty > 0)
            {
                DrawPenaltyCards(playerId);
                return;
            }

            // Normal çekme — turda sadece 1 kez
            if (_hasDrawnThisTurn)
            {
                throw new GameRuleViolationException(
                    "Bu turda zaten kart çektin! Kart atabilir veya pas geçebilirsin.",
                    "AlreadyDrawn",
                    GameId);
            }

            if (_deck == null || !_playerHands.ContainsKey(playerId)) return;

            // 1 kart çek
            var drawnCards = _deck.Draw(1);
            _playerHands[playerId].AddCards(drawnCards);
            _unoState.PlayerCardCounts[playerId] = _playerHands[playerId].Count;
            _hasDrawnThisTurn = true;
            _lastDrawnCard = drawnCards.FirstOrDefault();

            Console.WriteLine($"{GetPlayerName(playerId)} 1 kart cekti. Toplam: {_playerHands[playerId].Count}");

            _unoState.MarkAsUpdated();

            var cardDrawnEvent = new CardDrawnEvent(
                playerId,
                GameId,
                1,
                _unoState.PlayerCardCounts[playerId]
            );
            _eventDispatcher.DispatchAsync(cardDrawnEvent);

            // Çekilen kart atılabilir mi kontrol et
            bool canPlayDrawn = _lastDrawnCard != null && 
                                _unoState.LastPlayedCard != null &&
                                _unoRules.CanPlayCard(_lastDrawnCard, _unoState.LastPlayedCard);

            if (!canPlayDrawn)
            {
                // Çekilen kart atılamıyor — sıra otomatik geçer
                Console.WriteLine($"Cekilen kart atilamaz, sira geciyor.");
                AdvanceToNextTurn(false);
            }
            else
            {
                Console.WriteLine($"Cekilen kart atilabilir! Kart atabilir veya pas gecebilirsin.");
            }
        }

        /// <summary>
        /// DrawPenalty ceza kartlarını çeker ve sıra geçer
        /// </summary>
        private void DrawPenaltyCards(string playerId)
        {
            int cardsToDraw = _unoState.DrawPenalty;

            if (_deck != null && _playerHands.ContainsKey(playerId))
            {
                var drawnCards = _deck.Draw(cardsToDraw);
                _playerHands[playerId].AddCards(drawnCards);
                _unoState.PlayerCardCounts[playerId] = _playerHands[playerId].Count;
                
                Console.WriteLine($"{GetPlayerName(playerId)} {drawnCards.Count} ceza karti cekti. Toplam: {_playerHands[playerId].Count}");
            }

            _unoState.DrawPenalty = 0;
            _unoState.MarkAsUpdated();

            var cardDrawnEvent = new CardDrawnEvent(
                playerId,
                GameId,
                cardsToDraw,
                _unoState.PlayerCardCounts[playerId]
            );
            _eventDispatcher.DispatchAsync(cardDrawnEvent);

            // Penalty sonrası sıra geçer
            AdvanceToNextTurn(false);
        }

        // ═══════════════════════════════════════════
        //  PAS GEÇME
        // ═══════════════════════════════════════════

        /// <summary>
        /// Oyuncu kart çektikten sonra oynamamayı tercih ederse
        /// sırayı geçirir. Sadece kart çekildikten sonra kullanılabilir.
        /// </summary>
        public void PassTurn(string playerId)
        {
            if (TurnManager == null || !TurnManager.IsPlayerTurn(playerId))
            {
                throw new InvalidPlayerActionException(
                    "Senin sıran değil!", 
                    playerId, 
                    GameId);
            }

            if (!_hasDrawnThisTurn)
            {
                throw new GameRuleViolationException(
                    "Önce kart çekmelisin! Kart çekmeden pas geçemezsin.",
                    "MustDrawFirst",
                    GameId);
            }

            // Oyun bitti mi kontrolü
            if (IsGameFinished)
            {
                throw new GameRuleViolationException(
                    "Oyun bitti! Yeni hamle yapamazsın.",
                    "GameOver",
                    GameId);
            }

            // Önceki oyuncunun UNO kontrolü
            CheckPendingUnoCalls();

            Console.WriteLine($"{GetPlayerName(playerId)} pas gecti.");
            AdvanceToNextTurn(false);
        }

        // ═══════════════════════════════════════════
        //  UNO! ÇAĞIRMA
        // ═══════════════════════════════════════════

        /// <summary>
        /// Oyuncu UNO! çağırır (1 kart kaldığında)
        /// Sıra geçmeden önce çağırmalı, yoksa +2 ceza
        /// </summary>
        public void CallUno(string playerId)
        {
            if (_pendingUnoCall.ContainsKey(playerId) && _pendingUnoCall[playerId])
            {
                _pendingUnoCall[playerId] = false;
                Console.WriteLine($"UNO! {GetPlayerName(playerId)} UNO dedi!");
            }
            else
            {
                Console.WriteLine($"{GetPlayerName(playerId)} gereksiz UNO dedi (ceza yok).");
            }
        }

        /// <summary>
        /// Bir oyuncunun UNO demediğini kontrol eder
        /// Sıra geçtiğinde çağrılır — UNO dememiş oyuncuya +2 ceza
        /// </summary>
        private void CheckPendingUnoCalls()
        {
            var pendingPlayers = _pendingUnoCall
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var pid in pendingPlayers)
            {
                // +2 ceza kartı
                if (_deck != null && _playerHands.ContainsKey(pid))
                {
                    var penaltyCards = _deck.Draw(2);
                    _playerHands[pid].AddCards(penaltyCards);
                    _unoState.PlayerCardCounts[pid] = _playerHands[pid].Count;
                    
                    Console.WriteLine($"CEZA: {GetPlayerName(pid)} UNO demedi! +2 kart cekti.");

                    var penaltyEvent = new CardDrawnEvent(
                        pid,
                        GameId,
                        2,
                        _unoState.PlayerCardCounts[pid]
                    );
                    _eventDispatcher.DispatchAsync(penaltyEvent);
                }
                _pendingUnoCall[pid] = false;
            }
        }

        /// <summary>
        /// Bir oyuncunun UNO bekleyip beklemediğini kontrol eder
        /// </summary>
        public bool HasPendingUnoCall(string playerId)
        {
            return _pendingUnoCall.TryGetValue(playerId, out var pending) && pending;
        }

        // ═══════════════════════════════════════════
        //  TUR YÖNETİMİ
        // ═══════════════════════════════════════════

        /// <summary>
        /// Sırayı bir sonraki oyuncuya geçirir
        /// Skip parametresi: true ise sonraki oyuncu atlanır
        /// </summary>
        private void AdvanceToNextTurn(bool skipNextPlayer)
        {
            // NOT: CheckPendingUnoCalls burada DEĞİL, sonraki oyuncunun
            // aksiyonunda çalışır. Bu sayede oyuncu UNO butonuna basma
            // şansı bulur.

            TurnManager!.EndTurn();
            TurnManager.NextTurn();

            if (skipNextPlayer)
            {
                Console.WriteLine($"{TurnManager.CurrentPlayer.Name} atlandi!");
                TurnManager.NextTurn();
            }

            TurnManager.StartTurn();

            // Yeni tur başlangıcı
            _hasDrawnThisTurn = false;
            _lastDrawnCard = null;

            Console.WriteLine($"Siradaki: {TurnManager.CurrentPlayer.Name}");

            var turnChangedEvent = new TurnChangedEvent(
                GameId,
                TurnManager.CurrentPlayer.Id,
                TurnManager.CurrentTurnNumber
            );
            _eventDispatcher.DispatchAsync(turnChangedEvent);
        }

        // ═══════════════════════════════════════════
        //  KART ETKİLERİ
        // ═══════════════════════════════════════════

        /// <summary>
        /// Özel kart efektlerini uygular
        /// Dönüş: true ise bir sonraki oyuncu TAMAMEN atlanır (Skip kartı)
        /// 
        /// Resmi kurallar:
        /// - Skip: sonraki oyuncu atlanır → return true
        /// - DrawTwo: +2 ceza, sıra cezalı oyuncuya geçer (sadece çeker) → return false
        /// - WildDrawFour: +4 ceza, sıra cezalı oyuncuya geçer (sadece çeker) → return false
        /// - Reverse: yön değişir → return false
        /// 
        /// NOT: DrawTwo/WildDrawFour skip YAPMAZ!
        /// Cezalı oyuncunun sırası gelir, sadece kart çekebilir (PlayCard'da DrawPenalty kontrolü var),
        /// çektikten sonra DrawPenaltyCards otomatik olarak sırayı geçirir.
        /// </summary>
        private bool ApplyCardEffect(UnoCard card)
        {
            var turnMgr = TurnManager as TurnManager;

            switch (card.Type)
            {
                case UnoCard.CardType.Skip:
                    Console.WriteLine("Skip karti! Bir sonraki oyuncu atlanacak!");
                    return true;

                case UnoCard.CardType.Reverse:
                    _unoState.IsClockwise = !_unoState.IsClockwise;
                    turnMgr?.ReverseDirection();
                    Console.WriteLine("Yon degisti!");
                    return false;

                case UnoCard.CardType.DrawTwo:
                    _unoState.DrawPenalty += 2;
                    Console.WriteLine("+2 Bir sonraki oyuncu 2 kart cekecek!");
                    return false;  // Skip YAPMA — sıra cezalı oyuncuya geçsin

                case UnoCard.CardType.WildDrawFour:
                    _unoState.DrawPenalty += 4;
                    Console.WriteLine("+4 Bir sonraki oyuncu 4 kart cekecek!");
                    return false;  // Skip YAPMA — sıra cezalı oyuncuya geçsin

                default:
                    return false;
            }
        }

        // ═══════════════════════════════════════════
        //  YARDIMCI METODLAR
        // ═══════════════════════════════════════════

        public IReadOnlyList<UnoCard>? GetPlayerCards(string playerId)
        {
            return _playerHands.TryGetValue(playerId, out var hand) ? hand.Cards : null;
        }

        public List<UnoCard>? GetPlayableCards(string playerId)
        {
            if (_unoState.LastPlayedCard == null) return null;
            if (!_playerHands.TryGetValue(playerId, out var hand)) return null;

            // DrawPenalty varsa sadece Draw kartları atilabilir (stacking)
            if (_unoState.DrawPenalty > 0)
            {
                return hand.Cards
                    .Where(c => c.Type == UnoCard.CardType.DrawTwo || 
                                c.Type == UnoCard.CardType.WildDrawFour)
                    .ToList();
            }

            return hand.GetPlayableCards(_unoState.LastPlayedCard);
        }

        /// <summary>Ortadaki son kart (UI için)</summary>
        public UnoCard? LastPlayedCard => _unoState.LastPlayedCard;

        /// <summary>Bu turda kart çekildi mi?</summary>
        public bool HasDrawnThisTurn => _hasDrawnThisTurn;

        /// <summary>Çekilen kart atılabilir mi?</summary>
        public bool CanPlayDrawnCard
        {
            get
            {
                if (_lastDrawnCard == null || _unoState.LastPlayedCard == null) return false;
                return _unoRules.CanPlayCard(_lastDrawnCard, _unoState.LastPlayedCard);
            }
        }

        private string GetPlayerName(string playerId)
        {
            return Players.FirstOrDefault(p => p.Id == playerId)?.Name ?? playerId;
        }

        public void AddEventListener(IGameEventListener listener)
        {
            _eventDispatcher.Subscribe(listener);
        }

        public void RemoveEventListener(IGameEventListener listener)
        {
            _eventDispatcher.Unsubscribe(listener);
        }

        public void AddEventListenerAsync(IAsyncGameEventListener listener)
        {
            _eventDispatcher.SubscribeAsync(listener);
        }     
    }
}