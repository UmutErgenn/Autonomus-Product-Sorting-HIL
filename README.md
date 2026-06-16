Autonomous Tomato Sorter: Hardware-in-the-Loop Digital Twin

This project is a closed-loop digital twin simulation developed from scratch to autonomously detect and sort green tomatoes on a moving conveyor belt. 
By integrating Unity 3D, Python (OpenCV), and an STM32 microcontroller, the system achieves an average sorting accuracy of 90%.

The project demonstrates real-time system integration, overcoming physical hardware limitations and tracking challenges through custom predictive algorithms and precise serial communication.

⚙️ System Architecture & Data Flow
Unity Camera ➔ UDP ➔ Python (OpenCV) ➔ UART ➔ STM32 MCU ➔ UART ➔ Python ➔ UDP ➔ Unity Actuator

🎥 Project Demo
Watch the system in action on https://youtu.be/ZK5LPJkE1wE
