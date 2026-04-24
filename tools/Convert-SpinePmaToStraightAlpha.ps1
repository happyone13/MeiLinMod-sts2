param(
    [Parameter(Mandatory = $true)]
    [string]$ImagePath,

    [string]$OutputPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$fullPath = [System.IO.Path]::GetFullPath($ImagePath)
if (-not (Test-Path -LiteralPath $fullPath)) {
    throw "Image not found: $fullPath"
}

$targetPath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $fullPath
}
else {
    [System.IO.Path]::GetFullPath($OutputPath)
}

$source = [System.Drawing.Bitmap]::new($fullPath)
try {
    $converted = [System.Drawing.Bitmap]::new(
        $source.Width,
        $source.Height,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

    try {
        for ($y = 0; $y -lt $source.Height; $y++) {
            for ($x = 0; $x -lt $source.Width; $x++) {
                $pixel = $source.GetPixel($x, $y)
                $alpha = [int]$pixel.A

                if ($alpha -le 0) {
                    $converted.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
                    continue
                }

                $red = [Math]::Min(255, [Math]::Round($pixel.R * 255.0 / $alpha))
                $green = [Math]::Min(255, [Math]::Round($pixel.G * 255.0 / $alpha))
                $blue = [Math]::Min(255, [Math]::Round($pixel.B * 255.0 / $alpha))

                $converted.SetPixel(
                    $x,
                    $y,
                    [System.Drawing.Color]::FromArgb($alpha, [int]$red, [int]$green, [int]$blue))
            }
        }

        $tempPath = [System.IO.Path]::Combine(
            [System.IO.Path]::GetDirectoryName($targetPath),
            ([System.IO.Path]::GetFileNameWithoutExtension($targetPath) + '.straight-alpha.tmp.png'))

        $converted.Save($tempPath, [System.Drawing.Imaging.ImageFormat]::Png)
        if ([string]::Equals($targetPath, $fullPath, [System.StringComparison]::OrdinalIgnoreCase) -and
            (Test-Path -LiteralPath $targetPath)) {
            Remove-Item -LiteralPath $targetPath -Force
        }
        Move-Item -LiteralPath $tempPath -Destination $targetPath -Force
    }
    finally {
        $converted.Dispose()
    }
}
finally {
    $source.Dispose()
}
