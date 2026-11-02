# REIS-105M1 Digital Pulse Reflectometer Simulator

An interactive virtual trainer and digital simulation of the portable pulse reflectometer (Time-Domain Reflectometer / TDR) **REIS-105M1**, built on **.NET 8 (WPF)**. The project includes a physical/mathematical cable line model, a 128x64 monochrome virtual LCD screen buffer, and an interactive 2D desktop scene with animated cable connections and smooth camera zoom.

---

## Repository Description

**REIS-105M1 Simulator** is a specialized educational and industrial simulation software designed to emulate the operational workflow, controls, display metrics, and reflectometry physics of the REIS-105M1 cable fault locator. It provides electrical engineers, telecommunication technicians, and students with an authentic hands-on experience in detecting cable discontinuities (open circuits, short circuits, impedance mismatches), measuring fault distances, and calibrating velocity of propagation (VOP) parameters without requiring physical hardware or damaged transmission lines.

---

## Solution Architecture

The solution adheres to clean architecture principles and is divided into modular, decoupled libraries:

```
Reis105M1/
├── src/
│   ├── Reis105M1.Core/         # Domain models, TDR physics engine, finite state machine
│   ├── Reis105M1.Display/      # 128x64 LCD raster buffer, graphic primitives, blitter
│   └── Reis105M1.UI/           # WPF application, interactive device front panel, 2D animations
├── tests/
│   └── Reis105M1.Core.Tests/   # Unit tests for delay calculations, trace synthesis, and FSM
├── .editorconfig
├── .gitignore
├── README.md
└── Reis105M1.sln
```

### Module Breakdown

1. **`Reis105M1.Core`** (`net8.0-windows`):
   - `DeviceState`: Manages device power, active measurement range, velocity of propagation / shortening factor ($VOP$), digital gain (in dB), and cursor positions.
   - `ReflectometryEngine`: Synthesizes realistic TDR traces based on electromagnetic wave propagation, signal delays ($L = \frac{v \cdot \Delta t}{2}$), exponential cable attenuation, Gaussian noise, and boundary reflection coefficients for various defect types (open circuit, short circuit, impedance mismatches).
   - `MenuStateMachine`: Implements a deterministic finite state machine (FSM) handling front-panel keypad inputs, mode switching, and parameter modifications.

2. **`Reis105M1.Display`** (`net8.0-windows`):
   - Emulates the hardware 128x64 monochrome liquid crystal display (LCD) matrix.
   - Provides efficient line drawing (Bresenham algorithm), dotted measurement cursors, coordinate grids, and direct fast memory copying into WPF `WriteableBitmap`.

3. **`Reis105M1.UI`** (`net8.0-windows` / WPF):
   - Interactive 2D workbench scene hosting the virtual device enclosure, BNC connectors, and cable lines.
   - Smooth 2D hardware connection animations using WPF Storyboards and `DoubleAnimation`.
   - Cinematic zoom-in camera transitions to focus on the LCD and controls using `ScaleTransform` and `TranslateTransform`.
   - Interactive push-button layer mapped to the physical device control panel.

4. **`Reis105M1.Core.Tests`** (`xUnit`):
   - Verifies reciprocal delay-distance formulas, reflection pulse shapes, gain limits, and state transitions.

---

## Technology Stack

- **Platform:** .NET 8.0 (Windows Desktop / WPF)
- **Language:** C# 12
- **UI & Graphics:** WPF, XAML, Storyboard, WriteableBitmap
- **Testing:** xUnit, .NET Test SDK

---

## Build & Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher
- Operating System: Windows 10 / 11

### CLI Commands
```bash
# Clone the repository
git clone <repository-url>
cd Reis105M1

# Restore NuGet dependencies & build solution
dotnet restore
dotnet build

# Run unit tests
dotnet test

# Launch the simulator
dotnet run --project src/Reis105M1.UI/Reis105M1.UI.csproj
```

---

## Branching Strategy (Git Flow)

- `main` — Production-ready, stable releases.
- `develop` — Active integration and staging branch.
- `feature/core-simulation` — Enhancements for the mathematical TDR model and cable physics.
- `feature/display-renderer` — LCD screen emulation and bitmap rendering optimizations.
- `feature/ui-animations` — Desk scene, BNC plug animations, and camera viewport transitions.
