using UnityEngine;

public class TomatoSpawner : MonoBehaviour
{
    [Header("Üretilecek Domatesler")]
    public GameObject[] tomatoPrefabs; // Kırmızı ve yeşil prefabları buraya koyacağız

    [Header("Üretim Ayarları")]
    public float spawnInterval = 1.5f; // Kaç saniyede bir domates düşsün
    public Vector3 spawnAreaSize = new Vector3(2f, 0f, 2f); // Ne kadar genişlikte bir alana dağılsın

    private float timer;

    void Update()
    {
        // Zamanlayıcıyı çalıştır
        timer += Time.deltaTime;

        // Süre dolduysa yeni domates üret ve sayacı sıfırla
        if (timer >= spawnInterval)
        {
            SpawnTomato();
            timer = 0f;
        }
    }

    void SpawnTomato()
    {
        // Listeden rastgele bir domates seç (Kırmızı veya Yeşil)
        int randomIndex = Random.Range(0, tomatoPrefabs.Length);
        GameObject selectedTomato = tomatoPrefabs[randomIndex];

        // Belirlediğimiz alanın içinde rastgele bir X, Y, Z koordinatı bul
        Vector3 randomOffset = new Vector3(
            Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
            Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2),
            Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
        );

        // Spawner'ın merkez noktasına bu rastgele sapmayı ekle
        Vector3 spawnPosition = transform.position + randomOffset;

        // Domatesi o noktada ve rastgele bir açıyla yarat
        Instantiate(selectedTomato, spawnPosition, Random.rotation);
    }

    // Geliştirici Kolaylığı: Unity Editor'de spawn alanını yeşil yarı saydam bir kutu olarak gösterir
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawCube(transform.position, spawnAreaSize);
    }
}
