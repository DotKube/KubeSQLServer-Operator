# KubeSQLServer Operator Dev Container

This directory contains the VS Code Dev Container configuration for the KubeSQLServer Operator project.

## What's Included

The Dev Container provides a fully configured development environment with:

### Base Image & Features

- **Base Image**: `mcr.microsoft.com/devcontainers/dotnet:1-10.0` - Official Microsoft Dev Container image for .NET 10.0
- **Docker-in-Docker**: Official dev container feature providing isolated Docker daemon
- **kubectl & Helm**: Official dev container feature for Kubernetes tooling

### Runtime & SDKs
- **.NET 10.0 SDK** - Core runtime for the operator and related services
- **Bash** - Shell environment

### Kubernetes Tools
- **kubectl** - Kubernetes command-line tool (latest stable)
- **Helm** - Kubernetes package manager (latest stable)
- **Kind** - Kubernetes in Docker (v0.24.0, installed via postCreateCommand)

### Development Tools
- **Go-Task** - Task automation tool (v3.40.1, installed via postCreateCommand)
- **Docker** - Docker-in-Docker for isolated container operations
- **Tmux** - Terminal multiplexer (installed via postCreateCommand)
- **Git** - Version control

### VS Code Extensions

The Dev Container automatically installs the following extensions:

- **C# Dev Kit** - Full C# development experience
- **Kubernetes** - Kubernetes resource management
- **Docker** - Docker container management
- **YAML & JSON Support** - Enhanced YAML and JSON editing
- **EditorConfig** - Maintain consistent coding styles

## Getting Started

### Prerequisites

- [Visual Studio Code](https://code.visualstudio.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) or Docker Engine
- [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) for VS Code

### Opening the Project

1. Open the project folder in VS Code
2. When prompted, click "Reopen in Container" (or run the command "Dev Containers: Reopen in Container" from the Command Palette)
3. Wait for the container to build and initialize (first time may take a few minutes)
4. Once ready, you'll have a fully configured development environment!

### Verifying Installation

After the container starts, all tools should be available. You can verify by running:

```bash
dotnet --version      # Should show .NET 10.0.x
task --version        # Should show Task v3.40.1
kubectl version --client  # Should show kubectl client version
helm version          # Should show Helm v3.x.x
kind version          # Should show kind v0.24.0
docker --version      # Should show Docker version
tmux -V              # Should show tmux version
```

## Using the Dev Container

### Running Tasks

The project uses Go-Task for automation. List all available tasks:

```bash
task --list-all
```

### Local Development

Start local development with automatic CRD creation and cluster setup:

```bash
task quick-dev
```

Or use tmux for a split-screen experience:

```bash
task quick-dev-tmux
```

### Building and Testing

```bash
# Build the operator
task build-operator-image

# Create CRDs
task create-crds-and-copy

# Deploy to local cluster
task quick-deploy
```

## Docker-in-Docker

The Dev Container uses Docker-in-Docker (DinD) instead of mounting the host's Docker socket. This provides:

- **Isolation**: Container operations don't affect the host's Docker environment
- **Consistency**: Same Docker environment across all development machines
- **Security**: Better isolation between dev container and host system

Note that Docker images and containers created inside the dev container are isolated from the host. If you need to share images between the dev container and host, you can use a container registry or export/import images.

## Troubleshooting

### First-Time Setup

The first time you open the project in a Dev Container, it will:
1. Pull the base image (~2GB)
2. Install Docker-in-Docker and kubectl/Helm features
3. Run `.devcontainer/setup.sh` to install Kind, Task, and Tmux

This process typically takes 3-5 minutes depending on your internet connection.

### Tool Installation

Additional tools (Kind, Task, Tmux) are installed via the `.devcontainer/setup.sh` script after the container is created. The script includes error handling and will exit if any installation fails. If you need to re-run the setup, execute:

```bash
bash .devcontainer/setup.sh
```

Or manually install individual tools:

```bash
# Install Kind
curl -Lo /usr/local/bin/kind https://github.com/kubernetes-sigs/kind/releases/download/v0.24.0/kind-linux-amd64
chmod +x /usr/local/bin/kind

# Install Task
curl -fsSL https://github.com/go-task/task/releases/download/v3.40.1/task_linux_amd64.tar.gz | tar -xz
sudo mv task /usr/local/bin/task

# Install Tmux
sudo apt-get update && sudo apt-get install -y tmux
```

### Rebuilding the Container

If you need to rebuild the Dev Container (e.g., after updating dependencies):

1. Open the Command Palette (Ctrl+Shift+P / Cmd+Shift+P)
2. Run "Dev Containers: Rebuild Container"

## Customization

You can customize the Dev Container by modifying:

- **`.devcontainer/devcontainer.json`** - VS Code settings, extensions, features, and container configuration

The configuration uses official dev container features, which can be customized by adding options to the `features` section. See the [dev container features documentation](https://containers.dev/features) for available options.

## Support

For issues or questions about the Dev Container setup, please file an issue on the [GitHub repository](https://github.com/DotKube/KubeSQLServer-Operator/issues).
