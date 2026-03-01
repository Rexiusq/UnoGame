# 🎮 UnoGame — Real-Time Multiplayer UNO Card Game

<div align="center">

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-13.0-239120?style=for-the-badge&logo=csharp&logoColor=white)
![WebSocket](https://img.shields.io/badge/WebSocket-Real--Time-010101?style=for-the-badge&logo=socketdotio&logoColor=white)
![JSON-RPC](https://img.shields.io/badge/JSON--RPC-2.0-F7DF1E?style=for-the-badge&logo=json&logoColor=black)
![License](https://img.shields.io/badge/License-MIT-blue?style=for-the-badge)

**Resmi UNO kurallarına uygun, gerçek zamanlı çok oyunculu UNO kart oyunu.**

WebSocket üzerinden anlık iletişim • JSON-RPC 2.0 protokolü • Event-Driven mimari • SOLID prensipleri

[Kurulum](#-kurulum) · [Mimari](#-mimari-genel-bakış) · [API Referansı](#-websocket-api-referansı) · [Oyun Kuralları](#-oyun-kuralları)

</div>

---

## 📋 İçindekiler

- [Proje Hakkında](#-proje-hakkında)
- [Özellikler](#-özellikler)
- [Teknoloji Stack](#-teknoloji-stack)
- [Kurulum](#-kurulum)
- [Mimari Genel Bakış](#-mimari-genel-bakış)
- [Proje Yapısı](#-proje-yapısı)
- [Katmanlı Mimari Detayları](#-katmanlı-mimari-detayları)
- [WebSocket API Referansı](#-websocket-api-referansı)
- [REST API Endpoints](#-rest-api-endpoints)
- [Oyun Kuralları](#-oyun-kuralları)
- [Test Arayüzü](#-test-arayüzü)
- [Geliştirme Notları](#-geliştirme-notları)

---

## 🎯 Proje Hakkında

**UnoGame**, .NET 9.0 üzerinde geliştirilmiş, WebSocket tabanlı gerçek zamanlı çok oyunculu bir UNO kart oyunu motorudur. Proje, oyun geliştirme sürecinde yazılım mühendisliği prensiplerinin (SOLID, Event-Driven Architecture, Clean Architecture) nasıl uygulanacağını gösteren kapsamlı bir örnek uygulamadır.

Sistem, **GameCore** adlı genel amaçlı bir oyun çerçevesi (framework) üzerine inşa edilmiştir. GameCore, oyun yaşam döngüsü, tur yönetimi, oyuncu modelleri ve event dispatching gibi temel mekanizmaları sağlarken, UnoGame projesi UNO'ya özgü iş mantığını (kart kuralları, deste yönetimi, özel kart efektleri) bu çerçeve üzerinde implemente eder.

### Motivasyon

- **Gerçek zamanlı iletişim**: WebSocket ile düşük gecikmeli, çift yönlü haberleşme
- **Protokol standardizasyonu**: JSON-RPC 2.0 ile tutarlı ve genişletilebilir mesajlaşma
- **Genişletilebilir mimari**: Yeni oyunlar (Pişti, Okey vb.) aynı GameCore altyapısı üzerine kolayca inşa edilebilir
- **SOLID prensipleri**: İş yerinde kullanılabilir düzeyde temiz kod ve ayrıştırılmış sorumluluklar

---

## ✨ Özellikler

### Oyun Mekaniği
- ✅ Standart 108 kartlık UNO destesi (4 renk × 25 kart + 8 Wild kart)
- ✅ Resmi UNO kurallarına tam uyum
- ✅ Özel kart efektleri: **Skip**, **Reverse**, **Draw Two**, **Wild**, **Wild Draw Four**
- ✅ UNO çağırma mekanizması (+2 ceza)
- ✅ Draw kartı stacking (üst üste +2/+4 atabilme)
- ✅ Kart çekme sonrası oynama veya pas geçme seçeneği
- ✅ Otomatik deste yeniden karıştırma (deste bittiğinde)
- ✅ Oyun sonu tespiti ve kazanan belirleme

### Teknik Özellikler
- ✅ WebSocket ile gerçek zamanlı çift yönlü iletişim
- ✅ JSON-RPC 2.0 standardına uygun mesaj formatı
- ✅ Event-Driven Architecture ile loosely-coupled bileşenler
- ✅ Thread-safe bağlantı yönetimi (`ConcurrentDictionary`)
- ✅ Async/await tabanlı non-blocking I/O
- ✅ GameCore framework üzerinde genişletilebilir tasarım
- ✅ Web tabanlı test arayüzü (HTML/CSS/JS)
- ✅ REST API endpoints (durum kontrolü, oyun state)
- ✅ Kapsamlı hata yönetimi ve özel exception sınıfları

---

## 🛠 Teknoloji Stack

| Katman | Teknoloji | Açıklama |
|--------|-----------|----------|
| **Runtime** | .NET 9.0 | Modern, cross-platform çalışma ortamı |
| **Dil** | C# 13 | Strongly-typed, OOP & FP desteği |
| **Web Server** | ASP.NET Core (Kestrel) | Yüksek performanslı HTTP/WebSocket sunucu |
| **İletişim** | WebSocket | Full-duplex, düşük gecikmeli gerçek zamanlı iletişim |
| **Protokol** | JSON-RPC 2.0 | Standart, genişletilebilir RPC mesaj formatı |
| **Serialization** | System.Text.Json | Yüksek performanslı JSON işleme |
| **Framework** | GameCore (custom) | Oyun yaşam döngüsü, tur yönetimi, event sistemi |
| **Frontend** | HTML5 / CSS3 / Vanilla JS | Hafif, bağımlılıksız test arayüzü |

---

## 🚀 Kurulum

### Ön Gereksinimler

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Git

### Adım 1: Projeyi Klonlayın

```bash
git clone https://github.com/Rexiusq/UnoGame.git
cd UnoGame
```

### Adım 2: Bağımlılıkları Yükleyin

```bash
dotnet restore
```

### Adım 3: Projeyi Derleyin

```bash
dotnet build
```

### Adım 4: Sunucuyu Başlatın

```bash
dotnet run
```

Sunucu başlatıldığında aşağıdaki çıktıyı göreceksiniz:

```
═══════════════════════════════════════════
     UNO OYUNU - WebSocket Sunucusu
═══════════════════════════════════════════

✅ 3 oyuncu eklendi
✅ WebSocket Event Listener aktif!
✅ JSON-RPC Backend Service aktif!

═══════════════════════════════════════════
   Sunucu başlatiliyor...
   WebSocket: ws://localhost:5000/ws?playerId=p1
    Test UI:   http://localhost:5000
   API:       http://localhost:5000/api/status
═══════════════════════════════════════════
```

### Adım 5: Test Arayüzünü Açın

Tarayıcınızda `http://localhost:5000` adresine gidin.

---

## 🏗 Mimari Genel Bakış

Proje, **katmanlı mimari** (Layered Architecture) ve **Event-Driven Architecture** prensiplerine göre tasarlanmıştır.

```mermaid
graph TB
    subgraph Client ["🖥️ Client Layer"]
        HTML["Test UI<br/>(HTML/CSS/JS)"]
        MOBILE["Mobil / Diğer Client<br/>(Gelecek)"]
    end

    subgraph Transport ["🔌 Transport Layer"]
        WS["WebSocket<br/>Handler"]
        REST["REST API<br/>Endpoints"]
    end

    subgraph Backend ["⚙️ Backend Layer"]
        CM["Connection<br/>Manager"]
        JRPC["JSON-RPC<br/>Backend Service"]
        WEL["WebSocket<br/>Event Listener"]
    end

    subgraph Core ["🎮 Game Core"]
        ENGINE["UnoGame<br/>Engine"]
        RULES["UnoGame<br/>Rules"]
        ED["Event<br/>Dispatcher"]
    end

    subgraph Domain ["📦 Domain Models"]
        CARD["UnoCard"]
        DECK["UnoDeck"]
        HAND["PlayerHand"]
        STATE["UnoGameState"]
    end

    subgraph Framework ["🧱 GameCore Framework"]
        BG["BaseGame"]
        TM["TurnManager"]
        BGA["BaseGameAction"]
        BGS["BaseGameState"]
    end

    HTML -- WebSocket --> WS
    MOBILE -. WebSocket .-> WS
    HTML -- HTTP --> REST
    WS --> CM
    WS --> ENGINE
    ENGINE --> RULES
    ENGINE --> ED
    ED --> WEL
    ED --> JRPC
    WEL --> CM
    ENGINE --> DECK
    ENGINE --> HAND
    ENGINE --> STATE
    ENGINE --> BG
    RULES -.-> BGS
    STATE --> BGS
    ED --> BGA
    ENGINE --> TM

    style Client fill:#1a1a2e,stroke:#e94560,color:#fff
    style Transport fill:#16213e,stroke:#0f3460,color:#fff
    style Backend fill:#1a1a2e,stroke:#e94560,color:#fff
    style Core fill:#0f3460,stroke:#533483,color:#fff
    style Domain fill:#16213e,stroke:#0f3460,color:#fff
    style Framework fill:#533483,stroke:#e94560,color:#fff
```

### Veri Akışı

```
Client (WebSocket) → WebSocketHandler → UnoGame Engine → Event Dispatcher
                                                              ↓
                                          ┌─────────────────────────────────────┐
                                          │  WebSocketEventListener (broadcast) │
                                          │  JsonRpcBackendService (logging)    │
                                          │  ConsoleEventListener (debug)       │
                                          └─────────────────────────────────────┘
                                                              ↓
                                                    All Connected Clients
```

1. **Client** WebSocket üzerinden JSON-RPC 2.0 formatında komut gönderir
2. **WebSocketHandler** mesajı parse eder ve uygun oyun aksiyonunu çağırır
3. **UnoGame Engine** kural kontrollerini yapar, state'i günceller ve event fırlatır
4. **Event Dispatcher** event'i kayıtlı tüm listener'lara iletir
5. **WebSocketEventListener** event'i JSON-RPC formatında tüm client'lara broadcast eder

---

## 📁 Proje Yapısı

```
UnoGame/
├── Program.cs                          # Uygulama giriş noktası, sunucu yapılandırması
├── UnoGame.csproj                      # Proje dosyası (.NET 9.0, GameCore referansı)
├── UnoGame.sln                         # Solution dosyası
│
├── Core/
│   └── UnoGame.cs                      # 🎮 Ana oyun motoru (644 satır)
│                                       #    - Oyun yaşam döngüsü
│                                       #    - Kart atma / çekme / pas geçme
│                                       #    - UNO çağırma mekanizması
│                                       #    - Özel kart efektleri
│                                       #    - Tur yönetimi
│
├── Models/
│   ├── Cards/
│   │   ├── UnoCard.cs                  # 🃏 Kart modeli (renk, tip, numara)
│   │   ├── UnoDeck.cs                  # 📦 108 kartlık deste (çek, at, karıştır)
│   │   └── PlayerHand.cs              # ✋ Oyuncu eli (kart ekleme/çıkarma/sorgulama)
│   └── States/
│       └── UnoGameStates.cs            # 📊 Oyun durumu (BaseGameState'den türer)
│
├── Rules/
│   └── UnoGameRules.cs                 # 📏 Oyun kuralları (IGameRules implementasyonu)
│                                       #    - Kart geçerliliği kontrolü
│                                       #    - Oyun bitiş kontrolü
│                                       #    - Kazanan belirleme
│
├── Events/
│   ├── BaseUnoEvent.cs                 # 🔔 Tüm eventlerin temel sınıfı
│   ├── UnoGameStartedEvent.cs          # Oyun başladı eventi
│   ├── CardPlayedEvent.cs              # Kart atıldı eventi
│   ├── CardDrawnEvent.cs               # Kart çekildi eventi
│   ├── TurnChangedEvent.cs             # Tur değişti eventi
│   └── UnoGameEndedEvent.cs            # Oyun bitti eventi
│
├── Listeners/
│   └── ConsoleEventListener.cs         # 🖥️ Konsol event dinleyicisi (debug)
│
├── Backend/
│   ├── Protocol/
│   │   └── JsonRpcRequest.cs           # 📨 JSON-RPC 2.0 Request/Response modelleri
│   ├── Models/
│   │   ├── EventParams.cs              # 📋 Event parametreleri (DTO'lar)
│   │   └── GameStateModels.cs          # 📋 Oyun durumu DTO'ları (CardDto, PlayerStateDto...)
│   ├── Services/
│   │   └── JsonRpcBackendService.cs    # ⚙️ Backend JSON-RPC servis (event logging)
│   └── WebSocket/
│       ├── WebSocketHandler.cs         # 🔧 Client komut işleyici (389 satır)
│       ├── WebSocketEventListener.cs   # 📡 Event → WebSocket broadcast
│       └── WebSocketConnectionManager.cs # 🔌 Bağlantı yönetimi (thread-safe)
│
└── wwwroot/
    └── index.html                      # 🌐 Web tabanlı test arayüzü (715 satır)
```

---

## 🧩 Katmanlı Mimari Detayları

### 1. Domain Layer — `Models/`

Oyunun temel veri modellerini içerir. Hiçbir harici bağımlılığı yoktur.

#### `UnoCard` — Kart Modeli
```csharp
public class UnoCard
{
    public enum CardColor { Red, Blue, Green, Yellow, Wild }
    public enum CardType  { Number, Skip, Reverse, DrawTwo, Wild, WildDrawFour }

    public CardColor Color { get; set; }
    public CardType Type { get; set; }
    public int? Number { get; set; }           // 0-9 (sadece Number tipi)
    public CardColor? ChosenColor { get; set; } // Wild kartlarda seçilen renk
}
```

#### `UnoDeck` — Deste Yönetimi
- Standart 108 kartlık UNO destesi oluşturma
- Fisher-Yates shuffle algoritması ile karıştırma
- Çekme/atma işlemleri
- Deste bittiğinde otomatik yeniden karıştırma (discard pile → draw pile)
- Başlangıç kartı çekme (Wild/özel kart çıkarsa tekrar çeker)

#### `PlayerHand` — Oyuncu Eli
- Elde kart ekleme/çıkarma
- Ortadaki karta göre atılabilir kartları filtreleme
- **Güvenlik**: Gerçek kart bilgisi sadece backend'de saklanır; diğer oyunculara sadece kart **sayısı** gönderilir

#### `UnoGameState` — Oyun Durumu
`GameCore.BaseGameState`'den türer ve UNO'ya özel durumları ekler:

| Özellik | Tip | Açıklama |
|---------|-----|----------|
| `LastPlayedCard` | `UnoCard?` | Ortadaki son kart |
| `IsClockwise` | `bool` | Oyun yönü |
| `DrawPenalty` | `int` | Bekleyen çekme cezası |
| `PlayerCardCounts` | `Dictionary<string, int>` | Oyuncu kart sayıları |

---

### 2. Rules Layer — `Rules/`

#### `UnoGameRules`
`GameCore.IGameRules` interface'ini implemente eder. Kart oynama kuralları:

| # | Kural | Açıklama |
|---|-------|----------|
| 1 | Wild kartlar | Her zaman atılabilir |
| 2 | Önceki Wild | Seçilen renge uygun kart atılabilir |
| 3 | Renk eşleşme | Aynı renk atılabilir |
| 4 | Sayı eşleşme | Aynı numara atılabilir |
| 5 | Tip eşleşme | Aynı özel kart tipi atılabilir (Skip↔Skip vb.) |

---

### 3. Core Layer — `Core/UnoGame.cs`

Oyunun ana motoru. `GameCore.BaseGame`'den türer ve tüm iş mantığını yönetir.

#### Temel Aksiyonlar

| Metod | Açıklama |
|-------|----------|
| `PlayCard(playerId, card, chosenColor?)` | Kart atma — kural kontrolü, state güncelleme, efekt uygulama |
| `DrawCard(playerId)` | Kart çekme — penalty veya normal (turda 1 kez) |
| `PassTurn(playerId)` | Pas geçme — sadece kart çektikten sonra |
| `CallUno(playerId)` | UNO çağırma — 1 kart kaldığında |

#### Kart Efektleri

```
Skip        → Sonraki oyuncu atlanır
Reverse     → Oyun yönü değişir
DrawTwo     → +2 ceza, sıra cezalı oyuncuya geçer
WildDrawFour → Renk seçimi + +4 ceza
Wild        → Renk seçimi
```

---

### 4. Event System — `Events/`

Event-Driven Architecture ile oyun aksiyonları loosely-coupled bir şekilde dinlenir.

```mermaid
classDiagram
    class BaseGameAction {
        <<GameCore>>
        +ActionId: string
        +ActionType: GameActionType
        +PlayerId: string
        +Timestamp: DateTime
    }

    class BaseUnoEvent {
        <<abstract>>
        +GameId: string
        +Message: string
    }

    BaseGameAction <|-- BaseUnoEvent
    BaseUnoEvent <|-- UnoGameStartedEvent
    BaseUnoEvent <|-- CardPlayedEvent
    BaseUnoEvent <|-- CardDrawnEvent
    BaseUnoEvent <|-- TurnChangedEvent
    BaseUnoEvent <|-- UnoGameEndedEvent

    class CardPlayedEvent {
        +Card: UnoCard
        +RemainingCards: int
    }

    class CardDrawnEvent {
        +CardCount: int
        +TotalCards: int
    }
```

Tüm eventler `GameCore.BaseGameAction`'dan türer ve `GameEventDispatcher` aracılığıyla kayıtlı listener'lara iletilir:
- **`WebSocketEventListener`** → Tüm client'lara broadcast
- **`JsonRpcBackendService`** → Konsol loglaması
- **`ConsoleEventListener`** → Debug çıktıları

---

### 5. Backend Layer — `Backend/`

#### WebSocket Katmanı

| Sınıf | Sorumluluk |
|-------|------------|
| `WebSocketHandler` | Client → Server mesajlarını parse eder, oyun aksiyonlarını çağırır |
| `WebSocketEventListener` | Server → Client event broadcast (game.card_played, game.turn_changed vb.) |
| `WebSocketConnectionManager` | Bağlantı listesi, broadcast, unicast, thread-safety |

#### Protokol

Tüm mesajlar **JSON-RPC 2.0** formatındadır:

```json
// Client → Server (Request)
{
    "jsonrpc": "2.0",
    "method": "game.play_card",
    "params": {
        "card": { "color": "RED", "type": "NUMBER", "number": 7 }
    },
    "id": "1"
}

// Server → Client (Response)
{
    "jsonrpc": "2.0",
    "result": {
        "success": true,
        "message": "Kart atıldı: Red 7"
    },
    "id": "1"
}

// Server → All Clients (Event/Notification)
{
    "jsonrpc": "2.0",
    "method": "game.card_played",
    "params": {
        "player_id": "p1",
        "card": { "color": "RED", "type": "NUMBER", "number": 7 },
        "remaining_cards": 5,
        "game_state": { ... }
    },
    "id": "..."
}
```

---

## 📡 WebSocket API Referansı

### Bağlantı

```
ws://localhost:5000/ws?playerId={playerId}
```

Bağlantı kurulduğunda sunucu otomatik olarak `server.welcome` mesajı gönderir.

### Client → Server Komutları

| Method | Params | Açıklama |
|--------|--------|----------|
| `game.play_card` | `{ card: CardDto, chosen_color?: string }` | Kart at |
| `game.draw_card` | — | Kart çek |
| `game.pass_turn` | — | Pas geç (kart çektikten sonra) |
| `game.call_uno` | — | UNO! çağır |
| `game.get_hand` | — | Eldeki kartları al |
| `game.get_state` | — | Oyun durumunu al |

### `CardDto` Formatı

```json
{
    "color": "RED | BLUE | GREEN | YELLOW | WILD",
    "type": "NUMBER | SKIP | REVERSE | DRAW_TWO | WILD | WILD_DRAW_FOUR",
    "number": 0-9 | null
}
```

### Server → Client Event'leri

| Method | Açıklama | Tetiklenme Zamanı |
|--------|----------|-------------------|
| `server.welcome` | Hoş geldin mesajı | Bağlantı kurulduğunda |
| `game.started` | Oyun başladı | Oyun başlatıldığında |
| `game.card_played` | Kart atıldı | Her kart atıldığında |
| `game.card_drawn` | Kart çekildi | Her kart çekildiğinde |
| `game.turn_changed` | Tur değişti | Sıra değiştiğinde |
| `game.ended` | Oyun bitti | Oyun sona erdiğinde |

### Hata Kodları

| Kod | Açıklama |
|-----|----------|
| `-32700` | Geçersiz JSON |
| `-32601` | Bilinmeyen metod |
| `-32603` | Sunucu hatası |
| `-32000` | Oyun kuralı ihlali |

---

## 🌐 REST API Endpoints

| Endpoint | Method | Açıklama |
|----------|--------|----------|
| `/api/status` | `GET` | Sunucu durumu, bağlı oyuncular |
| `/api/state` | `GET` | Anlık oyun durumu (JSON) |

---

## 🃏 Oyun Kuralları

### Standart UNO Destesi (108 kart)

| Kart Tipi | Adet | Açıklama |
|-----------|------|----------|
| Sayı kartları (0) | 4 (her renkten 1) | Kırmızı, Mavi, Yeşil, Sarı |
| Sayı kartları (1–9) | 72 (her renkten 2'şer) | Kırmızı, Mavi, Yeşil, Sarı |
| Skip | 8 (her renkten 2) | Sonraki oyuncuyu atlar |
| Reverse | 8 (her renkten 2) | Yönü değiştirir |
| Draw Two (+2) | 8 (her renkten 2) | +2 kart çektirir |
| Wild | 4 | Renk seçme |
| Wild Draw Four (+4) | 4 | Renk seçme + 4 kart çektirir |

### Oyun Akışı

1. Her oyuncuya **7 kart** dağıtılır
2. Desteden bir **başlangıç kartı** açılır (mutlaka sayı kartı)
3. Sırayla oyuncular kart atar veya çeker
4. Kart çekildikten sonra: atılabiliyorsa atabilir veya pas geçebilir
5. Elindeki tüm kartları ilk bitiren oyuncu **kazanır**

### UNO! Mekanizması

- Elinde **1 kart** kalınca **UNO!** demelisin
- Sıra geçmeden önce UNO demezsen → **+2 ceza kartı**

### Draw Kartı Stacking

- `+2` üstüne `+2` veya `+4` atılabilir
- `+4` üstüne `+4` atılabilir
- Ceza birikir, çekme sırası gelen oyuncu toplam cezayı çeker

---

## 🖥 Test Arayüzü

Proje, WebSocket bağlantılarını test etmek için dahili bir web arayüzü içerir.

**Erişim**: `http://localhost:5000`

### Özellikler

- 🎮 Oyuncu seçimi (Ahmet, Mehmet, Ayşe)
- 🔌 WebSocket bağlantı yönetimi
- 🃏 Eldeki kartları görsel olarak görüntüleme (renk kodlu)
- ✅ Atılabilir kartları yeşil çerçeveyle vurgulama
- 🎨 Wild kartlar için renk seçici
- 📤 Kart atma, çekme, pas geçme, UNO çağırma
- 📝 JSON-RPC mesaj logları (gönderilen/alınan/event)
- 🔧 Custom JSON-RPC komutu gönderme

---

## 🔧 Geliştirme Notları

### SOLID Prensipleri

| Prensip | Uygulama |
|---------|----------|
| **Single Responsibility** | Her sınıfın tek bir sorumluluğu var (UnoCard → veri, UnoDeck → deste yönetimi, UnoGameRules → kurallar) |
| **Open/Closed** | `BaseGameState` genişletildi ama değiştirilmedi (`UnoGameState`) |
| **Liskov Substitution** | `UnoGame : BaseGame` — her yerde `BaseGame` yerine kullanılabilir |
| **Interface Segregation** | `IGameRules`, `IGameEventListener`, `IAsyncGameEventListener` — küçük, odaklı interface'ler |
| **Dependency Inversion** | Oyun motoru concrete class'lara değil, `IGameRules` interface'ine bağımlı |

### Güvenlik Kararları

- **Kart bilgisi güvenliği**: Gerçek kart verisi (`PlayerHand`) sadece backend'de tutulur. Diğer oyunculara sadece kart **sayısı** gönderilir. Oyuncunun kendi kartları sadece `game.get_hand` yanıtında ilgili oyuncuya iletilir.
- **Thread-safe bağlantı**: `ConcurrentDictionary` ile eşzamanlı bağlantı ekleme/çıkarma güvenliği sağlanır.

### GameCore Framework Kullanımı

Proje, aşağıdaki GameCore bileşenlerini kullanır:

| GameCore Bileşeni | Kullanım |
|-------------------|----------|
| `BaseGame` | Oyun yaşam döngüsü, oyuncu yönetimi |
| `BaseGameState` | State yönetimi (GameId, Status, Timestamps) |
| `BaseGameAction` | Event temel sınıfı |
| `TurnManager` | Tur yönetimi, yön değiştirme, oyuncu atlama |
| `GameEventDispatcher` | Event publish/subscribe |
| `IGameRules` | Kural implementasyonu için interface |
| `IPlayer` | Oyuncu modeli interface'i |
| `GameStatus` | Oyun durumu enum'ı (Waiting, InProgress, Completed) |
| `PlayerStatus` | Oyuncu durumu enum'ı (Active, Waiting, Eliminated) |
| `GameActionType` | Event tip enum'ı |

### Gelecek Geliştirmeler

- [ ] Dinamik oyuncu katılımı (lobby sistemi)
- [ ] Oyuncu zamanlayıcısı (timeout mekanizması)
- [ ] Çoklu oda desteği
- [ ] Kalıcı oyun durumu (veritabanı)
- [ ] Mobil client entegrasyonu
- [ ] Skor tablosu ve istatistikler
- [ ] Farklı kural modları (Hızlı oyun, Turnuva modu)

---

## 📄 Lisans

Bu proje MIT Lisansı ile lisanslanmıştır.

---

<div align="center">

**Geliştirici**: [Rexiusq](https://github.com/Rexiusq)

⭐ Projeyi beğendiyseniz yıldız vermeyi unutmayın!

</div>
