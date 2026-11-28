# Compileaza ShareWatch.exe (un singur fisier, fara setup) cu csc.exe din .NET Framework 4 (inclus in Windows).
# Optiune: -Test  => varianta fara cerere de administrator (doar pentru testarea interfetei)
param([switch]$Test)
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
Add-Type -AssemblyName System.Drawing

# --- icon pentru exe (PNG intr-un .ico)
$bmp = New-Object System.Drawing.Bitmap 256,256
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(214,150,0))), 24,32,100,48)
$g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(242,183,5))), 24,72,208,144)
$g.FillEllipse([System.Drawing.Brushes]::White, 120,112,128,128)
$g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(61,220,132))), 136,128,96,96)
$ms = New-Object System.IO.MemoryStream
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$png = $ms.ToArray()
$ico = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $ico
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]1)
$w.Write([byte]0); $w.Write([byte]0); $w.Write([byte]0); $w.Write([byte]0)
$w.Write([uint16]1); $w.Write([uint16]32); $w.Write([uint32]$png.Length); $w.Write([uint32]22)
$w.Write($png); $w.Flush()
$icoPath = Join-Path $here 'app.ico'
[IO.File]::WriteAllBytes($icoPath, $ico.ToArray())

$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$out = if ($Test) { 'ShareWatch-test.exe' } else { 'ShareWatch.exe' }
$args = @('/nologo','/target:winexe','/optimize+','/codepage:65001',"/win32icon:$icoPath","/out:$(Join-Path $here $out)",
  '/r:System.dll','/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Core.dll','/r:Microsoft.CSharp.dll','/r:System.Web.Extensions.dll')
if ($Test) { $args += '/define:TEST' }
if (-not $Test) { $args += "/win32manifest:$(Join-Path $here 'app.manifest')" }
$args += (Join-Path $here 'ShareWatch.cs')
$args += (Join-Path $here 'MainForm.cs')
$args += (Join-Path $here 'Toast.cs')
$args += (Join-Path $here 'Tray.cs')
$args += (Join-Path $here 'Support.cs')
$args += (Join-Path $here 'ShareDialog.cs')
$args += (Join-Path $here 'BlockList.cs')
$args += (Join-Path $here 'Firewall.cs')
if ($Test) { $args += (Join-Path $here 'Demo.cs') }
& $csc @args
if ($LASTEXITCODE -ne 0) { throw "Compilare esuata" }
# pagina de traducere (language\translate.html) primeste fisierele de limba actuale
& (Join-Path $here 'translate-data.ps1')
Get-Item (Join-Path $here $out) | Select-Object Name, Length, @{n='Versiune';e={$_.VersionInfo.FileVersion}}





