param([ValidateSet('Core', 'UI', 'Full')][string]$Suite = 'Core')
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Test.Common.ps1')
Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    Invoke-TestCheck 'English-only PowerShell source and syntax' { Test-PowerShellSource (Split-Path -Parent $PSScriptRoot) }
    Invoke-TestCheck 'Publishing safeguards' {
        Invoke-TestCommand 'powershell.exe' @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', 'tools/Test-Publish.ps1')
    }
    $arguments = @('run', '--project', 'ReciteWords.Tests', '-c', 'Release')
    if ($Suite -ne 'Core') { $arguments += @('--', '--ui-qa') }
    Invoke-TestCheck "Application suite: $Suite" { Invoke-TestCommand 'dotnet' $arguments }
    Complete-TestRun
}
finally { Pop-Location }
