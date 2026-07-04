using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    [Header("Ayarlar")]
    public float speed = 0.5f;
    public bool yataydaDonsun = false;

    private Renderer rend;
    private float currentOffset = 0f;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        // Zamanla offset değerini biriktiriyoruz
        currentOffset += speed * Time.deltaTime;

        // Yönü belirle X mi Y mi?
        Vector2 finalOffset = yataydaDonsun ? new Vector2(currentOffset, 0) : new Vector2(0, currentOffset);

        // Hem URP hem de Standart shader isimlerini kontrol ederek uygula
        if (rend != null && rend.material != null)
        {
            if (rend.material.HasProperty("_BaseMap")) // URP
                rend.material.SetTextureOffset("_BaseMap", finalOffset);
            else if (rend.material.HasProperty("_MainTex")) // Standard
                rend.material.SetTextureOffset("_MainTex", finalOffset);
        }
    }
}