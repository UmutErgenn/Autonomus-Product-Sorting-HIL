using UnityEngine;
using System.Collections;

public class RodController : MonoBehaviour
{
    [Header("Kontrol Ayarları")]
    public KeyCode activationKey = KeyCode.Alpha1; // Hangi tuşla çalışacak
    
    [Header("Mekanik Ayarlar")]
    public float hitAngle = 60f; // Çubuğun vurma açısı
    public float hitDuration = 0.1f; // Çubuğun ileride kalma süresi

    private HingeJoint hinge;
    private JointSpring hingeSpring;

    void Start()
    {
        // Başlangıçta objenin üzerindeki Hinge Joint'i ve yay ayarlarını hafızaya al
        hinge = GetComponent<HingeJoint>();
        hingeSpring = hinge.spring; 
    }

    void Update()
    {
        // Belirlenen tuşa basıldığında vurma işlemini başlat
        if (Input.GetKeyDown(activationKey))
        {
            Debug.Log(activationKey.ToString() + " tuşuna basıldı ve komut tetiklendi!");
            StartCoroutine(HitRoutine());
        }
    }

    // Vurma işlemini simüle eden zamanlı fonksiyon
    IEnumerator HitRoutine()
    {
        //Çubuğu fırlat (Hedef açıyı değiştir)
        hingeSpring.targetPosition = hitAngle;
        hinge.spring = hingeSpring;

        //Çubuğun açık konumda kalacağı mekanik süreyi bekle
        yield return new WaitForSeconds(hitDuration);

        //Çubuğu eski yerine (0 derecesine) geri çek
        hingeSpring.targetPosition = 0f;
        hinge.spring = hingeSpring;
    }

    // UDP alıcısının çağıracağı dışa açık fonksiyon
    public void VurmayiBaslat(float beklemeSuresi)
    {
        StartCoroutine(GecikmeliVurus(beklemeSuresi));
    }

    // Bekleme süresi bittiğinde vurma işlemini tetikleyen sayaç
    IEnumerator GecikmeliVurus(float beklemeSuresi)
    {
        // Python'un hesapladığı süre kadar bekle
        yield return new WaitForSeconds(beklemeSuresi); 
        
        // Asıl vurma animasyonunu başlat
        StartCoroutine(HitRoutine());
    }
}
