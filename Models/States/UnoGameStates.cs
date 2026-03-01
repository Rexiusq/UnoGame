using GameCore.Models;  // ⭐ GAMECORE KULLANIMI 1: BaseGameState'i import ediyoruz
using UnoGame.Models.Cards;
using System.Collections.Generic;

namespace UnoGame.Models.States
{
    /// <summary>
    /// UNO oyununun anlık durumunu tutan sınıf
    /// 
    /// ⭐ GAMECORE KULLANIMI: BaseGameState'den türüyor!
    /// 
    /// BaseGameState bize ne sağlıyor?
    /// - GameId (oyun kimliği)
    /// - Status (Waiting, InProgress, Completed)
    /// - CreatedAt (oluşturulma zamanı)
    /// - UpdatedAt (son güncelleme zamanı)
    /// - ToJson() (JSON'a çevirme metodu)
    /// - MarkAsUpdated() (güncelleme zamanını kaydet)
    /// 
    /// Biz sadece UNO'ya özel bilgileri ekliyoruz!
    /// 
    /// SOLID Prensipi: Open/Closed
    /// - Genişlemeye açık: BaseGameState'i extend ettik
    /// - Değişime kapalı: BaseGameState'in kodunu değiştirmedik
    /// </summary>
    public class UnoGameState : BaseGameState  // ⭐ GameCore'dan türetme
    {
        /// <summary>
        /// Ortadaki son atılan kart
        /// null = Henüz oyun başlamadı veya ilk kart atılmadı
        /// </summary>
        public UnoCard? LastPlayedCard { get; set; }  // DÜZELTME: Tekrar tanımlandı

        /// <summary>
        /// Oyunun yönü
        /// true = Saat yönü (normal)
        /// false = Saat yönü tersi (Reverse kartı atıldığında)
        /// </summary>
        public bool IsClockwise { get; set; }

        /// <summary>
        /// Bir sonraki oyuncunun kaç kart çekeceği
        /// +2 veya +4 kartları için kullanılır
        /// </summary>
        public int DrawPenalty { get; set; }

        /// <summary>
        /// Her oyuncunun elindeki kart sayısı
        /// 
        /// Neden gerçek kartları saklamıyoruz?
        /// - Güvenlik: State JSON olarak client'lara gönderilir
        /// - Başka oyuncular birbirinin kartlarını görmemeli
        /// - Backend gerçek kartları ayrı tutar
        /// 
        /// Dictionary kullanımı:
        /// Key: PlayerId (örn: "p1")
        /// Value: Kart sayısı (örn: 5)
        /// </summary>
        public Dictionary<string, int> PlayerCardCounts { get; set; }

        /// <summary>
        /// Constructor - UnoGameState oluştururken çağrılır
        /// 
        /// base(gameId) ne demek?
        /// - BaseGameState'in constructor'ını çağırır
        /// - BaseGameState orada GameId, CreatedAt gibi değerleri set eder
        /// </summary>
        public UnoGameState(string gameId) : base(gameId)  // ⭐ GameCore constructor çağrısı
        {
            // UNO'ya özel başlangıç değerleri
            IsClockwise = true;
            DrawPenalty = 0;
            PlayerCardCounts = new Dictionary<string, int>();
        }
    }
}