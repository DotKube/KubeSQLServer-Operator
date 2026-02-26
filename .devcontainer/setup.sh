#!/bin/bash
set -e  # Exit on any error

echo "Installing additional dev container tools..."

# Install Kind
echo "Installing Kind v0.24.0..."
curl -Lo /usr/local/bin/kind https://github.com/kubernetes-sigs/kind/releases/download/v0.24.0/kind-linux-amd64
chmod +x /usr/local/bin/kind

# Install Go-Task
echo "Installing Go-Task v3.40.1..."
curl -fsSL https://github.com/go-task/task/releases/download/v3.40.1/task_linux_amd64.tar.gz | tar -xz
sudo mv task /usr/local/bin/task

# Install Tmux
echo "Installing Tmux..."
sudo apt-get update && sudo apt-get install -y tmux

# Verify installations
echo ""
echo "Verifying tool installations..."
echo "✓ .NET:    $(dotnet --version)"
echo "✓ Task:    $(task --version)"
echo "✓ kubectl: $(kubectl version --client --short 2>/dev/null || kubectl version --client)"
echo "✓ Helm:    $(helm version --short)"
echo "✓ Kind:    $(kind version)"
echo "✓ Docker:  $(docker --version)"
echo "✓ Tmux:    $(tmux -V)"
echo ""
echo "Dev container setup complete! 🚀"
