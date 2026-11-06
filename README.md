# REIS-105M1 Digital Pulse Reflectometer — Interactive Overview & Feature Demonstration

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20WPF-0078D6?logo=windows&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
[![Tests](https://img.shields.io/badge/Tests-xUnit-blue)](https://xunit.net/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

An interactive visual demonstration application showcasing the architecture, operational capabilities, front-panel controls, authentic menu systems, and measurement principles of the portable pulse reflectometer (Time-Domain Reflectometer / TDR) **REIS-105M1**, built on **.NET 8 (WPF)**.

---

## 📌 Repository Overview & Keywords

> **Short Description (GitHub About):**
> *Interactive software overview and visual demonstration of the REIS-105M1 pulse reflectometer (TDR), illustrating its operational modes, front-panel controls, 128x64 LCD telemetry, and cable reflectometry principles.*

### 🏷️ Topic Tags & Keywords
`reis-105m1` `pulse-reflectometer` `tdr-overview` `feature-demonstration` `cable-fault-locator` `time-domain-reflectometry` `dotnet8` `wpf` `csharp12` `lcd-emulator` `writeablebitmap` `sound-effects` `interactive-ui`

### 🎯 Scope & Objectives
- **Device Capabilities Overview:** Demonstrates the functional features, measuring ranges ($12.5\,\text{m}$ to $25\,\text{km}$), velocity factor ($VOP$ / $\gamma$), digital gain adjustments (0–80 dB), zoom factors, and ergonomic layout of the REIS-105M1 instrument.
- **Interactive Control Panel Exploration:** Complete interactive front panel with authentic push-buttons, navigation D-pad, power button dialog, memory management, backlight brightness toggling, and tactile click sound feedback.
- **Realistic Menu & Dialog System:** Full emulation of device internal screens: Main Menu, Memory Allocation & Records, Cable Shortening Table, System Parameters, Battery Status, and Power-off Confirmation.
- **Reflectometry Visualization:** Illustrates how TDR pulses propagate along transmission lines, demonstrating the visual signatures of common cable discontinuities (open circuits $k = +1$, short circuits $k = -1$, and impedance irregularities) with distance calculation based on two-way delay $L = \frac{v \cdot \Delta t}{2}$.
- **Authentic Telemetry Display:** High-fidelity 128x64 monochrome LCD display layout with custom 5x7 bitmap raster fonts, dual measurement cursors, status lines, and on-screen diagnostics.

---

## 🏛️ Solution Architecture

The solution is engineered with clean separation of concerns across focused modular components:

```
Reis105M1/
├── assets/                     # Graphical assets (app icons) and tactile audio samples (.wav)
├── src/
│   ├── Reis105M1.Core/         # Domain state models, TDR simulation engine, finite state machine
│   ├── Reis105M1.Display/      # 128x64 LCD raster buffer, 5x7 typography, primitives, bitmap blitter
│   └── Reis105M1.UI/           # WPF presentation layer, interactive 2D scene, audio, animations
├── tests/
│   └── Reis105M1.Core.Tests/   # Unit tests for core formulas, trace synthesis, and FSM transitions
├── .editorconfig
├── .gitignore
├── README.md
└── Reis105M1.sln
```

### Module Breakdown

1. **`Reis105M1.Core`** (`net8.0-windows`):
   - `DeviceState`: Encapsulates operational parameters including power status, measurement range ($12.5\,\text{m}$ to $25\,\text{km}$), velocity factor ($VOP$ / shortening factor), digital gain (0–80 dB), zoom factors, and measurement cursor positions.
   - `ReflectometryEngine`: Synthesizes pulse response waveforms based on electromagnetic propagation, attenuation, and boundary reflection coefficients for demonstration purposes.
   - `MenuStateMachine`: Provides a deterministic state machine managing front-panel button actions, navigation, and state transitions.

2. **`Reis105M1.Display`** (`net8.0-windows`):
   - Emulates the hardware 128x64 monochrome liquid crystal display matrix with configurable backlight levels.
   - Contains an embedded 5x7 pixel raster font engine for rendering real-time on-screen telemetry (range, gain, $VOP$, cursor coordinates, and $\Delta L$).
   - Implements authentic screen modes: Trace view, Main Menu, Memory manager, Cable table, Parameters, Battery monitor, and Power-off dialog.
   - Zero-allocation frame copying directly to WPF `WriteableBitmap`.

3. **`Reis105M1.UI`** (`net8.0-windows` / WPF):
   - Rich interactive 2D workbench scene featuring an authentic high-resolution device front panel.
   - Embedded tactile audio feedback on button clicks using integrated waveform audio.
   - Smooth 2D cable connection animations (BNC connector mating) using WPF Storyboards.
   - Viewport camera zoom transitions (`ScaleTransform`, `TranslateTransform`) for focused inspection of the screen and controls.

4. **`Reis105M1.Core.Tests`** (`xUnit`):
   - Automated tests verifying signal velocity reciprocal calculations, defect distance resolution, trace point bounds, and state machine transitions.

---

## 💻 Technology Stack

- **Platform:** .NET 8.0 (Windows Desktop / WPF)
- **Language:** C# 12
- **UI & Graphics:** XAML, Storyboard, WriteableBitmap, Low-level pixel blitting
- **Audio:** Embedded WAV playback for tactile button feedback
- **Testing:** xUnit, .NET Test SDK

---

## 🚀 Build & Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher
- Operating System: Windows 10 / 11

### CLI Commands
```bash
# Clone the repository
git clone https://github.com/RaccoonKa/Reis105M1.git
cd Reis105M1

# Restore NuGet dependencies & build solution
dotnet restore
dotnet build

# Run unit tests
dotnet test

# Launch the interactive overview application
dotnet run --project src/Reis105M1.UI/Reis105M1.UI.csproj
```

---

## 🌿 Branching Strategy (Git Flow)

- `main` — Production-ready, stable final release.
- `develop` — Active development and integration branch.
- `feature/core-simulation` — Enhancements for device models and TDR signal generation.
- `feature/display-renderer` — LCD screen emulation, raster typography, and rendering optimizations.
- `feature/ui-animations` — Desk scene, interactive controls, audio feedback, and camera animations.
