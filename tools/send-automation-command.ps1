param(
    [Parameter(Mandatory = $true)]
    [string]$Command,

    [string]$PipeName = "ElgatoCaptureAutomation",
    [string]$AuthToken = "",
    [string]$PayloadJson = "{}",
    [int]$ConnectTimeoutMs = 5000
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-CommandValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $parsed = 0
    if ([int]::TryParse($Value, [ref]$parsed)) {
        return $parsed
    }

    switch ($Value.Trim().ToLowerInvariant()) {
        "authenticate" { return 0 }
        "getsnapshot" { return 1 }
        "getdiagnostics" { return 2 }
        "refreshdevices" { return 3 }
        "selectdevice" { return 4 }
        "selectaudioinputdevice" { return 5 }
        "setcustomaudioinput" { return 6 }
        "setresolution" { return 7 }
        "setframerate" { return 8 }
        "setrecordingformat" { return 9 }
        "setquality" { return 10 }
        "setcustombitrate" { return 11 }
        "sethdrenabled" { return 12 }
        "setaudioenabled" { return 13 }
        "setaudiopreviewenabled" { return 14 }
        "setoutputpath" { return 15 }
        "setpreviewenabled" { return 16 }
        "setrecordingenabled" { return 17 }
        "armclose" { return 18 }
        "windowaction" { return 19 }
        "waitforcondition" { return 20 }
        "verifylastrecording" { return 21 }
        "assertsnapshot" { return 22 }
        "settruehdrpreviewenabled" { return 23 }
        "probevideosource" { return 24 }
        "probepreviewcolor" { return 25 }
        "capturepreviewframe" { return 26 }
        "capturewindowscreenshot" { return 27 }
        "setvideoformat" { return 28 }
        default { throw "Unknown automation command '$Value'." }
    }
}

$commandValue = Resolve-CommandValue -Value $Command

$payload = $null
if ([string]::IsNullOrWhiteSpace($PayloadJson)) {
    $payload = @{}
}
else {
    $payload = $PayloadJson | ConvertFrom-Json
}

$request = [ordered]@{
    command = $commandValue
    correlationId = [Guid]::NewGuid().ToString("N")
    authToken = $AuthToken
    payload = $payload
}

$json = $request | ConvertTo-Json -Depth 20 -Compress

$client = [System.IO.Pipes.NamedPipeClientStream]::new(
    ".",
    $PipeName,
    [System.IO.Pipes.PipeDirection]::InOut,
    [System.IO.Pipes.PipeOptions]::None)

try {
    $client.Connect($ConnectTimeoutMs)

    $writer = [System.IO.StreamWriter]::new(
        $client,
        [System.Text.UTF8Encoding]::new($false),
        4096,
        $true)
    $writer.AutoFlush = $true
    $writer.WriteLine($json)

    $reader = [System.IO.StreamReader]::new(
        $client,
        [System.Text.Encoding]::UTF8,
        $false,
        4096,
        $true)

    $response = $reader.ReadLine()
    if ([string]::IsNullOrWhiteSpace($response)) {
        throw "No response received from automation pipe."
    }

    $response
}
finally {
    $client.Dispose()
}
