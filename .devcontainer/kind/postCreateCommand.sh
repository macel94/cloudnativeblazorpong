#!/bin/bash
sleep 10

# Globally install sqlpackage
dotnet tool install -g microsoft.sqlpackage
dotnet tool install --global dotnet-outdated-tool

# Add .NET Core SDK tools to the PATH
echo '# Add .NET Core SDK tools
export PATH="$PATH:/root/.dotnet/tools"' >> ~/.bashrc

# # Source the updated bash profile to update the PATH for the current session
# source ~/.bash_profile

curl -L https://github.com/kubernetes/kompose/releases/download/v1.36.0/kompose-linux-amd64 -o kompose
chmod +x kompose
sudo mv ./kompose /usr/local/bin/kompose

az aks install-cli

# Create a KIND cluster with extraPortMappings for local development
kind create cluster --name blazorpong --wait 120s --config - <<EOF
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
nodes:
  - role: control-plane
    extraPortMappings:
      - containerPort: 30000
        hostPort: 6350
        protocol: TCP
      - containerPort: 30001
        hostPort: 6401
        protocol: TCP
EOF

echo "KIND cluster 'blazorpong' is ready. Use 'kubectl cluster-info' to verify."
