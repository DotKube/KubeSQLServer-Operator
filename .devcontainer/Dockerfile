# Use the official .NET 10.0 SDK as the base image
FROM mcr.microsoft.com/dotnet/sdk:10.0

# Install required system packages first (including ca-certificates)
RUN apt-get update && apt-get install -y \
    ca-certificates \
    curl \
    wget \
    git \
    bash \
    tmux \
    apt-transport-https \
    gnupg \
    lsb-release \
    sudo \
    && update-ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Install kubectl, helm, kind, docker-cli, and task as binaries
RUN KUBECTL_VERSION=$(curl -L -s https://dl.k8s.io/release/stable.txt) \
    && curl -LO "https://dl.k8s.io/release/${KUBECTL_VERSION}/bin/linux/amd64/kubectl" \
    && install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl \
    && rm kubectl

# Install Helm from GitHub releases
# Helm may fail to install due to DNS restrictions; if so, it can be installed manually in the container
RUN (curl -fsSL https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash) || \
    echo "Helm installation skipped - install manually if needed with: curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash"

# Install Kind from GitHub releases 
RUN (curl -Lo /usr/local/bin/kind "https://github.com/kubernetes-sigs/kind/releases/download/v0.24.0/kind-linux-amd64" \
    && chmod +x /usr/local/bin/kind) || \
    echo "Kind installation skipped - install manually if needed"

RUN curl -fsSL https://download.docker.com/linux/static/stable/x86_64/docker-27.5.1.tgz -o docker.tgz \
    && tar -xzf docker.tgz --strip-components=1 docker/docker \
    && mv docker /usr/local/bin/docker \
    && rm docker.tgz

RUN curl -fsSL https://github.com/go-task/task/releases/download/v3.40.1/task_linux_amd64.tar.gz -o task.tar.gz \
    && tar -zxvf task.tar.gz task \
    && mv task /usr/local/bin/task \
    && rm task.tar.gz

# Set up a non-root user (vscode is the default user for dev containers)
ARG USERNAME=vscode
ARG USER_UID=1000
ARG USER_GID=$USER_UID

# Check if user/group exists and create if needed
RUN if getent group $USER_GID; then \
        echo "Group $USER_GID already exists"; \
    else \
        groupadd --gid $USER_GID $USERNAME; \
    fi \
    && if id -u $USER_UID > /dev/null 2>&1; then \
        echo "User $USER_UID already exists, using existing user"; \
        EXISTING_USER=$(id -un $USER_UID); \
        if [ "$EXISTING_USER" != "$USERNAME" ]; then \
            usermod -l $USERNAME $EXISTING_USER; \
            usermod -d /home/$USERNAME -m $USERNAME 2>/dev/null || true; \
        fi; \
    else \
        useradd --uid $USER_UID --gid $USER_GID -m $USERNAME; \
    fi \
    && echo $USERNAME ALL=\(root\) NOPASSWD:ALL > /etc/sudoers.d/$USERNAME \
    && chmod 0440 /etc/sudoers.d/$USERNAME

# Add vscode user to docker group for Docker socket access
RUN groupadd -f docker && usermod -aG docker $USERNAME || usermod -aG docker $(id -un $USER_UID)

USER $USERNAME

# Set the default shell to bash
SHELL ["/bin/bash", "-c"]
