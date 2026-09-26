$ErrorActionPreference = 'Stop'
$root = Get-Location
$temp = Join-Path $root 'report_docx_temp'
$zipPath = Join-Path $root 'Nory_Final_Report.docx'

if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }
Copy-Item 'assignment_xml' $temp -Recurse -Force

function Escape-Xml([string]$text) {
    [System.Security.SecurityElement]::Escape($text)
}

function Get-InnerText([string]$html) {
    $text = $html -replace '<[^>]+>', ''
    $text = $text -replace '&nbsp;', ' '
    $text = $text -replace '&lt;', '<'
    $text = $text -replace '&gt;', '>'
    $text = $text -replace '&amp;', '&'
    $text = $text -replace '&quot;', '"'
    $text = $text.Trim()
    return $text
}

function Add-TextParagraph([string]$text, [string]$type = 'body') {
    $align = 'both'
    $size = '24'
    $font = 'Times New Roman'
    $spacing = '<w:spacing w:after="120" w:line="360" w:lineRule="auto"/>'
    $ind = '<w:ind w:firstLine="720"/>'
    if ($type -eq 'h1') { $size = '36'; $spacing = '<w:spacing w:before="360" w:after="240"/>'; $ind = ''; $align = 'left' }
    elseif ($type -eq 'h2') { $size = '32'; $spacing = '<w:spacing w:before="240" w:after="180"/>'; $ind = ''; $align = 'left' }
    elseif ($type -eq 'h3') { $size = '28'; $spacing = '<w:spacing w:before="180" w:after="120"/>'; $ind = ''; $align = 'left' }
    elseif ($type -eq 'bullet') { $ind = '<w:ind w:left="720" w:hanging="360"/>'; $align = 'both' }
    elseif ($type -eq 'code') { $size = '20'; $font = 'Consolas'; $spacing = '<w:spacing w:after="60"/>'; $ind = '<w:ind w:left="720"/>'; $align = 'left' }
    elseif ($type -eq 'center') { $ind = ''; $align = 'center'; $spacing = '<w:spacing w:after="120"/>' }

    $runs = ''
    if ($type -eq 'bullet') {
        $runs += '<w:r><w:rPr><w:b/></w:rPr><w:t xml:space="preserve">•  </w:t></w:r>'
    }
    $runs += '<w:r><w:rPr><w:rFonts w:ascii="' + $font + '" w:hAnsi="' + $font + '"/><w:sz w:val="' + $size + '"/></w:rPr><w:t xml:space="preserve">' + (Escape-Xml $text) + '</w:t></w:r>'

    return '<w:p><w:pPr>' + $spacing + $ind + '<w:jc w:val="' + $align + '"/></w:pPr>' + $runs + '</w:p>'
}

function Add-PageBreak {
    return '<w:p><w:r><w:br w:type="page"/></w:r></w:p>'
}

function Add-Image([string]$rId, [int]$cx = 1800000, [int]$cy = 1800000, [string]$name = 'APU Logo') {
    return @"
<w:p><w:pPr><w:jc w:val="center"/></w:pPr><w:r><w:drawing>
  <wp:inline distT="0" distB="0" distL="0" distR="0">
    <wp:extent cx="$cx" cy="$cy"/><wp:effectExtent l="0" t="0" r="0" b="0"/>
    <wp:docPr id="1" name="$name"/><wp:cNvGraphicFramePr/>
    <a:graphic xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">
      <a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/picture">
        <pic:pic xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture">
          <pic:nvPicPr><pic:cNvPr id="1" name="$name"/><pic:cNvPicPr/></pic:nvPicPr>
          <pic:blipFill><a:blip r:embed="$rId"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill>
          <pic:spPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="$cx" cy="$cy"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></pic:spPr>
        </pic:pic>
      </a:graphicData>
    </a:graphic>
  </wp:inline>
</w:drawing></w:r></w:p>
"@
}

$html = Get-Content 'Nory_Final_Report.html' -Raw
$body = New-Object System.Text.StringBuilder
[void]$body.Append((Add-Image 'rId8'))

$pattern = '(?s)<(h1|h2|h3|p|li|pre|tr)[^>]*>(.*?)</\1>'
$matches = [regex]::Matches($html, $pattern)
foreach ($match in $matches) {
    $tag = $match.Groups[1].Value.ToLower()
    $inner = $match.Groups[2].Value
    if ($tag -eq 'h1') {
        [void]$body.Append((Add-TextParagraph (Get-InnerText $inner), 'h1'))
    } elseif ($tag -eq 'h2') {
        [void]$body.Append((Add-TextParagraph (Get-InnerText $inner), 'h2'))
    } elseif ($tag -eq 'h3') {
        [void]$body.Append((Add-TextParagraph (Get-InnerText $inner), 'h3'))
    } elseif ($tag -eq 'p') {
        [void]$body.Append((Add-TextParagraph (Get-InnerText $inner), 'body'))
    } elseif ($tag -eq 'li') {
        [void]$body.Append((Add-TextParagraph (Get-InnerText $inner), 'bullet'))
    } elseif ($tag -eq 'pre') {
        foreach ($line in ((Get-InnerText $inner) -split "`r?`n")) {
            [void]$body.Append((Add-TextParagraph $line, 'code'))
        }
    } elseif ($tag -eq 'tr') {
        $cells = [regex]::Matches($inner, '<t[dh][^>]*>(.*?)</t[dh]>', 'Singleline') | ForEach-Object { Get-InnerText $_.Groups[1].Value }
        if ($cells.Count -gt 0) {
            [void]$body.Append((Add-TextParagraph ($cells -join ' | '), 'body'))
        }
    }
}

$documentXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"
            xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"
            xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing"
            xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main"
            xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture"
            xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006">
  <w:body>
    $($body.ToString())
    <w:sectPr>
      <w:headerReference w:type="default" r:id="rId10"/>
      <w:footerReference w:type="default" r:id="rId12"/>
      <w:titlePg/>
      <w:pgSz w:w="11906" w:h="16838"/>
      <w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" w:header="720" w:footer="720" w:gutter="0"/>
      <w:cols w:space="720"/>
      <w:docGrid w:linePitch="360"/>
    </w:sectPr>
  </w:body>
</w:document>
"@

Set-Content -Path (Join-Path $temp 'word/document.xml') -Value $documentXml -Encoding UTF8

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($temp, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)
Write-Output "Created $zipPath"
