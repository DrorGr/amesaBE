# Script to edit rebase todo file
param($file)
$content = Get-Content $file
$newContent = $content | ForEach-Object {
    if ($_ -match '^pick f9b4263') {
        $_ -replace '^pick', 'edit'
    } else {
        $_
    }
}
Set-Content $file $newContent

