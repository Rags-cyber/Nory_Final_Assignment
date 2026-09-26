$word = New-Object -ComObject Word.Application
$word.Visible = $false
$doc = $word.Documents.Open((Resolve-Path 'test-docx.html').Path, $false, $true)
$doc.SaveAs2((Join-Path $PWD 'test-docx.docx'), 16)
$doc.Close($false)
$word.Quit()
Write-Output 'done'
