using UnityEngine;
using System.Net.Sockets;
using System.Net;

public class ImageStreamer : MonoBehaviour
{
    [Header("Yayın Ayarları")]
    public RenderTexture renderTexture; // Oluşturduğumuz KameraRender'ı buraya sürükle
    public string pythonIP = "127.0.0.1"; // Localhost
    public int port = 5055; // Görüntünün gideceği port
    
    [Range(10, 100)]
    public int kalite = 30; // JPG sıkıştırma kalitesi (düşük = daha hızlı)

    private UdpClient udpClient;
    private Texture2D texture2D;

    void Start()
    {
        udpClient = new UdpClient();
        // RenderTexture ile aynı boyutta boş bir doku (Texture) yaratıyoruz
        texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
    }

    // Her frame çizildikten hemen sonra çalışır
    void LateUpdate()
    {
        if (renderTexture == null) return;

        // 1. Kameranın çizdiği o anki belleği aktif et
        RenderTexture.active = renderTexture;

        // 2. Pikselleri okuyup oluşturduğumuz Texture2D içine kopyala
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        // 3. Pikselleri hafif bir JPG dosyasına çevir (Byte dizisi)
        byte[] imageBytes = texture2D.EncodeToJPG(kalite);

        // 4. Veriyi UDP üzerinden Python'a fırlat!
        udpClient.Send(imageBytes, imageBytes.Length, pythonIP, port);

        // Bellek sızıntısını önlemek için aktifliği sıfırla
        RenderTexture.active = null;
    }

    void OnApplicationQuit()
    {
        if (udpClient != null) udpClient.Close();
    }
}