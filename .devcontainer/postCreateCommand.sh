#!/bin/bash
sleep 10

# Globally install sqlpackage
dotnet tool install -g microsoft.sqlpackage

# Add .NET Core SDK tools to the PATH
echo '# Add .NET Core SDK tools
export PATH="$PATH:/root/.dotnet/tools"' >> ~/.bashrc

# # Source the updated bash profile to update the PATH for the current session
# source ~/.bash_profile