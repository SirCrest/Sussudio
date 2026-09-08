param(
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$Ffmpeg = 'ffmpeg.exe',
    [string]$Ffprobe = 'ffprobe.exe'
)
$ErrorActionPreference = 'Stop'
if ((Test-Path -LiteralPath $OutputDirectory) -and (Get-ChildItem -LiteralPath $OutputDirectory -Force | Measure-Object).Count -gt 0) {
    throw 'Generate fixtures into a new or empty directory, then review before replacing the committed corpus.'
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$mediaRoot = (Resolve-Path -LiteralPath $OutputDirectory).Path
$Ffmpeg = (Get-Command $Ffmpeg -ErrorAction Stop).Source
$Ffprobe = (Get-Command $Ffprobe -ErrorAction Stop).Source
$commandLog = [System.Collections.Generic.List[object]]::new()

function Make-Clip {
    param([string]$Name, [string]$VideoDuration = '2', [string[]]$AudioDurations = @(), [string]$Codec = 'libx264', [switch]$Hdr)
    $destination = Join-Path $mediaRoot $Name
    $arguments = @('-hide_banner','-loglevel','error','-nostdin','-n','-f','lavfi','-i',"color=c=black:s=64x64:r=50:d=$VideoDuration")
    $tone = 440
    foreach ($duration in $AudioDurations) {
        $arguments += @('-f','lavfi','-i',"sine=frequency=${tone}:sample_rate=48000:duration=$duration")
        $tone += 440
    }
    $arguments += @('-map','0:v')
    for ($index = 1; $index -le $AudioDurations.Count; $index++) { $arguments += @('-map',"${index}:a") }
    $arguments += @('-c:v',$Codec,'-threads:v','1')
    if ($Codec -eq 'libx264') { $arguments += @('-preset','ultrafast','-pix_fmt','yuv420p') }
    if ($Codec -eq 'libx265') {
        $x265Parameters = 'pools=1:frame-threads=1:log-level=error'
        if ($Hdr) { $x265Parameters += ':colorprim=bt2020:transfer=smpte2084:colormatrix=bt2020nc' }
        $arguments += @('-preset','ultrafast','-x265-params',$x265Parameters,'-pix_fmt','yuv420p10le')
    }
    if ($Codec -eq 'libaom-av1') { $arguments += @('-cpu-used','8','-row-mt','0','-pix_fmt','yuv420p') }
    if ($Hdr) { $arguments += @('-color_primaries','bt2020','-color_trc','smpte2084','-colorspace','bt2020nc') }
    if ($AudioDurations.Count -gt 0) { $arguments += @('-c:a','aac','-b:a','32k') }
    $arguments += @('-movflags','+faststart',$destination)
    & $Ffmpeg @arguments 2> (Join-Path $mediaRoot ($Name + '.generate.log'))
    if ($LASTEXITCODE -ne 0) { throw "FFmpeg failed for $Name with exit $LASTEXITCODE" }
    $commandLog.Add([pscustomobject]@{Executable=$Ffmpeg; Arguments=$arguments})
}

Make-Clip 'h264-video.mp4'
Make-Clip 'h264-program.mp4' -AudioDurations @('2')
Make-Clip 'h264-dual.mp4' -AudioDurations @('2','2')
Make-Clip 'hevc-hdr-explicit.mp4' -Codec 'libx265' -Hdr
Make-Clip 'hevc-sdr.mp4' -Codec 'libx265'
Make-Clip 'av1-video.mp4' -Codec 'libaom-av1'
Make-Clip 'h264-floor-boundary.mp4' -VideoDuration '0.1'
Make-Clip 'h264-percent-boundary.mp4' -VideoDuration '9.5'
Make-Clip 'h264-cap-boundary.mp4' -VideoDuration '42'
Make-Clip 'h264-audio-short-boundary.mp4' -AudioDurations @('1.5')
Make-Clip 'h264-audio-too-short.mp4' -AudioDurations @('1.48')
Make-Clip 'h264-audio-long-boundary.mp4' -AudioDurations @('2.5')
Make-Clip 'h264-audio-too-long.mp4' -AudioDurations @('2.52')

[IO.File]::WriteAllBytes((Join-Path $mediaRoot 'empty.mp4'), [byte[]]@())
[IO.File]::WriteAllBytes((Join-Path $mediaRoot 'garbage.mp4'), [Text.Encoding]::UTF8.GetBytes('This is not a media container.'))
$baseBytes = [IO.File]::ReadAllBytes((Join-Path $mediaRoot 'h264-video.mp4'))
$offset = 0
# Preserve ftyp/moov and the mdat header, but remove every packet payload.
while ($offset -lt $baseBytes.Length - 8) {
    $length = ([long]$baseBytes[$offset] -shl 24) + ([long]$baseBytes[$offset+1] -shl 16) + ([long]$baseBytes[$offset+2] -shl 8) + [long]$baseBytes[$offset+3]
    $kind = [Text.Encoding]::ASCII.GetString($baseBytes, $offset+4, 4)
    if ($kind -eq 'mdat') { break }
    if ($length -lt 8) { throw 'Unexpected MP4 top-level box' }
    $offset += $length
}
if ($kind -ne 'mdat') { throw 'mdat not found' }
[IO.File]::WriteAllBytes((Join-Path $mediaRoot 'header-without-packets.mp4'), $baseBytes[0..($offset+7)])

$manifest = foreach ($file in Get-ChildItem -LiteralPath $mediaRoot -Filter '*.mp4' | Sort-Object Name) {
    $metadata = $null
    if ($file.Name -notin @('empty.mp4','garbage.mp4','header-without-packets.mp4')) {
        $metadataText = & $Ffprobe -v error -show_entries 'stream=index,codec_name,codec_type,pix_fmt,width,height,duration,color_space,color_transfer,color_primaries,time_base,nb_frames' -of json $file.FullName
        if ($LASTEXITCODE -ne 0) { throw "ffprobe failed for $($file.Name)" }
        $metadata = ($metadataText -join [Environment]::NewLine) | ConvertFrom-Json
        & $Ffmpeg -hide_banner -loglevel error -nostdin -i $file.FullName -map 0 -f null NUL 2> (Join-Path $mediaRoot ($file.Name + '.decode.log'))
        if ($LASTEXITCODE -ne 0) { throw "Decode failed for $($file.Name)" }
    }
    [pscustomobject]@{Name=$file.Name;Length=$file.Length;Sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash;Metadata=$metadata}
}
$manifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $mediaRoot 'manifest.json') -Encoding utf8
[pscustomobject]@{
    FfmpegVersion = (& $Ffmpeg -version | Select-Object -First 1)
    FfprobeVersion = (& $Ffprobe -version | Select-Object -First 1)
    Commands = $commandLog
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $mediaRoot 'generation.json') -Encoding utf8
$manifest | Select-Object Name,Length
