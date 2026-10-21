param([string] $OutputDirectory = (Join-Path $PSScriptRoot '..\docs\assets'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$destination = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'assets\privacydot-logo.png') -Destination $destination -Force
$logo = [Drawing.Image]::FromFile((Join-Path $root 'assets\privacydot-logo.png'))
$background = [Drawing.ColorTranslator]::FromHtml('#11161b')
$ink = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#f5f7f8'))
$muted = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#bdc6cc'))
$green = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#1ac85b'))
$orange = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#ff9800'))
$gray = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#75808c'))
try {
  foreach ($layout in @(
    @{ Name='privacydot-social.png'; Width=1200; Height=630; Square=$false },
    @{ Name='privacydot-square.png'; Width=1080; Height=1080; Square=$true },
    @{ Name='privacydot-banner.png'; Width=1600; Height=900; Square=$false }
  )) {
    $canvas = [Drawing.Bitmap]::new($layout.Width,$layout.Height)
    $graphics = [Drawing.Graphics]::FromImage($canvas)
    $fonts = @()
    try {
      $graphics.Clear($background)
      $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
      $graphics.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
      $scale = $layout.Width / 1200.0
      $graphics.ScaleTransform($scale,$scale)
      $height = $layout.Height / $scale
      $nameFont = [Drawing.Font]::new('Segoe UI',25,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
      $titleFont = [Drawing.Font]::new('Segoe UI',64,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
      $bodyFont = [Drawing.Font]::new('Segoe UI',25,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
      $smallFont = [Drawing.Font]::new('Segoe UI',21,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
      $fonts = @($nameFont,$titleFont,$bodyFont,$smallFont)
      $graphics.DrawString('PrivacyDot',$nameFont,$ink,64,44)
      if ($layout.Square) {
        $graphics.DrawImage($logo,[Drawing.RectangleF]::new(735,35,360,360))
        $graphics.DrawString("A small dot.`nA clearer picture.",$titleFont,$ink,[Drawing.RectangleF]::new(64,350,1080,180))
        $graphics.DrawString("Microphone and camera activity,`nright in your Windows tray.",$bodyFont,$muted,[Drawing.RectangleF]::new(68,560,1020,110))
        $rowY = 760
      } else {
        $graphics.DrawString("A small dot.`nA clearer picture.",$titleFont,$ink,[Drawing.RectangleF]::new(60,145,790,180))
        $graphics.DrawString("Microphone and camera activity,`nright in your Windows tray.",$bodyFont,$muted,[Drawing.RectangleF]::new(65,350,800,90))
        $graphics.DrawImage($logo,[Drawing.RectangleF]::new(855,150,280,280))
        $rowY = $height - 123
      }
      $states = @(@{Text='Microphone';Brush=$green},@{Text='Camera';Brush=$orange},@{Text='No use detected';Brush=$gray})
      $stateX = 68
      foreach ($state in $states) {
        $graphics.FillEllipse($state.Brush,$stateX,$rowY+8,15,15)
        $graphics.DrawString($state.Text,$smallFont,$ink,$stateX+27,$rowY)
        $stateX += 230
      }
      $graphics.DrawString('Free + open source  /  Windows  /  MIT License',$smallFont,$muted,65,($height-68))
      if ($layout.Square) {
        $graphics.DrawString('github.com/farflashgroup/PrivacyDot',$smallFont,$ink,65,($height-120))
      }
      $canvas.Save((Join-Path $destination $layout.Name),[Drawing.Imaging.ImageFormat]::Png)
    } finally {
      foreach ($font in $fonts) { $font.Dispose() }
      $graphics.Dispose()
      $canvas.Dispose()
    }
  }
} finally {
  $logo.Dispose()
  foreach ($brush in @($ink,$muted,$green,$orange,$gray)) { $brush.Dispose() }
}
$bundle = Join-Path $destination 'privacydot-listing-kit.zip'
$bundleFiles = @(
  (Join-Path $destination 'privacydot-logo.png'),
  (Join-Path $destination 'privacydot-social.png'),
  (Join-Path $destination 'privacydot-square.png'),
  (Join-Path $destination 'privacydot-banner.png'),
  (Join-Path $root 'docs\screenshots\popup-dark.png'),
  (Join-Path $root 'docs\screenshots\popup-light.png'),
  (Join-Path $PSScriptRoot 'directory-copy.md'),
  (Join-Path $PSScriptRoot 'product-facts.json'),
  (Join-Path $root 'LICENSE')
)
Compress-Archive -LiteralPath $bundleFiles -DestinationPath $bundle -Force
Get-ChildItem -LiteralPath $destination | Select-Object Name,Length
