using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

public class UdpReceiver : MonoBehaviour
{
    [Header("15 Çubuğu Buraya Sürükle")]
    public RodController[] cubuklar; 

    private UdpClient udpClient;
    private Thread receiveThread;
    
    // Arka plan iş parçacığı (Thread) ile Unity ana yapısı arasında güvenli veri kuyruğu
    private ConcurrentQueue<string> veriKuyrugu = new ConcurrentQueue<string>();

    void Start()
    {
        // UDP dinlemeyi arka planda başlat, böylece oyun kasmaz
        receiveThread = new Thread(new ThreadStart(VeriDinle));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log("UDP Port 5065 Dinleniyor...");
    }

    private void VeriDinle()
    {
        udpClient = new UdpClient(5065);
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            try
            {
                // Python'dan veri geldiğinde yakala
                byte[] data = udpClient.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                
                // Gelen metni ("3,1.45") kuyruğa ekle
                veriKuyrugu.Enqueue(text);
            }
            catch (System.Exception err) { print(err.ToString()); }
        }
    }

    void Update()
    {
        // Kuyrukta işlenmeyi bekleyen komut var mı kontrol et
        while (veriKuyrugu.TryDequeue(out string komut))
        {
            // MCU'dan gelen komutu parçala. Örnek: "VUR,14"
            string[] parcalar = komut.Split(',');
            
            // Eğer iki parça varsa ve ilk parça "VUR" kelimesiyse
            if (parcalar.Length == 2 && parcalar[0] == "VUR")
            {
                // İkinci parçadaki çubuk numarasını (Örn: 14) al
                if (int.TryParse(parcalar[1], out int cubukNo))
                {
                    // 1-15 arası numarayı 0-14 dizi indeksine çevir
                    int indeks = cubukNo - 1; 
                    
                    if (indeks >= 0 && indeks < cubuklar.Length)
                    {
                        // MCU süreyi bizim için zaten beklediği ve komutu tam anında 
                        // gönderdiği için, Unity'de bekleme süresini SIFIR (0f) yapıyoruz!
                        cubuklar[indeks].VurmayiBaslat(0f);
                    }
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        // Oyun kapandığında portu serbest bırak
        if (udpClient != null) udpClient.Close();
        if (receiveThread != null) receiveThread.Abort();
    }
}