# OmenMon-Reborn: Architecture Notes

This document explains the design decisions behind the changes introduced in **OmenMon-Reborn**. It is aimed at contributors and anyone reviewing the code.

---

## Phase 3 — Dynamic Model Database

### The original flaw

`Hardware/Platform.cs` contained a hardcoded `switch` on the WMI baseboard `Product` string (e.g. `"8A14"`). Every unrecognised device fell through to `default`, silently inheriting the 2022/2023 Omen 16 EC register layout. When those addresses were wrong for the actual hardware — different fan setpoint registers, different RPM word locations — the result ranged from incorrect temperature readings to fans permanently stuck at full speed (GitHub issues #57, #81, #122, and others).

### The new architecture

Three additions fix this:

**`Hardware/PlatformPreset`** is a plain data class that names every EC register the fan subsystem needs: level setpoints (`SRP1`/`SRP2`), rate read/write registers (`XGS*`/`XSS*`), speed word registers (`RPM1`/`RPM3`), and the four control registers (`XFCD`, `OMCC`, `HPCM`, `SFAN`). `PlatformPreset.Default` holds the confirmed 2022/2023 values and is always available as a compile-time fallback — no XML required.

**`Config.Models`** (`Library/ConfigData.cs`) is a `Dictionary<string, PlatformPreset>` populated at startup from the `<Models>` section of `OmenMon.xml`. The XML schema mirrors the C# fields one-for-one, using decimal register values so they can be hand-edited without a hex calculator. `Config.SaveModel()` writes a preset back to XML through the existing `Config.Save()` path, so auto-detected entries are persisted automatically.

**`Platform.InitFans()`** (`Hardware/Platform.cs`) now does a single dictionary lookup:

```csharp
PlatformPreset preset = Config.Models.ContainsKey(product)
    ? Config.Models[product]
    : PlatformPreset.Default;
```

All `FanArray` and `EcComponent` objects are then constructed from the preset's fields. The hardcoded `switch` is gone entirely. David's confirmed `8A14` device hits the `Default` path and behaves identically to before; a different device can now have its own entry in XML without any code change.

---

## Phase 4 — Critical Bug Fixes

### TNT sensor isolation

EC registers `TNT2`–`TNT5` (0x47–0x4B) are auxiliary probes whose values are unreliable across models — on many devices they report 84 °C or 97 °C constantly, driving fan curves to maximum. They are now **disabled by default** (`Use = false`) in `ConfigData.TemperatureSensor`. They remain declared and visible in the GUI so users can verify what their hardware actually reports, but they no longer contribute to the maximum-temperature calculation that controls fan programs. Users who confirm the sensors are valid on their device can re-enable them in `OmenMon.xml`.

### HVCI driver hint

`Driver/Ring0.cs` already emitted a message when the WinRing0 kernel driver failed to load. The EC snapshot path in `CliOpProbe.cs` echoes that message including the phrase *"HVCI (Memory Integrity) active"*, directing users to the Windows Security → Core Isolation page — the most common cause of driver load failure on modern systems.

---

## Phase 5 — Smart Fallback (Heuristic Auto-Detector)

### Why read-only matters

The EC is a real microcontroller managing battery charging, thermal throttling, and power rails. Writing to an unknown register can permanently latch a fan off, corrupt a thermal threshold, or trigger a hardware protection mode. **`Hardware/AutoDetector`** therefore performs a full 256-byte read-only dump and makes its decision purely from the values it observes.

### Detection logic

The standard 2022+ Omen EC layout is characterised by two invariants that hold at any normal operating temperature and fan speed:

| Register | Address | Expected range |
|----------|---------|----------------|
| `CPUT`   | `0x57`  | 20–95 °C       |
| `RPM1`   | `0xB0`  | 0–7000 rpm     |

If both conditions are met, the device is mapped to `PlatformPreset.Default` (i.e. it uses the same register layout as the confirmed 2022/2023 hardware). The result is stored in `Config.Models` and written to `OmenMon.xml` via `Config.SaveModel()`. On the next launch, `Platform.InitFans()` finds the entry in the dictionary and the auto-detect prompt never fires again.

If neither condition is met — for example, if `CPUT` at `0x57` reads zero or above 95 °C, or `RPM1` reads an implausible value — `DetectHeuristic` returns `null`. The GUI then directs the user to the **"Auto-Calibrate & Diagnose..."** tray menu item, which runs an active 4-step fan stress sweep (`CliOpCalibration.cs` → `EcDiffScanner.cs`) to identify the correct RPM registers heuristically and apply them to the live session. The wizard supports three encodings — 16-bit little-endian, period-encoded 8-bit, and direct-multiplier 8-bit — and persists its result to a sidecar `OmenMon-AutoCal.xml` that's reloaded on the next launch via `AutoCal.Load()` in `Platform.InitFans()`. A Markdown report is copied to the clipboard so the user can also share the result upstream for inclusion in `OmenMon.xml`'s `<Models>` block.

### Integration point

The prompt appears in `GuiFormMain.CheckUnknownModel()`, wired to the form's `Shown` event so it fires once after the window is fully rendered. The `Shown` event is unsubscribed immediately so the method runs at most once per session.

---

## Phase 6 — Community Database Contributor

### Probe logic reuse

`App/Cli/CliOpProbe.cs` was originally written as a pure CLI operation: it built a Markdown string, printed it to the console, and wrote it to a file. The key change for Phase 6 is exposing `CliOp.ProbeGetMarkdown(bool includeEcDiff)` as an `internal static` method that returns the string without any file or console side-effects (at least in GUI mode, where `Console.WriteLine` is a no-op).

The `includeEcDiff` flag avoids the 5-second wait for a second EC snapshot when called from the GUI — the single-snapshot output is already sufficient to identify the device and its register layout.

### GUI surface

`GuiMenu.EventActionContribute` calls `ProbeGetMarkdown(includeEcDiff: false)`, places the result on the system clipboard, and opens the GitHub new-issue URL in the default browser via `Process.Start`. The full operation is synchronous and completes in under a second. The user pastes the clipboard content directly into the GitHub issue body — no file management, no copy-pasting from a terminal.

This design keeps the application itself strictly offline (no outbound connections are initiated by OmenMon), while still making it trivial for a non-technical user to contribute their hardware data.

---

## Phase 7 — RGB Preset Hotkey & Omen Key Custom Actions

### RGB Preset Hotkey
A dedicated method `CycleColorPresets()` in `App/Gui/GuiOp.cs` cycles through all `<ColorPresets>` defined in `OmenMon.xml` when the Omen key is pressed (if enabled via `KeyToggleColorPreset`). The cycle identifies the current preset by comparing active hardware color levels against configured presets, wrapping cleanly back to the first preset.

---

## Phase 8 — WinRing0 to PawnIO Kernel Driver Migration (v1.4.0)

### Security & HVCI Compliance
WinRing0 kernel driver loading triggered Microsoft Defender alerts and HVCI (Memory Integrity) blocks on modern Windows 11 builds. Phase 8 replaced WinRing0 with [PawnIO](https://pawnio.eu/), using a Microsoft-signed kernel driver and embedded bytecode module (`Resources/LpcACPIEC.bin`).

### Abstracted Driver Layer
`Driver/PawnIo.cs` exposes an execution engine for PawnIO, while `Driver/Ring0.cs` retains its legacy public interface. Higher hardware abstraction layers (`Hardware/Ec.cs`) remain completely unaware of the underlying driver engine swap.

---

## Phase 9 — Crash Telemetry, EC Trace & Auto-Calibration Architecture (v1.4.0)

### Telemetry-Free Crash Dumper (`App/Crash.cs`)
Hooks unhandled exception pipelines and dumps local `OmenMon-crash-*.log` files containing full stack trace, process metadata, and system environment info without initiating any network connections.

### Lock-Free EC Trace Buffer (`Library/EcTrace.cs`)
Maintains a 1024-entry circular buffer recording register reads/writes (`ReadByte`/`WriteByte`) to aid in diagnosing intermittent fan spikes or hardware latching bugs.

### Auto-Calibration Sweep & Sidecar (`Library/AutoCal.cs`, `CliOpCalibration.cs`)
Active 4-step fan stress sweep heuristically identifies 16-bit LE, period-encoded, or direct-multiplier tachometer registers and persists them to `OmenMon-AutoCal.xml`.

---

## Phase 10 — Fan Safety Auto-Revert & Topmost Overlay Architecture

### Safety Auto-Revert (`FanConstSafetyEnabled` / `FanConstSafetyTemp`)
To prevent thermal throttling and hardware damage when low manual constant fan speeds are left active during heavy workloads:
- `App/Gui/GuiOp.cs` implements `CheckFanConstSafety(byte maxTemp)`.
- When temperature exceeds `FanConstSafetyTemp` (e.g. 85°C), `RevertToAuto()` executes hardware reset commands (clearing custom speed levels, disengaging manual mode, setting countdown to 0, and restoring BIOS dynamic fan control).
- `SyncAfterRevert()` updates GUI controls (`RdoFanAuto.Checked = true`, locking trackbars `TrkFan0Lvl.Enabled = false`) to ensure UI and periodic background monitor loops (`UpdateFan()`, `GuiTray`) stay synchronized in Auto mode rather than re-asserting constant speed settings.

### Non-Intrusive In-Game Overlay (`App/Gui/GuiFormOverlay.cs`)
- Provides a transparent, topmost borderless window (`WS_EX_LAYERED`, `WS_EX_TRANSPARENT`, `WS_EX_NOACTIVATE`, `WS_EX_TOOLWINDOW`).
- Displays a critical temperature warning without stealing focus from full-screen games or applications.
- Features a self-terminating timer (3 seconds) to automatically dismiss after notification.

