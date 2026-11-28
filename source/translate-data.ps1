# Copiaza fisierele language\*.json in language\translate.html (intre /*DATA*/ si /*END*/),
# ca pagina de traducere sa mearga deschisa direct din folder, fara server. Rulat automat de build.ps1.
$ErrorActionPreference = 'Stop'
$lang = Join-Path (Split-Path $PSScriptRoot -Parent) 'language'
$html = Join-Path $lang 'translate.html'
if (-not (Test-Path $html)) { return }

$utf8 = New-Object System.Text.UTF8Encoding $false
$parts = @()
foreach ($f in Get-ChildItem $lang -Filter *.json | Sort-Object Name) {
    $code = $f.BaseName.ToLowerInvariant()
    if ($code -notmatch '^[a-z]{2,3}$') { continue }
    $json = [IO.File]::ReadAllText($f.FullName, $utf8).Trim()
    $parts += '"' + $code + '": ' + $json.Replace('</', '<\/')
}
$data = '{' + ($parts -join ",`n") + '}'

$text = [IO.File]::ReadAllText($html, $utf8)
$a = $text.IndexOf('/*DATA*/'); $b = $text.IndexOf('/*END*/')
if ($a -lt 0 -or $b -lt $a) { throw "translate.html: lipsesc marcajele /*DATA*/ ... /*END*/" }
$new = $text.Substring(0, $a + 8) + $data + $text.Substring($b)
if ($new -ne $text) { [IO.File]::WriteAllText($html, $new, $utf8) }
