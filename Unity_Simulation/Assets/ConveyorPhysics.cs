using UnityEngine;
using System.Collections.Generic;

public class ConveyorPhysics : MonoBehaviour
{
    public float beltSpeed = 2.0f; 
    public Vector3 direction = Vector3.forward; 
    
    private List<Rigidbody> onBelt = new List<Rigidbody>();

    void FixedUpdate()
    {
        foreach (Rigidbody rb in onBelt)
        {
            // Sadece yatay (X ve Z) ekseninde hız veriyoruz. 
            // Y eksenindeki (yerçekimi) hıza dokunmuyoruz ki zıplama/düşme bozulmasın.
            Vector3 currentVelocity = rb.linearVelocity;
            Vector3 targetVelocity = direction * beltSpeed;
            
            rb.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null && !onBelt.Contains(rb))
            onBelt.Add(rb);
    }

    void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null && onBelt.Contains(rb))
            onBelt.Remove(rb);
            
        // NOT: Nesne banttan (trigger'dan) çıktığında listeden silinir.
        // Ancak fizik motoru çalıştığı için, o anki rb.velocity değeri korunur 
        // ve nesne ileri doğru parabolik bir kavis çizerek düşmeye devam eder!
    }
}