# No Freeze Ice Cream Maker

# Authors

Arman Mukhi & Julianna Graham 

Mechanical Engineering – Mechatronics

University of British Columbia

# Overview
This is our MECH 423 project that aims to mimic the Breville Smart Scoop Ice Cream Maker. The project implements closed-loop control using embedded firmware running on a microcontroller and a PC-based C# application for communication, visualization, and logging.

The firmware handles real-time control (sensing & actuation), while the C# application provides a user interface for command input, monitoring, and data recording.

The goal is to design and implement a stable, responsive control system that integrates hardware, firmware, and software into a cohesive architecture.

# Technologies Used
Firmware
  
  - Language: C

  - MCU: MSP430FR5739

PC Application

  - Language: C#

  - Framework: .NET (WinForms)

Communication

  - Serial (UART)

# How to Build and Run
Firmware

  1. Open the firmware project in your IDE.
  
  2. Compile and flash to the microcontroller.
  
  3. Verify serial communication is active.

C# Application

  1. Open the solution file in csharp/src.
  
  2. Build the solution.
  
  3. Run the application.
  
  4. Select the appropriate COM port and connect.

# Equipment Needed

Electronics
  1. MCU
  2. Thermistors x 2
  3. Stepper Motor
  4. Breadboard
  5. Jumper Wires

 Hardware
  1. Small Bowl
  2. Large Bowl x 2
  3. Mixing Paddle
  4. 3D Printed Parts
  5. Towel
