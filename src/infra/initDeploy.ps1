# Prompt securely for your SA password once…
# $saPwd = Read-Host -AsSecureString "Enter SA password for SQL (sa)"
$saPwd = ConvertTo-SecureString -String "yourStrongTemp(!)Password"

# Then invoke the deploy script:
.\deploy.ps1 `
  -ResourceGroupName "rg-blazorpong" `
  -Location "westeurope" `
  -SqlAdminPassword $saPwd `
  -RepoPath "./.."
