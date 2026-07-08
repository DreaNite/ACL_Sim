# ACL Reconstruction Surgery Simulator

Unity-based simulation of an ACL (Anterior Cruciate Ligament) reconstruction
procedure, designed to run on a **Tekle holographic wall** via the THSDK with
multi-user stereo rendering. External triggers from a sibling Python project (LAIAR) drive the surgical steps over UDP.

---

## Overview

The simulation visualises the four phases of an ACL reconstruction:

1. **Setup** – patient positioned, knee flexion.
2. **Femoral Tunnel** – femoral tunnel drilled.
3. **Tibial Tunnel** – tibial tunnel drilled.
4. **Graft Insertion** – graft passed and fixed.

Each step has its own anatomical models and a camera viewpoint. A separate
Python process decides which step is active; the simulation responds by
activating the corresponding objects, showing an explanatory panel, and moving
the holographic camera rig to the matching viewpoint.

## Features

- **Multi-user holographic rendering** via Tekle THSDK; runs on the wall with
  calibrated per-user stereo.
- **Editor fallback** for development: any keyboard key replaces controller
  buttons when running on a PC without the device.
- **External step triggering** through UDP packets sent by a sibling Python
  process. 
- **Coordinated camera moves**: scripted lerps move the whole HolographicDevice
  rig (preserving per-user calibration) between viewpoints.
- **In-scene UI overlays** for welcome, per-step description, and completion.
- **Interrupt recovery**: if a step trigger arrives mid-transition, the rig is
  quickly brought back to default before the new step plays, and stale panels
  are hidden.

## Requirements

- **Unity 2022.3.62f3** (or a compatible LTS patch).
- **Tekle THSDK v3.2.0**, referenced by file path in `Packages/manifest.json`.
  This is a licensed package distributed by Tekle; without it the project will
  not compile.
- The bundled Unity modules: TextMeshPro, Timeline.

## Project structure

```
ACL_Sim/                            repo root
├── Assets/
│   ├── Scenes/ACL Sim.unity        main scene
│   ├── Scripts/                    project scripts (see table below)
│   ├── Prefabs/                    knee variants, drill, hands, doctor, OR
│   ├── Models/, Audio/, Timeline/  art and timeline assets
│   └── Plugins/                    Odin Inspector
├── Packages/manifest.json          package dependencies (THSDK)
├── ProjectSettings/
```

## Setup

1. Clone the repo.
2. Open the project in Unity 2022.3.62f3. Import the THSDK package via Package Manager.
3. Open `Assets/Scenes/ACL Sim.unity`.
4. Clone the repo with the Python project and launch it.
5. Select the **ControlManager** GameObject. On the **UdpSocket** component:
   - Set **Path Windows** or **Path Linux** to the absolute path of the Python
     project folder.
   - Toggle **Debug** to select between the Windows / Linux interpreter inside
     `.venv` (`.venv/Scripts/python.exe` vs `.venv/bin/python`).
   - Default ports: receive on **8000**, send on **8001** (loopback).
6. Press **Play**. In the editor, press any keyboard key to start the intro
   sequence (replaces a THSDK controller button press).

## How it works

### Trigger flow

```
Python ──UDP──▶ UdpSocket ──writes──▶ RAGInput.{action, step}
                                              │
                                              ▼
                                      StepCounter   (Update polls RAGInput.step)
                                              │ fires OnStepChanged(step)
                                              ▼
                                  CameraController.HandleStepChanged
                                              │
              recover ─▶ info panel ─▶ lerp to viewpoint ─▶ dwell ─▶ lerp back ─▶ (completion on last)
```

UDP payload format: `action,step` (e.g. `talk,2`). `action` drives the
surgeon's animation via `DoctorTalk`; `step` is a 1-based index that activates
the corresponding Step GameObject and viewpoint.

### Camera control

`CameraController` does **not** move a Unity Camera directly. It moves the
**HolographicDevice rig** (the parent GameObject) as a whole, so the per-user
Glasses cameras keep their calibrated local offsets — essential for correct
stereo on the wall.

Viewpoints are empty Transforms under `CameraViewpoints` in the scene. When
moving to a viewpoint, the controller computes the rig pose that places the
**midpoint of all reference cameras** (every wired Glasses) at the target.
Both users then share a symmetric framing, each with their own correct stereo.

### Sequence on Play

1. `Awake` caches the rig's serialized pose as **Default** and teleports the
   rig to **`View_Start`**. No UI visible.
2. `StartGate` polls every THSDK controller button each frame (any keyboard
   key in the editor). On press, calls `CameraController.PlayIntroSequence()`.
3. Intro: lerp `View_Start` → show fullscreen **WelcomePanel** → hide.
4. The simulation is now armed. Each UDP step trigger:
   - Hides any lingering panels.
   - If the rig isn't at Default, quick-lerps back.
   - Shows the **StepInfoPanel** with `_stepDescriptions[step-1]`.
   - Lerps Default → step viewpoint.
   - Dwells at the viewpoint.
   - Lerps back to Default.
   - If this was the last step, reuses the WelcomePanel to show the
     **completion message**.

## Scripts

| Script | Purpose |
|---|---|
| `CameraController.cs` | Owns the camera flow; lerps the holographic rig and drives the UI overlays. |
| `StartGate.cs` | Waits for any controller / keyboard press, triggers the intro. |
| `StepCounter.cs` | Watches `RAGInput.step`, toggles Step1–4 GameObjects, fires `OnStepChanged`. |
| `PlayStep.cs` | Plays the attached `PlayableDirector` whenever the GameObject enables (per-step timeline). |
| `DoctorTalk.cs` | Drives the surgeon animator integer from `RAGInput.action`. |
| `RAGInput.cs` | Static shared bus (`action`, `step`) between `UdpSocket` and consumers. |
| `UdpSocket.cs` | Listens for UDP packets from Python. |
| `Pulse.cs` | Simple scale-pulse animation, used on the Pointer of each Step. |

## License

Copyright 2026 Andrea Gómez Díaz

This project is licensed under the MIT License. 
This product includes software developed by Tekle (THSDK), used under its own proprietary license; the THSDK is not redistributed with this project.
Character animations from Mixamo, used under Mixamo Terms of Use.
