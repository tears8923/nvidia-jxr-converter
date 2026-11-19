@echo off
setlocal

REM ============================================================
REM  为 .jxr 文件安装右键菜单（转换为 JPG / PNG）
REM  需要管理员权限
REM ============================================================

set "EXE=%~dp0JXR转格式.exe"

if not exist "%EXE%" (
    echo 错误：找不到 %EXE%
    echo 请把这个 bat 和 JXR转格式.exe 放在同一文件夹
    pause
    exit /b 1
)

echo 正在安装右键菜单...

REM 转换为 JPG
reg add "HKCR\SystemFileAssociations\.jxr\shell\JxrToJpg" /ve /d "转换为 JPG" /f >nul
reg add "HKCR\SystemFileAssociations\.jxr\shell\JxrToJpg\command" /ve /d "\"%EXE%\" -tojpg \"%%1\"" /f >nul

REM 转换为 PNG
reg add "HKCR\SystemFileAssociations\.jxr\shell\JxrToPng" /ve /d "转换为 PNG" /f >nul
reg add "HKCR\SystemFileAssociations\.jxr\shell\JxrToPng\command" /ve /d "\"%EXE%\" -topng \"%%1\"" /f >nul

REM 转换为 JPG+PNG
reg add "HKCR\SystemFileAssociations\.jxr\shell\JxrToBoth" /ve /d "转换为 JPG + PNG" /f >nul
reg add "HKCR\SystemFileAssociations\.jxr\shell\JxrToBoth\command" /ve /d "\"%EXE%\" -toboth \"%%1\"" /f >nul

echo.
echo 安装完成！
echo 现在右键 .jxr 文件，会看到：
echo   转换为 JPG
echo   转换为 PNG
echo   转换为 JPG + PNG
echo.
pause
