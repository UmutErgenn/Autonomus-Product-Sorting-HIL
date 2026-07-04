import cv2
import numpy as np
from mss import mss
import socket
import time
import sys
import math
import serial
import threading

# --- MCU SERİ PORT AYARLARI ---
try:
    mcu_serial = serial.Serial('COM6', 115200, timeout=0.01)
    print("MCU'ya başarıyla bağlanıldı!")
except Exception as e:
    print(f"COM Port Hatası: {e}")

# --- UDP SOKET AYARLARI ---
UDP_IP = "127.0.0.1" # localhost
UDP_PORT = 5065
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)


# --- GÖRÜNTÜ ALMA SOKETİ ---
RECEIVE_IP = "127.0.0.1"
RECEIVE_PORT = 5055
receive_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
receive_sock.bind((RECEIVE_IP, RECEIVE_PORT))


# --- KALİBRASYON DEĞERLERİ ---
BAND_SOL_X = 50
BAND_SAG_X = 450
CUBUKLAR_Y = 815
SENSOR_Y = 680
SENSOR_Y2 = 480
HASSASIYET = 15
BANT_HIZI = 160
MEKANIK_GECIKME = 0.1
CUBUK_SAYISI = 15
bekleyen_domatesler = [] 
MAKS_KAYMA = 5
X_OFFSET = 2.5

# --- NESNE TAKİP DEĞİŞKENLERİ ---
siradaki_id = 1
takip_edilen_domatesler = {}
MAKS_MESAFE = 45
MAKS_KAYIP_FRAME = 30

def dairesel_mi(contour, min_dairesellik=0.55):
    hull = cv2.convexHull(contour)
    alan = cv2.contourArea(hull)
    
    if alan < 30: 
        return False
        
    (x, y), radius = cv2.minEnclosingCircle(hull)
    cember_alani = math.pi * (radius ** 2)
    
    if cember_alani == 0:
        return False
        
    dairesellik = alan / cember_alani
    
    if dairesellik > min_dairesellik:
        return True
    else:
        return False

# --- MCU DİNLEME ---
def mcu_dinle():
    while True:
        try:
            if mcu_serial.in_waiting > 0:
                gelen_mesaj = mcu_serial.readline().decode('utf-8').strip()
                if gelen_mesaj.startswith("VUR"):
                    sock.sendto(gelen_mesaj.encode('utf-8'), (UDP_IP, UDP_PORT))
                    print(f"*** MCU'DAN GELDİ, UNITY'YE İLETİLDİ: {gelen_mesaj} ***")
        except:
            pass

dinleme_thread = threading.Thread(target=mcu_dinle, daemon=True)
dinleme_thread.start()

sct = mss()

monitor = {"top": 0, "left": 0, "width": 960, "height": 1080}

print("Canlı takip başladı")




while True:
    try:
        data, addr = receive_sock.recvfrom(65536)# recvfrom=görüntü gelene kadar bekle. 65536 byte - UDP sınırı
        np_data = np.frombuffer(data, dtype=np.uint8)
        frame = cv2.imdecode(np_data, cv2.IMREAD_COLOR)
        
        if frame is None:
            continue
    except Exception as e:
        print(f"Görüntü alınırken hata: {e}")
        continue
    
    hsv_tum_ekran = cv2.cvtColor(frame, cv2.COLOR_BGR2HSV)
    _, _, v_kanali = cv2.split(hsv_tum_ekran)
    blurred = cv2.GaussianBlur(v_kanali, (7, 7), 0)
    _, thresh = cv2.threshold(blurred, 40, 255, cv2.THRESH_BINARY)
    
    # --- AYRIŞTIRMA MİMARİSİ ---
    kernel_oval = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (7, 7))
    
    thresh = cv2.morphologyEx(thresh, cv2.MORPH_CLOSE, kernel_oval, iterations=2)
    
    # --- WATERSHED ---
    sure_bg = cv2.dilate(thresh, kernel_oval, iterations=3)

    dist_transform = cv2.distanceTransform(thresh, cv2.DIST_L2, 5)
    _, sure_fg = cv2.threshold(dist_transform, 14, 255, cv2.THRESH_BINARY)
    sure_fg = np.uint8(sure_fg)

    unknown = cv2.subtract(sure_bg, sure_fg)

    _, markers = cv2.connectedComponents(sure_fg)
    markers = markers + 1
    markers[unknown == 255] = 0

    markers = cv2.watershed(frame, markers)

    thresh = np.zeros_like(v_kanali)
    thresh[markers > 1] = 255
    
    contours, _ = cv2.findContours(thresh, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)


    # Filtreleme ve Dairesellik Testi
    NORMAL_ALAN = 800
    MAKS_TEK_ALAN = 1300

    # ZORLA ÇEMBER ARAMA
    anlik_merkezler = []
    
    for cnt in contours:
        x, y, w, h = cv2.boundingRect(cnt)
        alan = cv2.contourArea(cnt)
        merkez_x = int(x + (w / 2))
        merkez_y = int(y + (h / 2))
        
        if merkez_x < BAND_SOL_X or merkez_x > BAND_SAG_X:
            continue
            
        if alan < 50:
            continue
            
        # tekil domates
        elif alan < MAKS_TEK_ALAN:
            if dairesel_mi(cnt, min_dairesellik=0.55):
                anlik_merkezler.append((merkez_x, merkez_y, x, y, w, h))
                
        # bitişik domates 
        else:
            tahmini_sayi = max(2, round(alan / NORMAL_ALAN))
            roi_mask = thresh[y:y+h, x:x+w]
            
            roi_blur = cv2.GaussianBlur(roi_mask, (9, 9), 2)
            
            # kutu içinde zorla çember arama
            circles = cv2.HoughCircles(roi_blur, cv2.HOUGH_GRADIENT, dp=1, minDist=20,
                                       param1=50, param2=15, minRadius=10, maxRadius=30)
            
            if circles is not None:
                circles = np.round(circles[0, :]).astype("int")
                
                for (cx, cy, r) in circles[:tahmini_sayi]:
                    
                    gercek_cx = x + cx
                    gercek_cy = y + cy
                    
                    anlik_merkezler.append((gercek_cx, gercek_cy, gercek_cx-r, gercek_cy-r, r*2, r*2))
            
    # --- MERKEZ TAKİBİ EŞLEŞTİRMESİ ---
    guncellenen_idler = []
    MIN_YESIL_ORANI = 0.95 # %90 yeşil oyu HAM
    
    for (m_x, m_y, x, y, w, h) in anlik_merkezler:
        eslesen_id = None
        en_kisa_mesafe = 9999
        
        for domates_id, veri in takip_edilen_domatesler.items():
            eski_x, eski_y_merkez = veri['merkez']
            kayip = veri['kayip_frame']
            
            # TAHMİNCİ TAKİP
            tahmini_y = eski_y_merkez + (kayip * 5)
            
            mesafe = math.hypot(m_x - eski_x, m_y - tahmini_y)
            
            dinamik_maks_mesafe = MAKS_MESAFE + (kayip * 10)
            
            if mesafe < dinamik_maks_mesafe and mesafe < en_kisa_mesafe:
                en_kisa_mesafe = mesafe
                eslesen_id = domates_id
                
        if eslesen_id is not None:
            hedef_id = eslesen_id
            takip_edilen_domatesler[hedef_id]['eski_y'] = takip_edilen_domatesler[hedef_id]['merkez'][1]
            takip_edilen_domatesler[hedef_id]['merkez'] = (m_x, m_y)
            takip_edilen_domatesler[hedef_id]['kayip_frame'] = 0
        else:
            hedef_id = siradaki_id
            takip_edilen_domatesler[hedef_id] = {
                'merkez': (m_x, m_y),
                'eski_y': m_y - 1, 
                'kayip_frame': 0,
                'yesil_oy': 0,
                'toplam_oy': 0,
                'zaman_s1': 0.0,
                'karar_verildi': False 
            }
            siradaki_id += 1
            
        guncellenen_idler.append(hedef_id)
        
        # --- RENK OYLAMASI ---
        roi = frame[y:y+h, x:x+w]
        if roi.shape[0] > 0 and roi.shape[1] > 0:
            hsv_roi = cv2.cvtColor(roi, cv2.COLOR_BGR2HSV)
            lower_green = np.array([30, 30, 20])
            upper_green = np.array([90, 255, 255])
            green_mask = cv2.inRange(hsv_roi, lower_green, upper_green)
            
            if cv2.countNonZero(green_mask) > 15: 
                takip_edilen_domatesler[hedef_id]['yesil_oy'] += 1
            takip_edilen_domatesler[hedef_id]['toplam_oy'] += 1
            
            cv2.rectangle(frame, (x, y), (x + w, y + h), (255, 255, 255), 2)
            cv2.putText(frame, f"ID:{hedef_id} Y:{takip_edilen_domatesler[hedef_id]['yesil_oy']}", 
                        (x, y - 5), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 2)

        # --- HIZ HESABI VE KARAR ---
        if not takip_edilen_domatesler[hedef_id]['karar_verildi']:
            
            su_anki_y = m_y
            eski_y = takip_edilen_domatesler[hedef_id]['eski_y']
            
            if eski_y <= SENSOR_Y2 and su_anki_y > SENSOR_Y2:
                if takip_edilen_domatesler[hedef_id]['zaman_s1'] == 0.0:
                    takip_edilen_domatesler[hedef_id]['zaman_s1'] = time.time()
                    
            elif eski_y <= SENSOR_Y and su_anki_y > SENSOR_Y:
                zaman_s1 = takip_edilen_domatesler[hedef_id]['zaman_s1']
                
                if zaman_s1 > 0.0:
                    gecen_zaman = time.time() - zaman_s1
                    
                    if gecen_zaman > 0.05: 
                        # Dinamik Hızı Hesapla
                        mesafe_1_2 = SENSOR_Y - SENSOR_Y2 
                        anlik_hiz = mesafe_1_2 / gecen_zaman
                        
                        yesil_oy = takip_edilen_domatesler[hedef_id]['yesil_oy']
                        toplam_oy = max(1, takip_edilen_domatesler[hedef_id]['toplam_oy'])
                        oy_orani = yesil_oy / toplam_oy
                        
                        if oy_orani > MIN_YESIL_ORANI:
                            paket = f"D,{int(m_x)},{int(anlik_hiz)},{int(oy_orani * 100)}\n"
                            mcu_serial.write(paket.encode('utf-8'))
                            
                        takip_edilen_domatesler[hedef_id]['karar_verildi'] = True
        
            
    # --- kaybolanları temizle ---
    silinecek_idler = []
    for domates_id in takip_edilen_domatesler.keys():
        if domates_id not in guncellenen_idler:
            takip_edilen_domatesler[domates_id]['kayip_frame'] += 1
            if takip_edilen_domatesler[domates_id]['kayip_frame'] > MAKS_KAYIP_FRAME:
                silinecek_idler.append(domates_id)
                
    for domates_id in silinecek_idler:
        del takip_edilen_domatesler[domates_id]
    
    
    
    
    
    # Kalibrrasyon Çizgileri
    
    # sanal sensör çizgisi mavi
#    cv2.line(frame, (0, SENSOR_Y), (960, SENSOR_Y), (255, 0, 0), 2)
#    cv2.putText(frame, "SENSOR", (10, SENSOR_Y - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 0, 0), 2)
    
    # sanal sensör çizgisi2 
#    cv2.line(frame, (0, SENSOR_Y2), (960, SENSOR_Y2), (0, 0, 0), 2)
#    cv2.putText(frame, "SENSOR2", (10, SENSOR_Y2 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 0, 0), 2)

    # çubukların çizgisi sarı
#    cv2.line(frame, (0, CUBUKLAR_Y), (960, CUBUKLAR_Y), (0, 255, 255), 2)
#    cv2.putText(frame, "CUBUKLAR", (10, CUBUKLAR_Y - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 255), 2)

    # sol ve sağ bant sınırları yeşil
#    cv2.line(frame, (BAND_SOL_X, 0), (BAND_SOL_X, 1080), (0, 255, 0), 2)
#    cv2.line(frame, (BAND_SAG_X, 0), (BAND_SAG_X, 1080), (0, 255, 0), 2)
    
    
    cv2.imshow("Unity Canli Takip", frame)
    
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cv2.destroyAllWindows()
cv2.waitKey(1)

sct.close()
sys.exit()