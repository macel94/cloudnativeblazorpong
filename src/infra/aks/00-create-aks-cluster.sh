# Verify if already logged in to Azure
if ! az account show &> /dev/null; then
  echo "You are not logged in to Azure. Please log in using 'az login'."
  exit 1
fi

rg="fbelacca-aks-rg"
aks="fbelacca-aks-cluster-22062025"
az group create --location italynorth --name $rg

# Default
az aks create \
    --resource-group $rg \
    --name $aks \
    --location italynorth \
    --node-count 1 \
    --node-vm-size "Standard_B2as_v2" \
    --generate-ssh-keys

# Login to the AKS cluster
az aks get-credentials --resource-group $rg --name $aks

# Test using the new https://learn.microsoft.com/en-us/azure/virtual-machines/sizes/general-purpose/bpsv2-series?tabs=sizebasic
# az aks create \
#     --resource-group $rg \
#     --name $aks \
#     --location italynorth \
#     --node-count 1 \
#     --node-vm-size "Standard_B2ps_v2"

# Now we use kompose to create the Kubernetes resources from the src/docker-compose.nobuild.yml file
kompose convert -f src/docker-compose.nobuild.yml --volumes emptyDir -o src/infra/aks

# Now we can deploy the Kubernetes resources to the AKS cluster
kubectl apply -f src/infra/aks