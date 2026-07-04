Autonomous Tomato Sorter: Hardware-in-the-Loop Digital Twin

A closed-loop system that detects unripe (green) tomatoes on a moving conveyor and fires a physical mechanism to remove them, combining a Unity 3D simulation, a Python/OpenCV vision pipeline, and a real STM32 microcontroller.

The STM32 runs the real, unmodified firing-decision and timing logic on physical silicon. The conveyor, camera, and firing rods are simulated in Unity as a stand-in for real sensors and motors — this is a hardware-in-the-loop (HIL) setup, not a purely software simulation: everything downstream of "here is a detected tomato's position, speed, and color ratio" happens on real hardware, over a real USB link.

⚙️ System Architecture & Data Flow
➔ Unity (simulated camera view) ➔ UDP (JPEG frame) 
➔ Python — OpenCV vision pipeline ➔ USB (CDC Virtual COM Port) 
➔ STM32F4 — real-time firing decision + timing (physical MCU) ➔ USB (CDC Virtual COM Port) 
➔ Python — bridges MCU output back to simulation ➔ UDP 
➔ Unity (simulated actuator / firing rod)

🎥 Project Demo
Watch the system in action on https://youtu.be/ZK5LPJkE1wE
