# Industrial Machine Vision HMI — full replacement project

This is a clean replacement architecture for the existing FactorialApp prototype. It keeps the core intent (WPF + MVVM + TCP/OpenCV vision + SQLite) and adds an industrial lifecycle around it.

## Implemented target map
1. **Real camera path** — `BaslerCameraService` runtime adapter for Basler pylon; simulator remains available.
2. **Real PLC path** — native Modbus TCP service (FC03 read holding register / FC06 write single register), no PLC NuGet package required.
3. **Automatic state machine** — WaitingPart → TriggerCamera → Acquiring → Inspecting → SendingResult → Completed/Alarm.
4. **PLC-Camera-Vision handshake** — PLC start, BUSY, camera capture, vision, DONE + PASS/NG result, reset.
5. **Reconnect** — watchdog checks camera/PLC/vision every 2 s and attempts reconnect.
6. **Recipe versioning** — every save creates a new immutable recipe version in SQLite.
7. **NG image archive** — annotated NG plus raw images, date folders and serial-number filenames.
8. **SQLite traceability** — serial, cycle id, product, recipe/version, result, NG code/reason, measurement, exposure, gain, operator, image paths.
9. **History filters + image + CSV** — date/product/recipe/result/NG/serial filters, VIEW, CSV export.
10. **RBAC** — Operator / Engineer / Admin login; recipe page is Engineer+.
11. **Alarm + ACK** — persistent alarm table, alarm codes and operator acknowledgement.
12. **Watchdog + timeout + retry** — camera/vision retries, per-service timeouts, watchdog reconnect.
13. **Production statistics** — total/pass/NG/yield.
14. **Startup auto-connect** — camera/PLC connect and watchdog start when the window loads.
15. **EXE / installer** — `publish.ps1` and Inno Setup script.

## Important: real hardware boundary
The Modbus TCP implementation is complete at the protocol level, but the PLC register map must match the actual PLC program. Default register map is 0..4 in `appsettings.json`.

The Basler adapter deliberately loads `Basler.Pylon.dll` at runtime so the solution can compile without the vendor SDK reference. It targets common pylon 7 API names. **Before connecting a production camera, install Basler pylon, set the DLL path, then verify the exact parameter names and `GrabOne` overload for your installed pylon version.** A real camera cannot be hardware-validated in this environment.

The included Python server accepts the captured image itself (`image_base64`) rather than choosing a canned test image, so the pipeline is now capable of real image flow.

## First run (simulation)
1. Replace your old project with this folder or open `FactorialApp.csproj` in Rider/Visual Studio.
2. Keep `UseSimulator=true`.
3. Start `VisionServer/vision_server_industrial.py` on the configured host/port.
4. Run the WPF app.
5. Login with one of the temporary first-run accounts below.
6. Run one cycle or Auto Start.

Temporary first-run accounts:
- operator / `Operator123!`
- engineer / `Engineer123!`
- admin / `Admin123!`

Change these before any real deployment. Passwords are stored as PBKDF2-SHA256 hashes with random salts, not plaintext.

## Switch to real PLC + Basler
Set in `appsettings.json`:
```json
"UseSimulator": false,
"Camera": { "Provider": "Basler", ... },
"Plc": { "Provider": "ModbusTcp", ... }
```
Then configure PLC IP and register addresses. The PLC should provide `StartRegister=1` when a part is ready. HMI writes BUSY, DONE and Result (`1=PASS`, `2=NG`).

## Build / publish
On Windows with .NET 8 SDK:
```powershell
.\publish.ps1
```
Then compile `Installer/FactorialApp.iss` with Inno Setup.

## What must still be validated on physical equipment
- Basler pylon API compatibility for your exact SDK/camera model.
- PLC register map and endian/word semantics against your PLC program.
- Trigger electrical timing if hardware trigger is used instead of software trigger.
- Vision thresholds/tolerances using real production images and GR&R / capability data.
- Storage retention policy, disk quota and backup path.
- Machine safety: E-stop, guards, safe motion and safety PLC are **not** implemented in HMI software and must never rely on this app.
