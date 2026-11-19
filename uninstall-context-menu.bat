@echo off
setlocal

echo 正在卸载右键菜单...

reg delete "HKCR\SystemFileAssociations\.jxr\shell\JxrToJpg" /f >nul 2>&1
reg delete "HKCR\SystemFileAssociations\.jxr\shell\JxrToPng" /f >nul 2>&1
reg delete "HKCR\SystemFileAssociations\.jxr\shell\JxrToBoth" /f >nul 2>&1

echo.
echo 卸载完成！右键菜单已移除。
echo.
pause
