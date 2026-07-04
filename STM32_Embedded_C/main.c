/* USER CODE BEGIN Header */
/**
  ******************************************************************************
  * @file           : main.c
  * @brief          : Main program body
  ******************************************************************************
  * @attention
  *
  * Copyright (c) 2026 STMicroelectronics.
  * All rights reserved.
  *
  * This software is licensed under terms that can be found in the LICENSE file
  * in the root directory of this software component.
  * If no LICENSE file comes with this software, it is provided AS-IS.
  *
  ******************************************************************************
  */
/* USER CODE END Header */
/* Includes ------------------------------------------------------------------*/
#include "main.h"
#include "usb_device.h"

/* Private includes ----------------------------------------------------------*/
/* USER CODE BEGIN Includes */
#include <string.h>
#include <sys/_intsup.h>
#include "usbd_cdc_if.h"
#include <stdio.h>
/* USER CODE END Includes */

/* Private typedef -----------------------------------------------------------*/
/* USER CODE BEGIN PTD */
#define data_length 64  // gelen veri paketi max 64 byte
#define queue_size 10 // kuyrukta max 10 veri bekleyebilir

typedef struct {
    char buffer[queue_size][data_length]; // 10 adet 64 karakterlik buffer
    volatile uint8_t head;
    volatile uint8_t tail;
    volatile uint8_t count;
    // volatile kesme sonrası ramdeki değerin güncellenmesini sağlar,
    // böylece kesme sırasında buffer'a veri yazılırken 
    // ana döngüdeki okuma işlemi güncel veriyi okuyabilir.
} RingBuffer; // dairesel kuyruk

typedef struct {
    uint32_t target_time; // hesaplanan vurulma anı
    int rod_no;         // Hangi çubuk vuracak
    uint8_t active;        // 1 ise bu hedef bekleniyor, 0 ise boş
} Target_t; // vurulacak hedeflerin bilgileri

// Çift ateşleme kapasite arttırımı
#define MAX_TARGETS 20
Target_t firing_list[MAX_TARGETS] = {0};


/* USER CODE END PTD */

/* Private define ------------------------------------------------------------*/
/* USER CODE BEGIN PD */
#define BAND_SOL_X 50
#define BAND_SAG_X 450
#define CUBUK_SAYISI 15
#define CUBUKLAR_Y 780//815
#define SENSOR_Y2 480
#define SENSOR_Y 680
#define MEKANIK_GECIKME 0.1f
#define MIN_YESIL_ORANI 0.90f
#define X_OFFSET 2.5
/* USER CODE END PD */

/* Private macro -------------------------------------------------------------*/
/* USER CODE BEGIN PM */

/* USER CODE END PM */

/* Private variables ---------------------------------------------------------*/

/* USER CODE BEGIN PV */
RingBuffer message_queue = {0};
/* USER CODE END PV */

/* Private function prototypes -----------------------------------------------*/
void SystemClock_Config(void);
static void MX_GPIO_Init(void);
/* USER CODE BEGIN PFP */

/* USER CODE END PFP */

/* Private user code ---------------------------------------------------------*/
/* USER CODE BEGIN 0 */

/* USER CODE END 0 */

/**
  * @brief  The application entry point.
  * @retval int
  */
int main(void)
{

  /* USER CODE BEGIN 1 */

  /* USER CODE END 1 */

  /* MCU Configuration--------------------------------------------------------*/

  /* Reset of all peripherals, Initializes the Flash interface and the Systick. */
  HAL_Init(); // stm32 donanımını başlatır

  /* USER CODE BEGIN Init */

  /* USER CODE END Init */

  /* Configure the system clock */
  SystemClock_Config(); // işlemci saat hızını ayarlar

  /* USER CODE BEGIN SysInit */

  /* USER CODE END SysInit */

  /* Initialize all configured peripherals */
  MX_GPIO_Init(); // pinleri giriş çıkışa ayarlar
  MX_USB_DEVICE_Init(); // USB haberleşmesini başlatır
  /* USER CODE BEGIN 2 */
      //  cihaz ilk çalıştığında bir kere çalışacak kodlar buraya yazılır.
  HAL_Delay(2000); // Bilgisayarın COM portunu tanıması için bekle
  
  /* USER CODE END 2 */

  /* Infinite loop */
  /* USER CODE BEGIN WHILE */
  while (1)
  {
    // ==========================================
    // KUYRUKTAN YENİ DOMATES OKUMA
    // ==========================================

    //Gelen verileri oku, hedef zamanı hesapla ve firing_list'e ekle
    //Kuyrukta eleman varsa kuyruğun sonundan veriyi al, message değişkenine kopyala. tail bir artırıp dairesel yapıyı koru.
    if (message_queue.count > 0) {
      char message[data_length];  //kuyruktaki mesajı tutacak geçici buffer
      strncpy(message, message_queue.buffer[message_queue.tail], data_length);
      message_queue.tail = (message_queue.tail + 1) % queue_size;
        
      // kesmeleri kapat, eleman sayısını güncelle, kesmeleleri tekrar aç
      __disable_irq(); 
      message_queue.count--;
      __enable_irq(); 

      // gelen mesajı parçala, tam sayı formatından floata dön
      int x_koord, speed_int, green_ratio_int;
      sscanf(message, "D,%d,%d,%d", &x_koord, &speed_int, &green_ratio_int); 
      float speed = (float)speed_int;
      float green_ratio = (float)green_ratio_int / 100.0f;  

      if (green_ratio > MIN_YESIL_ORANI) {
        if (x_koord < BAND_SOL_X || x_koord > BAND_SAG_X)
          continue; // koordinatlar bant sınırları dışındaysa işlemi atla
        float band_width = BAND_SAG_X - BAND_SOL_X;
        float area_width = band_width / CUBUK_SAYISI;
        float relative_x = x_koord - BAND_SOL_X + X_OFFSET; // bantın solundan itibaren x koordinatını hesapla
        // --- ÇİFT ATEŞLEME ---
        float exact_rod = relative_x / area_width;
        int primary_rod = (int)exact_rod + 1;
        float fractional_part = exact_rod - (int)exact_rod; // 0.0 ile 0.99 arası
        
        if (primary_rod < 1) primary_rod = 1;
        if (primary_rod > CUBUK_SAYISI) primary_rod = CUBUK_SAYISI;

        int rods_to_fire[2] = {primary_rod, 0};
        int num_rods = 1;
        
        // Domates hangi çubuğa yakınsa ona göre çift ateşleme
        if (fractional_part > 0.75f && primary_rod < CUBUK_SAYISI) {
            rods_to_fire[1] = primary_rod + 1;
            num_rods = 2;
        } else if (fractional_part < 0.25f && primary_rod > 1) {
            rods_to_fire[1] = primary_rod - 1;
            num_rods = 2;
        }

        float length = CUBUKLAR_Y - SENSOR_Y; // domatesin kat edeceği mesafe

        // ms cinsinden varış süresini hesapla, ateşleme için gereken bekleme süresini bul, ateşleme listesine ekle
        if(speed > 0) {
          float arrival_time = length / speed;
          float firing_time = arrival_time - MEKANIK_GECIKME;

          if (firing_time > 0.0f) {
            uint32_t current_time = HAL_GetTick();
            uint32_t moment_to_hit = current_time + (uint32_t)(firing_time * 1000);

            // Çift ateşleme varsa her iki çubuğu da listeye ekle
            for (int r = 0; r < num_rods; r++) {
                for (int i = 0; i < MAX_TARGETS; i++) {
                  if (firing_list[i].active == 0) {
                    firing_list[i].target_time = moment_to_hit;
                    firing_list[i].rod_no = rods_to_fire[r];
                    firing_list[i].active = 1;
                    break; 
                  }
                }
            }
          }
        }
      }
    }
    
    // ==========================================
    // VAKTİ GELEN DOMATESLERİ VURMA
    // ==========================================
    uint32_t now = HAL_GetTick(); // her döngüde güncel zamanı al, 

    for (int i = 0; i < MAX_TARGETS; i++) {
      if (firing_list[i].active == 1 && now >= firing_list[i].target_time) { // vurulma zamanı gelmiş hedefleri kontrol et
        char command[32]; // komut göndermek için buffer
        sprintf(command, "VUR,%d\r\n", firing_list[i].rod_no); // "VUR,rod_no" formatında komut hazırla
        CDC_Transmit_FS((uint8_t*)command, strlen(command)); // CDC üzerinden "VUR,rod_no" komutunu gönder
        firing_list[i].active = 0;  // listedeki bu görevi boşalt
      }
    }

    /* USER CODE END WHILE */

    /* USER CODE BEGIN 3 */
  }
  /* USER CODE END 3 */
}

/**
  * @brief System Clock Configuration
  * @retval None
  */
void SystemClock_Config(void)
{
  RCC_OscInitTypeDef RCC_OscInitStruct = {0};
  RCC_ClkInitTypeDef RCC_ClkInitStruct = {0};

  /** Configure the main internal regulator output voltage
  */
  __HAL_RCC_PWR_CLK_ENABLE();
  __HAL_PWR_VOLTAGESCALING_CONFIG(PWR_REGULATOR_VOLTAGE_SCALE1);

  /** Initializes the RCC Oscillators according to the specified parameters
  * in the RCC_OscInitTypeDef structure.
  */
  RCC_OscInitStruct.OscillatorType = RCC_OSCILLATORTYPE_HSE;
  RCC_OscInitStruct.HSEState = RCC_HSE_ON;
  RCC_OscInitStruct.PLL.PLLState = RCC_PLL_ON;
  RCC_OscInitStruct.PLL.PLLSource = RCC_PLLSOURCE_HSE;
  RCC_OscInitStruct.PLL.PLLM = 25;
  RCC_OscInitStruct.PLL.PLLN = 192;
  RCC_OscInitStruct.PLL.PLLP = RCC_PLLP_DIV2;
  RCC_OscInitStruct.PLL.PLLQ = 4;
  if (HAL_RCC_OscConfig(&RCC_OscInitStruct) != HAL_OK)
  {
    Error_Handler();
  }

  /** Initializes the CPU, AHB and APB buses clocks
  */
  RCC_ClkInitStruct.ClockType = RCC_CLOCKTYPE_HCLK|RCC_CLOCKTYPE_SYSCLK
                              |RCC_CLOCKTYPE_PCLK1|RCC_CLOCKTYPE_PCLK2;
  RCC_ClkInitStruct.SYSCLKSource = RCC_SYSCLKSOURCE_PLLCLK;
  RCC_ClkInitStruct.AHBCLKDivider = RCC_SYSCLK_DIV1;
  RCC_ClkInitStruct.APB1CLKDivider = RCC_HCLK_DIV2;
  RCC_ClkInitStruct.APB2CLKDivider = RCC_HCLK_DIV1;

  if (HAL_RCC_ClockConfig(&RCC_ClkInitStruct, FLASH_LATENCY_3) != HAL_OK)
  {
    Error_Handler();
  }
}

/**
  * @brief GPIO Initialization Function
  * @param None
  * @retval None
  */
static void MX_GPIO_Init(void)
{
  /* USER CODE BEGIN MX_GPIO_Init_1 */

  /* USER CODE END MX_GPIO_Init_1 */

  /* GPIO Ports Clock Enable */
  __HAL_RCC_GPIOH_CLK_ENABLE();
  __HAL_RCC_GPIOA_CLK_ENABLE();

  /* USER CODE BEGIN MX_GPIO_Init_2 */

  /* USER CODE END MX_GPIO_Init_2 */
}

/* USER CODE BEGIN 4 */

/* USER CODE END 4 */

/**
  * @brief  This function is executed in case of error occurrence.
  * @retval None
  */
void Error_Handler(void)
{
  /* USER CODE BEGIN Error_Handler_Debug */
  /* User can add his own implementation to report the HAL error return state */
  __disable_irq();
  while (1)
  {
  }
  /* USER CODE END Error_Handler_Debug */
}
#ifdef USE_FULL_ASSERT
/**
  * @brief  Reports the name of the source file and the source line number
  *         where the assert_param error has occurred.
  * @param  file: pointer to the source file name
  * @param  line: assert_param error line source number
  * @retval None
  */
void assert_failed(uint8_t *file, uint32_t line)
{
  /* USER CODE BEGIN 6 */
  /* User can add his own implementation to report the file name and line number,
     ex: printf("Wrong parameters value: file %s on line %d\r\n", file, line) */
  /* USER CODE END 6 */
}
#endif /* USE_FULL_ASSERT */
