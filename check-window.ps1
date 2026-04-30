Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

public class WT {
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lp, IntPtr p);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder sb, int cap);
    [DllImport("user32.dll")] public static extern int GetWindowTextLength(IntPtr h);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L,T,R,B; }
    public delegate bool EnumWindowsProc(IntPtr h, IntPtr p);
}
"@

$targetPid = 3488
$wins = [System.Collections.Generic.List[object]]::new()
$cb = [WT+EnumWindowsProc]{
    param($h, $p)
    $out = 0
    [WT]::GetWindowThreadProcessId($h, [ref]$out)
    if ($out -eq $targetPid) {
        $len = [WT]::GetWindowTextLength($h)
        if ($len -gt 0) {
            $sb = New-Object System.Text.StringBuilder ($len + 1)
            [WT]::GetWindowText($h, $sb, $sb.Capacity)
            $wins.Add([PSCustomObject]@{Hwnd=$h; Title=$sb.ToString()})
        }
    }
    return $true
}
[WT]::EnumWindows($cb, [IntPtr]::Zero) | Out-Null

$w = $wins | Where-Object { $_.Title -eq "OpenClaw DesignHub" } | Select-Object -First 1
if ($w) {
    $rect = New-Object WT+RECT
    [WT]::GetWindowRect($w.Hwnd, [ref]$rect) | Out-Null
    $w2 = $rect.R - $rect.L
    $h2 = $rect.B - $rect.T
    Write-Host "Window: $($rect.L),$($rect.T) ${w2}x${h2}"
    Write-Host "Expected: 200x48 (collapsed pill)"
} else {
    Write-Host "Window not found"
}