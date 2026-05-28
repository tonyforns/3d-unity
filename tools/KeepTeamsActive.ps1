# Simula actividad minima para que Windows (y Teams) no te marque como Ausente.
# Mueve el cursor 1 px y lo devuelve; no hace clic ni escribe texto.
#
# Uso:
#   .\KeepTeamsActive.ps1
#   .\KeepTeamsActive.ps1 -IntervalSeconds 45
#
# Detener: Ctrl+C

param(
    [int]$IntervalSeconds = 60,
    [switch]$UseScrollLock
)

Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class NativeInput
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public const byte VK_SCROLL = 0x91;
    public const uint KEYEVENTF_KEYUP = 0x0002;
}
"@

function Invoke-KeepAliveTick {
    if ($UseScrollLock) {
        # Alternativa: pulsa Scroll Lock (suele no tener efecto visible).
        [NativeInput]::keybd_event([NativeInput]::VK_SCROLL, 0, 0, [UIntPtr]::Zero)
        [NativeInput]::keybd_event([NativeInput]::VK_SCROLL, 0, [NativeInput]::KEYEVENTF_KEYUP, [UIntPtr]::Zero)
        return
    }

    $point = New-Object NativeInput+POINT
    if (-not [NativeInput]::GetCursorPos([ref]$point)) {
        Write-Warning "No se pudo leer la posicion del cursor."
        return
    }

    [void][NativeInput]::SetCursorPos($point.X + 1, $point.Y)
    Start-Sleep -Milliseconds 50
    [void][NativeInput]::SetCursorPos($point.X, $point.Y)
}

Write-Host "KeepTeamsActive: intervalo ${IntervalSeconds}s. Ctrl+C para salir."
if ($UseScrollLock) {
    Write-Host "Modo: Scroll Lock (sin mover raton)."
} else {
    Write-Host "Modo: micro-movimiento del raton (sin clic)."
}

while ($true) {
    Invoke-KeepAliveTick
    $timestamp = Get-Date -Format "HH:mm:ss"
    Write-Host "[$timestamp] Actividad simulada."
    Start-Sleep -Seconds $IntervalSeconds
}
