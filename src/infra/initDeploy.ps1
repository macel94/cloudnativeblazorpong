# Prompt securely for your SA password once…
# $saPwd = Read-Host -AsSecureString "Enter SA password for SQL (sa)"
$saPwd = ConvertTo-SecureString -String "yourStrongTemp(!)Password" -AsPlainText -Force

# Then invoke the deploy script:
. $($PSScriptRoot + '/deploy.ps1') `
  -ResourceGroupName "rg-blazorpong" `
  -Location "westeurope" `
  -SqlAdminPassword $saPwd `
  -BaseName "cnblazorpong1303072310" `
  -RepoPath "./.."