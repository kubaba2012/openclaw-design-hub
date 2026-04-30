Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

public class WC3 {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] public static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
}
"@

$targetPid = 18004
$wins = [System.Collections.Generic.List[object]]::new()
$cb = [WC3+EnumWindowsProc]{
    param($h, $p)
    $outPid = 0; [WC3]::GetWindowThreadProcessId($h, [ref]$outPid)
    if ($outPid -eq $targetPid) {
        $len = [WC3]::GetWindowTextLength($h)
        if ($len -gt 0) {
            $sb = New-Object System.Text.StringBuilder ($len + 1)
            [WC3]::GetWindowText($h, $sb, $sb.Capacity)
            $wins.Add([PSCustomObject]@{ Hwnd = $h; Title = $sb.ToString() })
        }
    }
    return $true
}
[WC3]::EnumWindows($cb, [IntPtr]::Zero)

$w = $wins | Where-Object { $_.Title -eq "OpenClaw DesignHub" } | Select-Object -First 1
if ($w) {
    [WC3]::SetForegroundWindow($w.Hwnd) | Out-Null
    Start-Sleep -Milliseconds 300

    $rect = New-Object WC3+RECT
    [WC3]::GetWindowRect($w.Hwnd, [ref]$rect) | Out-Null
    $winW = $rect.Right - $rect.Left
    $winH = $rect.Bottom - $rect.Top
    $borderLeft = $rect.Left + ($winW - 200) / 2

    Write-Host "Window: $($rect.Left),$($rect.Top) ${winW}x${winH}"
    Write-Host "Border: $borderLeft,$($rect.Top)  200x48"

    # Click center of collapsed pill
    $cx = [int]($borderLeft + 100)
    $cy = [int]($rect.Top + 24)
    Write-Host "Clicking ($cx, $cy)..."

    [WC3]::SetCursorPos($cx, $cy)
    Start-Sleep -Milliseconds 200
    [WC3]::mouse_event([uint32]2, [uint32]0, [uint32]0, [uint32]0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 80
    [WC3]::mouse_event([uint32]4, [uint32]0, [uint32]0, [uint32]0, [UIntPtr]::Zero)
    Write-Host "Click sent"

    Start-Sleep -Milliseconds 600

    $rect2 = New-Object WC3+RECT
    [WC3]::GetWindowRect($w.Hwnd, [ref]$rect2) | Out-Null
    $newH = $rect2.Bottom - $rect2.Top
    Write-Host "Window height after click: $newH (expect ~600 if expanded)"
} else {
    Write-Host "Window not found"
}