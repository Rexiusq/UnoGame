using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnoGame.Core;
using UnoGame.Models.Cards;
using GameCore.Models;

namespace UnoGame.Benchmark
{
    class BenchmarkRunner
    {
        // ═══════════════════════════════════════════════════════════════
        //  Benchmark renk kodları
        // ═══════════════════════════════════════════════════════════════
        static readonly string RESET  = "\u001b[0m";
        static readonly string CYAN   = "\u001b[36m";
        static readonly string GREEN  = "\u001b[32m";
        static readonly string YELLOW = "\u001b[33m";
        static readonly string RED    = "\u001b[31m";
        static readonly string BOLD   = "\u001b[1m";
        static readonly string DIM    = "\u001b[2m";

        static void Main(string[] args)
        {
            // Console output'u temizle — oyun motoru çok fazla Console.WriteLine yapıyor
            var originalOut = Console.Out;
            Console.SetOut(TextWriter.Null);

            // Banner'ı gerçek console'a yaz
            originalOut.WriteLine();
            originalOut.WriteLine($"{BOLD}{CYAN}╔══════════════════════════════════════════════════════════════╗{RESET}");
            originalOut.WriteLine($"{BOLD}{CYAN}║         UNO GAME — BENCHMARK & RAM ANALİZİ                 ║{RESET}");
            originalOut.WriteLine($"{BOLD}{CYAN}║         Sunucu Boyutlandırma Raporu                        ║{RESET}");
            originalOut.WriteLine($"{BOLD}{CYAN}╚══════════════════════════════════════════════════════════════╝{RESET}");
            originalOut.WriteLine();

            // ─── TEST 1: Baseline RAM ───
            originalOut.WriteLine($"{BOLD}{YELLOW}━━━ TEST 1: Baseline RAM Ölçümü ━━━{RESET}");
            ForceGC();
            long baselineManaged   = GC.GetTotalMemory(true);
            long baselineWorkingSet = Process.GetCurrentProcess().WorkingSet64;

            originalOut.WriteLine($"  Managed Heap (GC)  : {BOLD}{FormatBytes(baselineManaged)}{RESET}");
            originalOut.WriteLine($"  Working Set (OS)   : {BOLD}{FormatBytes(baselineWorkingSet)}{RESET}");
            originalOut.WriteLine();

            // ─── TEST 2: Tek Oyun Maliyeti ───
            originalOut.WriteLine($"{BOLD}{YELLOW}━━━ TEST 2: Tek Oyun Bellek Maliyeti ━━━{RESET}");
            ForceGC();
            long beforeSingle = GC.GetTotalMemory(true);

            var singleGame = CreateAndStartGame("single-game-0", 4);

            ForceGC();
            long afterSingle = GC.GetTotalMemory(true);
            long singleGameCost = afterSingle - beforeSingle;

            originalOut.WriteLine($"  Oyun Öncesi        : {FormatBytes(beforeSingle)}");
            originalOut.WriteLine($"  Oyun Sonrası       : {FormatBytes(afterSingle)}");
            originalOut.WriteLine($"  {GREEN}Tek Oyun Maliyeti  : {BOLD}{FormatBytes(singleGameCost)}{RESET}");
            originalOut.WriteLine($"  (4 oyuncu, 7'şer kart, 108 kartlık deste)");
            originalOut.WriteLine();

            // Tek oyunu serbest bırak
            singleGame = null;
            ForceGC();

            // ─── TEST 3: Ölçeklenme Testi ───
            originalOut.WriteLine($"{BOLD}{YELLOW}━━━ TEST 3: Ölçeklenme Testi (Eşzamanlı Oyunlar) ━━━{RESET}");
            originalOut.WriteLine();

            int[] scaleLevels = { 10, 50, 100, 500, 1000 };
            var scaleResults = new List<(int count, long totalManagedBytes, long perGameBytes, long workingSet, double createTimeMs)>();

            // Tablo başlığı
            originalOut.WriteLine($"  {DIM}{"Oyun Sayısı",12} │ {"Toplam RAM",14} │ {"Oyun Başına",14} │ {"Working Set",14} │ {"Oluşturma Süresi",18}{RESET}");
            originalOut.WriteLine($"  {DIM}{"─────────────",12}─┼─{"──────────────",14}─┼─{"──────────────",14}─┼─{"──────────────",14}─┼─{"──────────────────",18}{RESET}");

            foreach (int count in scaleLevels)
            {
                ForceGC();
                long beforeScale = GC.GetTotalMemory(true);

                var sw = Stopwatch.StartNew();
                var games = new List<UnoGame.Core.UnoGame>(count);
                for (int i = 0; i < count; i++)
                {
                    games.Add(CreateAndStartGame($"scale-{count}-{i}", 4));
                }
                sw.Stop();

                ForceGC();
                long afterScale = GC.GetTotalMemory(true);
                long totalCost = afterScale - beforeScale;
                long perGame = totalCost / count;
                long workingSet = Process.GetCurrentProcess().WorkingSet64;

                scaleResults.Add((count, totalCost, perGame, workingSet, sw.Elapsed.TotalMilliseconds));

                string countStr = count.ToString().PadLeft(8);
                originalOut.WriteLine($"  {GREEN}{countStr,12}{RESET} │ {BOLD}{FormatBytes(totalCost),14}{RESET} │ {FormatBytes(perGame),14} │ {FormatBytes(workingSet),14} │ {sw.Elapsed.TotalMilliseconds,14:F1} ms");

                // Temizle
                games.Clear();
                games = null;
                ForceGC();
            }
            originalOut.WriteLine();

            // ─── TEST 4: Gameplay Simülasyonu ───
            originalOut.WriteLine($"{BOLD}{YELLOW}━━━ TEST 4: Gameplay Simülasyonu (100 Oyun) ━━━{RESET}");
            
            ForceGC();
            long beforeGameplay = GC.GetTotalMemory(true);
            int gen0Before = GC.CollectionCount(0);
            int gen1Before = GC.CollectionCount(1);
            int gen2Before = GC.CollectionCount(2);

            int totalGames = 100;
            int totalTurns = 0;
            int totalCardsPlayed = 0;
            int totalCardsDrawn = 0;
            int completedGames = 0;
            var gameplayStopwatch = Stopwatch.StartNew();

            var random = new Random(42); // deterministik seed

            for (int g = 0; g < totalGames; g++)
            {
                var game = CreateAndStartGame($"gameplay-{g}", 4);
                int maxTurns = 200; // sonsuz döngü koruması
                int turns = 0;

                while (!game.IsGameFinished && turns < maxTurns)
                {
                    turns++;
                    try
                    {
                        var currentPlayer = game.CurrentTurnManager!.CurrentPlayer;
                        string pid = currentPlayer.Id;

                        var playableCards = game.GetPlayableCards(pid);

                        if (playableCards != null && playableCards.Count > 0)
                        {
                            // Rastgele atılabilir bir kart seç
                            var cardToPlay = playableCards[random.Next(playableCards.Count)];
                            
                            UnoCard.CardColor? chosenColor = null;
                            if (cardToPlay.Color == UnoCard.CardColor.Wild)
                            {
                                var colors = new[] { UnoCard.CardColor.Red, UnoCard.CardColor.Blue, UnoCard.CardColor.Green, UnoCard.CardColor.Yellow };
                                chosenColor = colors[random.Next(colors.Length)];
                            }

                            game.PlayCard(pid, cardToPlay, chosenColor);
                            totalCardsPlayed++;

                            // 1 kart kaldıysa UNO de
                            var remainingCards = game.GetPlayerCards(pid);
                            if (remainingCards != null && remainingCards.Count == 1)
                            {
                                game.CallUno(pid);
                            }
                        }
                        else
                        {
                            // Kart çek
                            game.DrawCard(pid);
                            totalCardsDrawn++;

                            // Çekilen kart oynanabiliyorsa oyna
                            if (game.CanPlayDrawnCard && !game.IsGameFinished)
                            {
                                var newPlayable = game.GetPlayableCards(pid);
                                if (newPlayable != null && newPlayable.Count > 0)
                                {
                                    var card = newPlayable[0];
                                    UnoCard.CardColor? color = null;
                                    if (card.Color == UnoCard.CardColor.Wild)
                                    {
                                        var colors = new[] { UnoCard.CardColor.Red, UnoCard.CardColor.Blue, UnoCard.CardColor.Green, UnoCard.CardColor.Yellow };
                                        color = colors[random.Next(colors.Length)];
                                    }
                                    game.PlayCard(pid, card, color);
                                    totalCardsPlayed++;
                                }
                                else
                                {
                                    // Pas geç
                                    if (game.HasDrawnThisTurn && !game.IsGameFinished)
                                    {
                                        game.PassTurn(pid);
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Oyun kuralı ihlali — devam et
                        try
                        {
                            var pid = game.CurrentTurnManager?.CurrentPlayer.Id;
                            if (pid != null && !game.IsGameFinished)
                            {
                                game.DrawCard(pid);
                                totalCardsDrawn++;
                                if (game.HasDrawnThisTurn && !game.IsGameFinished)
                                {
                                    game.PassTurn(pid);
                                }
                            }
                        }
                        catch { /* skip turn */ }
                    }
                }

                totalTurns += turns;
                if (game.IsGameFinished) completedGames++;
            }
            gameplayStopwatch.Stop();

            ForceGC();
            long afterGameplay = GC.GetTotalMemory(true);

            originalOut.WriteLine($"  Tamamlanan Oyunlar : {BOLD}{completedGames}{RESET} / {totalGames}");
            originalOut.WriteLine($"  Toplam Tur Sayısı  : {BOLD}{totalTurns}{RESET}");
            originalOut.WriteLine($"  Atılan Kart        : {BOLD}{totalCardsPlayed}{RESET}");
            originalOut.WriteLine($"  Çekilen Kart       : {BOLD}{totalCardsDrawn}{RESET}");
            originalOut.WriteLine($"  Toplam Süre        : {BOLD}{gameplayStopwatch.Elapsed.TotalMilliseconds:F1} ms{RESET}");
            
            double avgTurnTime = totalTurns > 0 
                ? gameplayStopwatch.Elapsed.TotalMilliseconds / totalTurns 
                : 0;
            originalOut.WriteLine($"  Tur Başına Süre    : {BOLD}{avgTurnTime:F4} ms{RESET}");
            originalOut.WriteLine($"  Gameplay RAM Delta : {FormatBytes(afterGameplay - beforeGameplay)}");
            originalOut.WriteLine();

            // ─── TEST 5: GC Detay Raporu ───
            originalOut.WriteLine($"{BOLD}{YELLOW}━━━ TEST 5: GC & Bellek Detay Raporu ━━━{RESET}");

            int gen0After = GC.CollectionCount(0);
            int gen1After = GC.CollectionCount(1);
            int gen2After = GC.CollectionCount(2);

            var gcInfo = GC.GetGCMemoryInfo();

            originalOut.WriteLine($"  GC Collection Sayıları (gameplay sırasında):");
            originalOut.WriteLine($"    Gen 0 : {BOLD}{gen0After - gen0Before}{RESET} collection");
            originalOut.WriteLine($"    Gen 1 : {BOLD}{gen1After - gen1Before}{RESET} collection");
            originalOut.WriteLine($"    Gen 2 : {BOLD}{gen2After - gen2Before}{RESET} collection");
            originalOut.WriteLine();
            originalOut.WriteLine($"  GC Heap Bilgileri:");
            originalOut.WriteLine($"    Heap Boyutu       : {BOLD}{FormatBytes(gcInfo.HeapSizeBytes)}{RESET}");
            originalOut.WriteLine($"    Committed Bellek  : {BOLD}{FormatBytes(gcInfo.TotalCommittedBytes)}{RESET}");
            originalOut.WriteLine($"    Available Bellek  : {BOLD}{FormatBytes(gcInfo.TotalAvailableMemoryBytes)}{RESET}");
            originalOut.WriteLine();

            var process = Process.GetCurrentProcess();
            originalOut.WriteLine($"  İşlem Bellek Bilgileri:");
            originalOut.WriteLine($"    Working Set       : {BOLD}{FormatBytes(process.WorkingSet64)}{RESET}");
            originalOut.WriteLine($"    Private Memory    : {BOLD}{FormatBytes(process.PrivateMemorySize64)}{RESET}");
            originalOut.WriteLine($"    Virtual Memory    : {BOLD}{FormatBytes(process.VirtualMemorySize64)}{RESET}");
            originalOut.WriteLine($"    Paged Memory      : {BOLD}{FormatBytes(process.PagedMemorySize64)}{RESET}");
            originalOut.WriteLine();

            // ─── ÖZET ───
            originalOut.WriteLine($"{BOLD}{CYAN}╔══════════════════════════════════════════════════════════════╗{RESET}");
            originalOut.WriteLine($"{BOLD}{CYAN}║                    SONUÇ ÖZETİ                              ║{RESET}");
            originalOut.WriteLine($"{BOLD}{CYAN}╚══════════════════════════════════════════════════════════════╝{RESET}");
            originalOut.WriteLine();

            // En son ölçeklenme testinden 1000 oyun verisi
            var scale1000 = scaleResults.FirstOrDefault(r => r.count == 1000);

            originalOut.WriteLine($"  {BOLD}Tek Oyun RAM Maliyeti       :{RESET} ~{FormatBytes(singleGameCost)}");
            if (scale1000.count > 0)
            {
                originalOut.WriteLine($"  {BOLD}1000 Oyun Toplam RAM        :{RESET} ~{FormatBytes(scale1000.totalManagedBytes)}");
                originalOut.WriteLine($"  {BOLD}1000 Oyun Working Set       :{RESET} ~{FormatBytes(scale1000.workingSet)}");
                originalOut.WriteLine($"  {BOLD}Oyun Başına Ortalama RAM    :{RESET} ~{FormatBytes(scale1000.perGameBytes)}");
            }
            originalOut.WriteLine($"  {BOLD}İşlem Toplam Working Set    :{RESET} ~{FormatBytes(process.WorkingSet64)}");
            originalOut.WriteLine($"  {BOLD}Tur İşlem Hızı              :{RESET} ~{avgTurnTime:F4} ms/tur");
            originalOut.WriteLine();

            // Sunucu önerileri
            originalOut.WriteLine($"{BOLD}{GREEN}  ─── Sunucu Boyutlandırma Önerisi ───{RESET}");
            if (scale1000.count > 0)
            {
                double mbPer1000 = scale1000.totalManagedBytes / (1024.0 * 1024.0);
                double mbPerGame = scale1000.perGameBytes / (1024.0 * 1024.0);
                double gamesPerGB = (1024.0) / mbPerGame;

                originalOut.WriteLine($"  • 1 GB RAM ile tahmini eşzamanlı oyun: ~{(int)gamesPerGB} oyun");
                originalOut.WriteLine($"  • 2 GB RAM ile tahmini eşzamanlı oyun: ~{(int)(gamesPerGB * 2)} oyun");
                originalOut.WriteLine($"  • 4 GB RAM ile tahmini eşzamanlı oyun: ~{(int)(gamesPerGB * 4)} oyun");
                originalOut.WriteLine($"  • 8 GB RAM ile tahmini eşzamanlı oyun: ~{(int)(gamesPerGB * 8)} oyun");
            }
            originalOut.WriteLine();

            originalOut.WriteLine($"{DIM}  Not: Bu değerler sadece oyun motoru için geçerlidir.{RESET}");
            originalOut.WriteLine($"{DIM}  WebSocket bağlantıları, MongoDB ve ASP.NET overhead'i hariçtir.{RESET}");
            originalOut.WriteLine($"{DIM}  Gerçek sunucu yükünde ek ~200-500 MB OS/runtime overhead beklenmelidir.{RESET}");
            originalOut.WriteLine();

            // Console'u geri yükle
            Console.SetOut(originalOut);
        }

        // ═══════════════════════════════════════════
        //  Yardımcı Metodlar
        // ═══════════════════════════════════════════

        static UnoGame.Core.UnoGame CreateAndStartGame(string gameId, int playerCount)
        {
            var game = new UnoGame.Core.UnoGame(gameId);
            for (int i = 0; i < playerCount; i++)
            {
                game.AddPlayer(new Player($"p{i}", $"Player{i}"));
            }
            game.StartGame();
            return game;
        }

        static void ForceGC()
        {
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
        }

        static string FormatBytes(long bytes)
        {
            if (bytes < 0) return $"-{FormatBytes(-bytes)}";

            string[] units = { "B", "KB", "MB", "GB" };
            double value = bytes;
            int unitIndex = 0;

            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return $"{value:F2} {units[unitIndex]}";
        }
    }
}
