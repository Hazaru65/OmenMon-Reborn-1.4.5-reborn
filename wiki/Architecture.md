# Architecture

This page provides a high-level overview of the OmenMon-Reborn architecture and links to dedicated documentation guides.

For the authoritative, complete technical architecture and internal subsystem design document, see [ARCHITECTURE.md](../ARCHITECTURE.md).

---

## Architectural Overview

OmenMon-Reborn extends the upstream OmenMon platform to replace static hardware assumptions with dynamic configuration, non-destructive hardware detection, and modern security compliance:

1. **Dynamic Model Database (`PlatformPreset`)**: Hardware register mappings are decoupled from compiled C# switches into runtime XML presets (`OmenMon.xml`) and cached in memory. See the [Model Database](Model-Database) guide.
2. **Safe Heuristic Auto-Detection (`AutoDetector`)**: Unknown motherboards are analyzed via non-destructive, read-only EC register dumps rather than blind defaults or speculative writes. See the [Auto-Detection](Auto-Detection) guide.
3. **Interactive Hardware Calibration & Probe**: Active fan sweep calibration (`EcDiffScanner`) and hardware probe export (`CliOpProbe`) enable users to identify and submit device profiles upstream without external tools. See [Contributing Hardware Data](Contributing-Hardware-Data).
4. **Modern Kernel Bridge (`PawnIO`)**: Replaces obsolete WinRing0 with Microsoft-signed PawnIO driver services and bytecode execution, maintaining Windows 11 HVCI (Memory Integrity) compatibility.
5. **Thermal Safety Guardrails**: Runtime monitoring (`CheckFanConstSafety`) reverts manual fixed fan overrides to BIOS automatic curves when critical temperature thresholds are breached, accompanied by a non-intrusive notification overlay.

---

## Detailed Reference

- **Core Technical Architecture:** [`ARCHITECTURE.md`](../ARCHITECTURE.md)
- **Model Database & XML Schema:** [Model Database](Model-Database)
- **Auto-Detection Workflow:** [Auto-Detection](Auto-Detection)
- **Fan Calibration Wizard:** [Contributing Hardware Data](Contributing-Hardware-Data)
- **General User & CLI Manual:** [INSTRUCTION.md](../INSTRUCTION.md)

