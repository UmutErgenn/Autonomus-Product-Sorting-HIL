using UnityEngine;
using TMPro;

public class AccuracyCounter : MonoBehaviour
{
    // Singleton yapısı: Diğer scriptlerin (domateslerin) bu merkeze doğrudan ulaşabilmesi için
    public static AccuracyCounter Instance; 

    [Header("Skorbord UI")]
    public TextMeshProUGUI accuracyText;

    // --- KARMAŞIKLIK MATRİSİ (CONFUSION MATRIX) VERİLERİ ---
    private int truePositive = 0;  // DOĞRU: Kutuya girmesi gereken kırmızı kutuya girdi
    private int trueNegative = 0;  // DOĞRU: Atılması gereken yeşil banttan atıldı
    private int falsePositive = 0; // HATA: Atılması gereken yeşil atılamadı, kutuya girdi
    private int falseNegative = 0; // HATA: Kutuya girmesi gereken kırmızı yanlışlıkla atıldı

    void Awake()
    {
        Instance = this;
    }

    // Domates sayım kutusuna (Trigger'a) çarptığında çalışır
    void OnTriggerEnter(Collider other)
    {
        TomatoData domates = other.GetComponent<TomatoData>();
        
        // Eğer çarpan şey domatesse ve daha önce sayılmadıysa
        if (domates != null && !domates.isProcessed)
        {
            domates.isProcessed = true; // Sayıldığını mühürle ki düşerken bir daha sayılmasın
            
            if (domates.isOlgun) 
                truePositive++;  // Kırmızı kutuya girdi
            else 
                falsePositive++; // Yeşil kutuya girdi (Hata)

            GuncelleUI();
        }
    }

    // Domates banttan aşağı atıldığında (kutuya girmeden düştüğünde) TomatoData tarafından tetiklenir
    public void BanttanAtildiRaporu(bool isOlgun)
    {
        if (isOlgun) 
            falseNegative++; // Kırmızı banttan atıldı (Hata)
        else 
            trueNegative++;  // Yeşil banttan atıldı (Doğru)

        GuncelleUI();
    }

    void GuncelleUI()
    {
        // Toplam işleme giren domates sayısı
        float islenenToplam = truePositive + trueNegative + falsePositive + falseNegative;
        
        if (islenenToplam > 0)
        {
            // GERÇEK ENDÜSTRİYEL DOĞRULUK: (Tüm Doğrular / Tüm Domatesler)
            float dogruKararlar = truePositive + trueNegative;
            float accuracy = (dogruKararlar / islenenToplam) * 100f;
            
            // Opsiyonel Ürün Saflığı (Kutudaki kırmızıların kutudaki tüm ürünlere oranı)
            float purity = 0;
            if ((truePositive + falsePositive) > 0)
                purity = ((float)truePositive / (truePositive + falsePositive)) * 100f;

            accuracyText.text = $"SİSTEM DOĞRULUĞU: %{accuracy:F1}\n" +
                                $"KUTU SAFLIĞI: %{purity:F1}\n\n" +
                                $"[BAŞARILI İŞLEMLER]\n" +
                                $"Kutuya Ulaşan Kırmızı: {truePositive}\n" +
                                $"Banttan Atılan Yeşil: {trueNegative}\n\n" +
                                $"[HATALAR]\n" +
                                $"Kaçırılan Yeşil: {falsePositive}\n" +
                                $"Yanlış Atılan Kırmızı: {falseNegative}";
        }
    }
}