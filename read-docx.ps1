param([string]$Path)
[xml]$xml = Get-Content $Path -Raw
foreach ($paragraph in $xml.document.body.p) {
    $text = foreach ($run in $paragraph.r) { $run.'#text' }
    ($text -join '')
}
