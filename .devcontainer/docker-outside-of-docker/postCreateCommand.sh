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