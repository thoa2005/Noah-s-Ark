Add-Type @"
using System;
using System.Runtime.InteropServices;

public class User32 {
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
"@

$process = Get-Process -Name "Unity" | Where-Object { $_.MainWindowTitle -ne "" } | Select-Object -First 1
if ($process) {
    $hWnd = $process.MainWindowHandle
    if ($hWnd -ne [IntPtr]::Zero) {
        # Restore and Focus
        [User32]::ShowWindow($hWnd, 9)
        [User32]::SetForegroundWindow($hWnd)
        Start-Sleep -Seconds 2

        # Send Ctrl+P to stop current Play mode
        $wshell = New-Object -ComObject Wscript.Shell
        Write-Output "Stopping current Play mode..."
        $wshell.SendKeys("^p")
        Start-Sleep -Seconds 3

        # Send Ctrl+P to start Play mode again (fresh load)
        Write-Output "Starting fresh Play mode to load C# gradient and latest styles..."
        $wshell.SendKeys("^p")
        Start-Sleep -Seconds 5


        # Get Window Rect
        $rect = New-Object User32+RECT
        if ([User32]::GetWindowRect($hWnd, [ref]$rect)) {
            $width = $rect.Right - $rect.Left
            $height = $rect.Bottom - $rect.Top

            if ($width -gt 0 -and $height -gt 0) {
                Add-Type -AssemblyName System.Drawing
                $Bitmap = New-Object System.Drawing.Bitmap $width, $height
                $Graphic = [System.Drawing.Graphics]::FromImage($Bitmap)
                $Graphic.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $Bitmap.Size)
                $Bitmap.Save("d:\My project\unity_editor_screenshot.png", [System.Drawing.Imaging.ImageFormat]::Png)
                $Graphic.Dispose()
                $Bitmap.Dispose()
                Write-Output "Unity window captured successfully! Width: $width, Height: $height"
                exit 0
            }
        }
    }
}

Write-Output "Failed to capture Unity window!"
exit 1
