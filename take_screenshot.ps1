Add-Type @"
using System;
using System.Runtime.InteropServices;
public class User32 {
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
"@

$process = Get-Process -Name "Unity" | Select-Object -First 1
if ($process) {
    $hWnd = $process.MainWindowHandle
    if ($hWnd -ne [IntPtr]::Zero) {
        [User32]::ShowWindow($hWnd, 9)
        [User32]::SetForegroundWindow($hWnd)
        Start-Sleep -Seconds 2
    }
}

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$Screen = [System.Windows.Forms.Screen]::PrimaryScreen
$Width  = $Screen.Bounds.Width
$Height = $Screen.Bounds.Height
$Bitmap = New-Object System.Drawing.Bitmap $Width, $Height
$Graphic = [System.Drawing.Graphics]::FromImage($Bitmap)
$Graphic.CopyFromScreen($Screen.Bounds.X, $Screen.Bounds.Y, 0, 0, $Bitmap.Size)
$Bitmap.Save("d:\My project\unity_editor_screenshot.png", [System.Drawing.Imaging.ImageFormat]::Png)
$Graphic.Dispose()
$Bitmap.Dispose()
Write-Output "Screenshot saved successfully!"
