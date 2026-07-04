using UnityEngine;


public class RandomRotator : MonoBehaviour
{
    [Header("Dönüş Hızı Limitleri")]
    public float minSpeed = 50f;
    public float maxSpeed = 150f;

    private Vector3 randomAxis;
    private float currentSpeed;

    void Start()
    {
        // Başlangıçta tamamen rastgele bir 3D dönüş ekseni belirle böylece her obje farklı bir yöne döner
        randomAxis = Random.onUnitSphere; 
        
        // Inspector'dan belirlediğin aralıkta rastgele bir hız seç
        currentSpeed = Random.Range(minSpeed, maxSpeed);
    }

    void Update()
    {
        // Objeyi kendi ekseni etrafında sürekli döndür
        transform.Rotate(randomAxis * currentSpeed * Time.deltaTime, Space.Self);
    }
}
