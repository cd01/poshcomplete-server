$candidates = @()

foreach ($c in $(0 .. 25 | % { [System.Convert]::ToChar([System.Convert]::ToInt32(97 + $_, 10)) }))
{
    $completeWords = [System.Management.Automation.CommandCompletion]::CompleteInput($c + "*", 2, $null).CompletionMatches |
                     Select-Object CompletionText, ResultType, ToolTip |
                     where { $_.CompletionText.Contains("-") } |
                     where { -not $_.CompletionText.Contains(".") }

    foreach ($completeWord in $completeWords)
    {
        $candidates += @{
            "CompletionText" = $($completeWord.CompletionText -replace "\\","\\");
            "ResultType" = "[" + $completeWord.ResultType + "]";
            "ToolTip" = $($completeWord.ToolTip -replace "\\","\\") -replace "`r`n","";
        }
    }
}

$candidates | ConvertTo-Json -Compress > .\dict.json


# vim:set et ts=4 sts=0 sw=4 ff=dos:

