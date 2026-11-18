# NVIDIA HDR Screenshot Converter

[English](README.md) | [简体中文](README.zh-CN.md)

Convert NVIDIA GeForce Experience / NVIDIA App HDR screenshots (`.jxr` / JPEG XR) into standard **JPG** or **PNG** files that any phone, messaging app, or viewer can open — with correct colors.

When HDR is enabled, NVIDIA captures screenshots as `.jxr` (JPEG XR), a Microsoft HDR format that:

- Most phones and chat apps (WeChat, WhatsApp, etc.) **cannot open**.
- Stores **linear light** data that looks dark, grey, and purple-tinted when viewed as a normal sRGB image.

This tool fixes both problems: it decodes the HDR data correctly, applies a proper **HDR → SDR tone mapping**, and exports a standard sRGB image.

---

## Table of Contents

- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Usage](#usage)
  - [Double-click (beginner)](#double-click-beginner)
  - [Command line](#command-line)
- [Options](#options)
- [How it works](#how-it-works)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

---

## Features

- ✅ Converts `.jxr` (JPEG XR) HDR screenshots to **JPG** and/or **PNG**
- ✅ Correct **HDR → SDR tone mapping** (Reinhard curve) — no blown-out highlights, no crushed blacks
- ✅ Fast — pixel processing runs in compiled C# (a 2560×1600 image converts in ~1 second)
- ✅ Batch processing — scans an entire folder recursively
- ✅ Zero dependencies — uses only Windows built-in components
- ✅ No network access, no telemetry, no installers
- ✅ Interactive menu + full command-line interface
- ✅ Original files are never modified

---

## Requirements

- **Windows 10 / 11** (any edition)
- Nothing else. The tool uses the built-in **Windows Imaging Component (WIC)** codec that ships with Windows.

> No Python, no .NET SDK, no third-party software required.

---

## Installation

1. Download or clone this repository:

   ```bash
   git clone https://github.com/tears8923/nvidia-jxr-converter.git
   ```

2. That's it. There is no installer.

The three files must stay in the **same folder**:

- `convert-jxr.bat` — the double-click launcher with a format menu
- `jxr2sdr.ps1` — the conversion engine (PowerShell)
- `JxrConverter.cs` — the C# pixel-processing engine (required for speed)

---

## Usage

### Double-click (beginner)

1. Open the folder containing the three files.
2. **Double-click `convert-jxr.bat`**.
3. A menu appears:

   ```
   [1] JPG  - small size, good for WeChat / phone
   [2] PNG  - lossless, best quality
   [3] BOTH - export JPG and PNG together

   Choose 1, 2 or 3:
   ```

4. Press `1`, `2`, or `3` and hit Enter.
5. Wait for `done: X converted` to appear.
6. Press any key to close the window.
7. Find your images in the `done` folder.

By default, the tool scans `Videos\NVIDIA` recursively and converts **every** `.jxr` file it finds.

### Command line

Open PowerShell in the project folder and run `jxr2sdr.ps1` directly.

**Convert all `.jxr` files under `Videos\NVIDIA` to JPG:**

```powershell
.\jxr2sdr.ps1
```

**Convert to PNG:**

```powershell
.\jxr2sdr.ps1 -Fmt png
```

**Convert a single file:**

```powershell
.\jxr2sdr.ps1 -InPath "C:\path\to\screenshot.jxr"
```

**Convert a whole folder:**

```powershell
.\jxr2sdr.ps1 -InPath "C:\path\to\folder"
```

**Custom output location:**

```powershell
.\jxr2sdr.ps1 -Fmt jpg -OutDir "D:\converted"
```

---

## Options

| Option    | Values            | Default              | Description                              |
|-----------|-------------------|----------------------|------------------------------------------|
| `-Fmt`    | `jpg`, `png`      | `jpg`                | Output format                            |
| `-InPath` | file or folder    | *(scan Videos\NVIDIA)* | Input file or folder to convert         |
| `-OutDir` | folder path       | source folder        | Output directory                         |
| `-Mode`   | `tonemap`, `clip` | `tonemap`            | HDR → SDR conversion method              |

### `-Mode` details

| Mode       | Behavior                                                                 | Use when                          |
|------------|--------------------------------------------------------------------------|-----------------------------------|
| `tonemap`  | Reinhard curve compresses highlights smoothly, preserving detail         | **Default**, natural-looking results |
| `clip`     | Values above 1.0 are hard-clipped to white                              | Simpler, may lose highlight detail |

---

## How it works

The conversion happens in four steps:

1. **Decode** — Windows' built-in WIC (`BitmapDecoder`) decodes the `.jxr` file into **32-bit float RGB**, preserving the full HDR dynamic range.
2. **Tone map** — a Reinhard curve (`v / (1 + 0.35·v)`) compresses the HDR luminance into the 0–1 range with a smooth highlight rolloff. This step runs in **compiled C#** (`JxrConverter.cs`) for speed.
3. **sRGB encode** — the standard sRGB transfer function (`1.055·v^(1/2.4) - 0.055`) is applied.
4. **Encode** — the .NET `JpegBitmapEncoder` / `PngBitmapEncoder` writes a standard 8-bit image.

Unlike generic viewers (which simply truncate the float data — producing blown highlights and grey shadows), this tool performs a real tone mapping pass, so the result looks like what you saw on screen.

### Why not just rename `.jxr` to `.jpg`?

Because `.jxr` is **JPEG XR**, not regular JPEG — it is a completely different codec. Renaming does not convert anything. This tool actually decodes and re-encodes the image.

---

## Troubleshooting

### The window flashes and closes instantly

The script likely failed before you could read the error. Run it from PowerShell instead:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\jxr2sdr.ps1
```

This keeps the window open and shows any error message.

### "Running scripts is disabled on this system"

PowerShell's default execution policy blocks scripts. Allow it once:

```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

Type `Y` and press Enter when prompted. You only need to do this once.

### "no .jxr files found"

Check that your NVIDIA screenshots are being saved as `.jxr`. With HDR enabled, NVIDIA saves to:

```
C:\Users\<your-user>\Videos\NVIDIA\<game-name>\
```

### The output still looks dark

Make sure you are using `-Mode tonemap` (the default). If a particular image is still dark, the screenshot itself was likely underexposed — that is not a conversion issue.

### Duplicate `_1`, `_2` files appear

To avoid overwriting existing files, new files get a numeric suffix when a name collision is detected.

---

## The root cause — and the permanent fix

If you would rather not convert files at all, disable HDR before taking a screenshot:

- Press **`Win + Alt + B`** to toggle HDR on/off.

With HDR off, NVIDIA captures plain 8-bit PNG files that phones can open directly — no conversion needed.

---

## Contributing

Contributions, issues, and feature requests are welcome.

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push and open a pull request

Please keep changes minimal and document any new options in the README.

---

## License

This project is licensed under the **MIT License**. See [LICENSE](LICENSE) for details.

```
MIT License

Copyright (c) 2026 tears8923
```
