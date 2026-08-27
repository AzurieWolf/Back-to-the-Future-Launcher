param(
    [string]$Source = "$PSScriptRoot\launcher-icon-source.png",
    [string]$Destination = "$PSScriptRoot\launcher.ico"
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

if (-not (Test-Path -LiteralPath $Source)) {
    throw "Icon source image not found: $Source"
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$sourceImage = [System.Drawing.Bitmap]::FromFile((Resolve-Path -LiteralPath $Source))
$frames = [System.Collections.Generic.List[byte[]]]::new()

try {
    foreach ($size in $sizes) {
        $bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.DrawImage($sourceImage, 0, 0, $size, $size)

            $stream = [System.IO.MemoryStream]::new()
            try {
                $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
                $frames.Add($stream.ToArray())
            }
            finally {
                $stream.Dispose()
            }
        }
        finally {
            $graphics.Dispose()
            $bitmap.Dispose()
        }
    }
}
finally {
    $sourceImage.Dispose()
}

$destinationPath = [System.IO.Path]::GetFullPath($Destination)
$fileStream = [System.IO.File]::Create($destinationPath)
$writer = [System.IO.BinaryWriter]::new($fileStream)

try {
    $writer.Write([uint16]0)          # Reserved
    $writer.Write([uint16]1)          # ICO image type
    $writer.Write([uint16]$sizes.Count)

    $dataOffset = 6 + (16 * $sizes.Count)
    for ($index = 0; $index -lt $sizes.Count; $index++) {
        $sizeByte = if ($sizes[$index] -eq 256) { 0 } else { $sizes[$index] }
        $writer.Write([byte]$sizeByte)
        $writer.Write([byte]$sizeByte)
        $writer.Write([byte]0)        # Palette colors
        $writer.Write([byte]0)        # Reserved
        $writer.Write([uint16]1)      # Color planes
        $writer.Write([uint16]32)     # Bits per pixel
        $writer.Write([uint32]$frames[$index].Length)
        $writer.Write([uint32]$dataOffset)
        $dataOffset += $frames[$index].Length
    }

    foreach ($frame in $frames) {
        $writer.Write($frame)
    }
}
finally {
    $writer.Dispose()
    $fileStream.Dispose()
}

Write-Host "Created $destinationPath"
