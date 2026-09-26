$word = New-Object -ComObject Word.Application
$word.Visible = $false
$inputPath = (Resolve-Path 'Nory_Final_Report.html').Path
$outputPath = Join-Path $PWD 'Nory_Final_Report.docx'
$doc = $word.Documents.Open($inputPath, $false, $true)
$doc.SaveAs2($outputPath, 16)
$doc.Close($false)
$word.Quit()
Write-Output "Created $outputPath"
