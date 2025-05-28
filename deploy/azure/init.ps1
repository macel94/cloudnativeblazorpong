# Set the current directory to the script's location for ps core even when psscriptroot is not set
if (-not $PSScriptRoot) {
    $PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

Set-Location -Path $PSScriptRoot


# Deploy everything containerized
.\deploytoaca.ps1 -ResourceGroupName "rg-fb-cnblazorpong-dev" -Location "swedencentral" -ComposePath "../../src/docker-compose.nobuild.yml"