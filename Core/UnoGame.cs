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
    /// UNO oyununun ana motoru. Resmi UNO kurallarına uygun şekilde
    /// kart atma, çekme, pas geçme, UNO çağırma ve tur yönetimini sağlar.
    /// </summary>
    public class UnoGame : BaseGame
    {
        private readonly UnoGameState _unoState;
        private readonly UnoGameRules _unoRules;
        private readonly GameEventDispatcher _eventDispatcher;

        private UnoDeck? _deck;
        private readonly Dictionary<string, PlayerHand> _playerHands;
        private readonly Dictionary<string, bool> _pendingUnoCall;

        private bool _hasDrawnThisTurn;
        private UnoCard? _lastDrawnCard;

        public ITurnManager? CurrentTurnManager => TurnManager;
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
        /// Oyuncunun kart atmasını işler. Wild kartlar için opsiyonel renk seçimi alır.
        /// Sıra kontrolü, penalty kontrolü, kart geçerliliği ve özel efektleri uygular.
        /// </summary>
        public void PlayCard(string playerId, UnoCard card, UnoCard.CardColor? chosenColor = null)
        {
            if (IsGameFinished)
            {
                throw new GameRuleViolationException(
                    "Oyun bitti! Yeni hamle yapamazsın.",
                    "GameOver",
                    GameId);
            }

            CheckPendingUnoCalls();

            if (TurnManager == null || !TurnManager.IsPlayerTurn(playerId))
            {
                throw new InvalidPlayerActionException(
                    "Senin sıran değil!", 
                    playerId, 
                    GameId);
            }

            // DrawPenalty varsa sadece Draw kartı atılabilir (stacking)
            if (_unoState.DrawPenalty > 0)
            {
                bool isDrawCard = card.Type == UnoCard.CardType.DrawTwo || 
                                  card.Type == UnoCard.CardType.WildDrawFour;
                
                if (!isDrawCard)
                {
                    throw new GameRuleViolationException(
                        $"Önce {_unoState.DrawPenalty} kart çekmelisin! (Ya da üstüne Draw kartı at!)", 
                        "DrawPenaltyRequired", 
                        GameId);
                }
            }

            if (_playerHands.ContainsKey(playerId) && !_playerHands[playerId].HasCard(card))
            {
                throw new GameRuleViolationException(
                    "Bu kart elinde yok!", 
                    "CardNotInHand", 
                    GameId);
            }

            if (_unoState.LastPlayedCard != null && 
                !_unoRules.CanPlayCard(card, _unoState.LastPlayedCard))
            {
                throw new GameRuleViolationException(
                    "Bu kartı atamazsın!", 
                    "CardValidation", 
                    GameId);
            }

            // State güncelleme
            _unoState.LastPlayedCard = card;

            // Wild kart renk seçimi
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
            
            // Kartı elden çıkar
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

            var cardPlayedEvent = new CardPlayedEvent(
                playerId,
                GameId,
                card,
                remainingCards
            );
            _eventDispatcher.DispatchAsync(cardPlayedEvent);

            bool skipNextPlayer = ApplyCardEffect(card);

            if (_unoRules.IsGameOver(State))
            {
                EndGame();
                return;
            }

            AdvanceToNextTurn(skipNextPlayer);
        }

        // ═══════════════════════════════════════════
        //  KART ÇEKME
        // ═══════════════════════════════════════════

        /// <summary>
        /// Oyuncunun kart çekmesini işler.
        /// DrawPenalty varsa ceza kartları çekilir ve sıra geçer.
        /// Normal durumda turda 1 kez çekilebilir; çekilen kart atılabiliyorsa oyuncuya bildirilir.
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

            if (IsGameFinished)
            {
                throw new GameRuleViolationException(
                    "Oyun bitti! Yeni hamle yapamazsın.",
                    "GameOver",
                    GameId);
            }

            CheckPendingUnoCalls();

            // Ceza kartı çekme
            if (_unoState.DrawPenalty > 0)
            {
                DrawPenaltyCards(playerId);
                return;
            }

            // Turda sadece 1 kez çekilebilir
            if (_hasDrawnThisTurn)
            {
                throw new GameRuleViolationException(
                    "Bu turda zaten kart çektin! Kart atabilir veya pas geçebilirsin.",
                    "AlreadyDrawn",
                    GameId);
            }

            if (_deck == null || !_playerHands.ContainsKey(playerId)) return;

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

            // Çekilen kart atılabilir mi?
            bool canPlayDrawn = _lastDrawnCard != null && 
                                _unoState.LastPlayedCard != null &&
                                _unoRules.CanPlayCard(_lastDrawnCard, _unoState.LastPlayedCard);

            if (!canPlayDrawn)
            {
                Console.WriteLine($"Cekilen kart atilamaz, sira geciyor.");
                AdvanceToNextTurn(false);
            }
            else
            {
                Console.WriteLine($"Cekilen kart atilabilir! Kart atabilir veya pas gecebilirsin.");
            }
        }

        /// <summary>
        /// DrawPenalty ceza kartlarını çeker ve sırayı sonraki oyuncuya geçirir.
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

            AdvanceToNextTurn(false);
        }

        // ═══════════════════════════════════════════
        //  PAS GEÇME
        // ═══════════════════════════════════════════

        /// <summary>
        /// Kart çektikten sonra oynamamayı tercih eden oyuncunun sırasını geçirir.
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

            if (IsGameFinished)
            {
                throw new GameRuleViolationException(
                    "Oyun bitti! Yeni hamle yapamazsın.",
                    "GameOver",
                    GameId);
            }

            CheckPendingUnoCalls();

            Console.WriteLine($"{GetPlayerName(playerId)} pas gecti.");
            AdvanceToNextTurn(false);
        }

        // ═══════════════════════════════════════════
        //  UNO! ÇAĞIRMA
        // ═══════════════════════════════════════════

        /// <summary>
        /// Oyuncu UNO çağırır. 1 kart kaldığında sıra geçmeden çağrılmazsa +2 ceza uygulanır.
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
        /// UNO dememiş oyuncuları kontrol eder ve +2 ceza uygular.
        /// </summary>
        private void CheckPendingUnoCalls()
        {
            var pendingPlayers = _pendingUnoCall
                .Where(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var pid in pendingPlayers)
            {
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

        public bool HasPendingUnoCall(string playerId)
        {
            return _pendingUnoCall.TryGetValue(playerId, out var pending) && pending;
        }

        // ═══════════════════════════════════════════
        //  TUR YÖNETİMİ
        // ═══════════════════════════════════════════

        /// <summary>
        /// Sırayı sonraki oyuncuya geçirir. skipNextPlayer true ise bir oyuncu atlanır (Skip kartı).
        /// </summary>
        private void AdvanceToNextTurn(bool skipNextPlayer)
        {
            TurnManager!.EndTurn();
            TurnManager.NextTurn();

            if (skipNextPlayer)
            {
                Console.WriteLine($"{TurnManager.CurrentPlayer.Name} atlandi!");
                TurnManager.NextTurn();
            }

            TurnManager.StartTurn();

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
        /// Özel kart efektlerini uygular.
        /// Skip → sonraki oyuncu atlanır, Reverse → yön değişir,
        /// DrawTwo → +2 ceza, WildDrawFour → +4 ceza.
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
                    return false;

                case UnoCard.CardType.WildDrawFour:
                    _unoState.DrawPenalty += 4;
                    Console.WriteLine("+4 Bir sonraki oyuncu 4 kart cekecek!");
                    return false;

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

        /// <summary>
        /// Oyuncunun atılabilir kartlarını döndürür. DrawPenalty varsa sadece Draw kartları listelenir.
        /// </summary>
        public List<UnoCard>? GetPlayableCards(string playerId)
        {
            if (_unoState.LastPlayedCard == null) return null;
            if (!_playerHands.TryGetValue(playerId, out var hand)) return null;

            if (_unoState.DrawPenalty > 0)
            {
                return hand.Cards
                    .Where(c => c.Type == UnoCard.CardType.DrawTwo || 
                                c.Type == UnoCard.CardType.WildDrawFour)
                    .ToList();
            }

            return hand.GetPlayableCards(_unoState.LastPlayedCard);
        }

        public UnoCard? LastPlayedCard => _unoState.LastPlayedCard;
        public bool HasDrawnThisTurn => _hasDrawnThisTurn;

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