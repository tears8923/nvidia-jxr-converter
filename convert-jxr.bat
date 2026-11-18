@echo off
setlocal

echo.
echo  ============================================
echo   NVIDIA HDR Screenshot (.jxr) Converter
echo  ============================================
echo.
echo    [1] JPG  - small size, good for WeChat / phone
echo    [2] PNG  - lossless, best quality
echo    [3] BOTH - export JPG and PNG together
echo.
choice /c 123 /n /m "Choose 1, 2 or 3: "

if errorlevel 3 goto both
if errorlevel 2 goto png
goto jpg

:jpg
echo.
echo Converting to JPG ...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0jxr2sdr.ps1" -Fmt jpg -OutDir "%~dp0_converted"
goto done

:png
echo.
echo Converting to PNG ...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0jxr2sdr.ps1" -Fmt png -OutDir "%~dp0_converted"
goto done

:both
echo.
echo Converting to JPG + PNG ...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0jxr2sdr.ps1" -Fmt jpg -OutDir "%~dp0_converted"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0jxr2sdr.ps1" -Fmt png -OutDir "%~dp0_converted"
goto done

:done
echo.
echo Done. Output folder:
echo %~dp0_converted
echo.
pause
