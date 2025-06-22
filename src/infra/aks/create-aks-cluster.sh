# Verify if already logged in to Azure
if ! az account show &> /dev/null; then
  echo "You are not logged in to Azure. Please log in using 'az login'."
  exit 1
fi

$rg = "fbelacca-aks-rg"
$aks = "fbelacca-aks-cluster-22062025"
az group create --location italynorth --name $rg

az aks create \
    --resource-group $rg \
    --name $aks \
    --location italynorth \
    --node-count 1 \
    --node-vm-size "Standard_B2s_v2"