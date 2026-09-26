$raw = Get-Content 'assignment_xml/word/document.xml' -Raw
$text = $raw -replace '</w:p>', "`n" -replace '<[^>]+>', ''
Set-Content assignment.txt $text

$raw = Get-Content 'proposal_xml/word/document.xml' -Raw
$text = $raw -replace '</w:p>', "`n" -replace '<[^>]+>', ''
Set-Content proposal.txt $text
