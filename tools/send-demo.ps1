param(
    [string]$PortName = "COM20",
    [int]$BaudRate = 115200,
    [int]$IntervalMs = 10,
    [int]$Count = 0
)

$ErrorActionPreference = "Stop"

$available = [System.IO.Ports.SerialPort]::GetPortNames()
Write-Host "Available ports: $($available -join ', ')"
Write-Host "Opening $PortName, $BaudRate 8-N-1 ..."

$port = [System.IO.Ports.SerialPort]::new(
    $PortName,
    $BaudRate,
    [System.IO.Ports.Parity]::None,
    8,
    [System.IO.Ports.StopBits]::One)
$port.WriteTimeout = 1000

$sent = 0
try {
    $port.Open()
    Write-Host "Sending AA 01 XX frames to $PortName. Press Ctrl+C to stop."

    while ($Count -le 0 -or $sent -lt $Count) {
        # value 17.0..25.0 -> one byte 170..250
        $value = 21 + 4 * [Math]::Sin($sent * 0.06)
        $raw = [byte][Math]::Round($value * 10)

        $frame = [byte[]]::new(3)
        $frame[0] = 0xAA
        $frame[1] = 0x01
        $frame[2] = $raw

        $port.Write($frame, 0, $frame.Length)
        $sent++

        if ($sent % 50 -eq 0) {
            Write-Host ("sent={0} value={1:F1} raw=0x{2:X2}" -f $sent, $value, $raw)
        }

        Start-Sleep -Milliseconds $IntervalMs
    }
}
finally {
    if ($port.IsOpen) {
        $port.Close()
    }
    $port.Dispose()
    Write-Host "Done. sent=$sent frame(s)."
}
