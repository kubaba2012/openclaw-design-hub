@echo off
chcp 65001 >nul 2>&1
title OpenClaw DesignHub - 编译运行

echo ========================================
echo   OpenClaw DesignHub 自动构建脚本
echo ========================================
echo.

:: 设置颜色
color 0A

:: 检查 .NET SDK
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] 未找到 .NET SDK，请先安装 .NET 8.0 SDK
    pause
    exit /b 1
)

:: 停止旧进程
echo [1/4] 停止旧进程...
taskkill /F /IM OpenClaw.DesignHub.exe >nul 2>&1
taskkill /F /IM OpenClaw.DesignHub2.exe >nul 2>&1
timeout /t 1 /nobreak >nul

:: 清理并编译
echo [2/4] 清理并编译...
dotnet clean -c Release --verbosity quiet 2>nul
if %errorlevel% neq 0 (
    echo [警告] 清理失败，继续编译...
)

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true --verbosity normal
if %errorlevel% neq 0 (
    echo.
    color 0C
    echo [错误] 编译失败！
    pause
    exit /b 1
)

echo.
echo [3/4] 启动程序...
start "" "bin\Release\net8.0-windows\win-x64\publish\OpenClaw.DesignHub2.exe"
timeout /t 2 /nobreak >nul

:: 验证进程
echo [4/4] 验证运行状态...
tasklist /FI "IMAGENAME eq OpenClaw.DesignHub2.exe" 2>nul | find /I "OpenClaw.DesignHub2.exe" >nul
if %errorlevel% equ 0 (
    color 0A
    echo.
    echo ========================================
    echo   ✓ 程序启动成功！
    echo   进程: OpenClaw.DesignHub.exe
    echo ========================================
) else (
    color 0E
    echo.
    echo [警告] 进程可能未正常启动，请检查日志:
    echo   %APPDATA%\OpenClaw.DesignHub\Logs\
)

echo.
pause