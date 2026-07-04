using UnityEngine;

public class TomatoData : MonoBehaviour
{
    [Header("Domates Türü")]
    [Tooltip("Kırmızı (Olgun) domatesler için bu kutucuğu işaretle.")]
    public bool isOlgun; 

    // Doğruluk hesabında domatesin iki kere sayılmasını engelleyen güvenlik mührü
    [HideInInspector]
    public bool isProcessed = false;
    void Update()
    {
        // Domates Y ekseninde (aşağı doğru) -10 noktasının altına inerse kendini imha etsin
        if (transform.position.y < -10f)
        {
            // EĞER domates sayım kutusuna girmeden buraya kadar düştüyse, demek ki çubuk vurup atmıştır!
            if (!isProcessed && AccuracyCounter.Instance != null)
            {
                isProcessed = true;
                AccuracyCounter.Instance.BanttanAtildiRaporu(isOlgun);
            }

            Destroy(gameObject); // İstatistiği verdikten sonra RAM'i rahatlatmak için intihar et
        }
    }
}
