# Convert NVIDIA HDR screenshots (.jxr / JPEG XR) into SDR JPG/PNG.
# Fast version: per-pixel loop runs in compiled C# (JxrConverter.cs), not PowerShell.
param(
    [string]$InPath = "",
    [string]$OutDir = "",
    [ValidateSet("tonemap", "clip")][string]$Mode = "tonemap",
    [ValidateSet("jpg", "png")][string]$Fmt = "jpg"
)

$ErrorActionPreference = 'Stop'

# ---- load WPF imaging assemblies ----
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

# ---- compile the C# engine from the adjacent .cs file ----
$csPath = Join-Path $PSScriptRoot 'JxrConverter.cs'
if (Test-Path -LiteralPath $csPath) {
    Add-Type -Path $csPath
} else {
    Write-Output "missing $csPath"
    exit 1
}

# ---- collect input files ----
$files = @()
if ($InPath) {
    if (Test-Path -LiteralPath $InPath -PathType Container) {
        $files = @(Get-ChildItem -LiteralPath $InPath -Recurse -Filter *.jxr -ErrorAction SilentlyContinue |
                   Select-Object -ExpandProperty FullName)
    } elseif (Test-Path -LiteralPath $InPath -PathType Leaf) {
        $files = @((Resolve-Path -LiteralPath $InPath).Path)
    } else {
        Write-Output "path not found: $InPath"
        exit 1
    }
} else {
    $root = Join-Path $env:USERPROFILE "Videos\NVIDIA"
    if (-not (Test-Path -LiteralPath $root)) {
        Write-Output "default folder not found: $root"
        exit 1
    }
    $files = @(Get-ChildItem -LiteralPath $root -Recurse -Filter *.jxr -ErrorAction SilentlyContinue |
               Select-Object -ExpandProperty FullName)
}

if ($files.Count -eq 0) {
    Write-Output "no .jxr files found"
    exit 0
}

Write-Output "found $($files.Count) file(s), converting to $($Fmt.ToUpper())"

$okCount = 0
$failCount = 0

foreach ($f in $files) {
    Write-Output ""
    $leaf = Split-Path $f -Leaf
    Write-Output "$leaf"
    try {
        $uri = New-Object System.Uri($f)
        $dec = [System.Windows.Media.Imaging.BitmapDecoder]::Create(
            $uri,
            [System.Windows.Media.Imaging.BitmapCreateOptions]::None,
            [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
        $frame = $dec.Frames[0]

        $conv = New-Object System.Windows.Media.Imaging.FormatConvertedBitmap
        $conv.BeginInit()
        $conv.Source = $frame
        $conv.DestinationFormat = [System.Windows.Media.PixelFormats]::Rgba128Float
        $conv.EndInit()

        $w = $conv.PixelWidth
        $h = $conv.PixelHeight
        $stride = $w * 16
        $buf = New-Object byte[] ($stride * $h)
        $conv.CopyPixels($buf, $stride, 0)

        $floats = New-Object float[] ($w * $h * 4)
        [System.Buffer]::BlockCopy($buf, 0, $floats, 0, $w * $h * 16)

        # fast C# pixel loop
        $out = [JxrConverter]::Process($floats, $w, $h, ($Mode -eq 'tonemap'))

        $bmp = [System.Windows.Media.Imaging.BitmapSource]::Create(
            $w, $h, 96, 96,
            [System.Windows.Media.PixelFormats]::Rgb24, $null, $out, $w * 3)

        if ($OutDir) { $dir = $OutDir } else { $dir = Split-Path $f -Parent }
        New-Item -ItemType Directory -Force -Path $dir | Out-Null

        $name = [System.IO.Path]::GetFileNameWithoutExtension($f) + "." + $Fmt
        $dst = Join-Path $dir $name
        $i2 = 1
        while (Test-Path -LiteralPath $dst) {
            $name = [System.IO.Path]::GetFileNameWithoutExtension($f) + "_$i2." + $Fmt
            $dst = Join-Path $dir $name
            $i2++
        }

        if ($Fmt -eq 'jpg') {
            $enc = New-Object System.Windows.Media.Imaging.JpegBitmapEncoder
            $enc.QualityLevel = 95
        } else {
            $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
        }
        $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
        $fs = [System.IO.File]::Create($dst)
        try { $enc.Save($fs) } finally { $fs.Dispose() }

        $mb = [math]::Round((Get-Item -LiteralPath $dst).Length / 1MB, 2)
        Write-Output "  ${w}x${h} -> $name  ($mb MB)"
        $okCount++
    } catch {
        Write-Output "  FAILED: $($_.Exception.Message)"
        $failCount++
    }
}

Write-Output ""
Write-Output "done: $okCount converted, $failCount failed"
