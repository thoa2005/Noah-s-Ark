Get-Process | Where-Object { $_.MainWindowTitle -ne "" } | Select-Object -Property Name, MainWindowTitle | Format-Table -AutoSize
