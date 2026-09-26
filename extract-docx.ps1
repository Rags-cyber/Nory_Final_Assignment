$word = New-Object -ComObject Word.Application
$word.Visible = $false
foreach ($file in @('CT050-3-2 WAPP_Assignment_Question_April_2025.docx','Nory_Proposal_Report_.docx')) {
    $doc = $word.Documents.Open((Resolve-Path $file).Path, $false, $true)
    $out = [IO.Path]::ChangeExtension($file, '.txt')
    $doc.SaveAs([ref](Join-Path $pwd $out), [ref]2)
    $doc.Close($false)
}
$word.Quit()
Get-ChildItem *.txt | Select-Object Name, Length
