# KubeSQLServer Operator Dev Container

This directory contains the VS Code Dev Container configuration for the KubeSQLServer Operator project.

## What's Included

The Dev Container provides a fully configured development environment with:

### Runtime & SDKs
- **.NET 10.0 SDK** - Core runtime for the operator and related services
- **Bash** - Shell environment

### Kubernetes Tools
- **kubectl** - Kubernetes command-line tool
- **Helm** - Kubernetes package manager
- **Kind** - Kubernetes IN Docker (local cluster)

### Development Tools
- **Go-Task** - Task automation tool for running tasks defined in `Taskfile.yml`
- **Docker CLI** - Configured to use the host's Docker daemon
- **Tmux** - Terminal multiplexer (required for `quick-dev-tmux` task)
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
task --version        # Should show Task v3.40.1 or later
kubectl version --client  # Should show kubectl client version
helm version          # Should show Helm v3.x.x
kind version          # Should show kind v0.24.0
docker --version      # Should show Docker 27.5.1
tmux -V              # Should show tmux 3.4
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

## Docker Socket Access

The Dev Container is configured to mount the host's Docker socket, allowing you to:

- Build Docker images
- Run Docker containers
- Use Kind to create local Kubernetes clusters

All Docker operations run on the host's Docker daemon, so images and containers persist even after the Dev Container is stopped.

## Troubleshooting

### Helm Installation

If Helm fails to install during the initial container build, it will be automatically installed after the container starts via the `postCreateCommand`. If you still encounter issues, you can manually install it:

```bash
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
```

### Docker Socket Permission Issues

If you encounter Docker socket permission errors, ensure:

1. Docker Desktop is running (on Windows/Mac)
2. Your user has permission to access the Docker socket
3. The container has mounted the Docker socket correctly

### Rebuilding the Container

If you need to rebuild the Dev Container (e.g., after updating dependencies):

1. Open the Command Palette (Ctrl+Shift+P / Cmd+Shift+P)
2. Run "Dev Containers: Rebuild Container"

## Customization

You can customize the Dev Container by modifying:

- **`.devcontainer/devcontainer.json`** - VS Code settings, extensions, and container configuration
- **`.devcontainer/Dockerfile`** - Base image and tool installations

## Support

For issues or questions about the Dev Container setup, please file an issue on the [GitHub repository](https://github.com/DotKube/KubeSQLServer-Operator/issues).
