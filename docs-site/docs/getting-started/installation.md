---
sidebar_position: 1
---

# Installation

This guide walks you through installing the KubeSQLServer Operator in your Kubernetes cluster.

## Prerequisites

- Kubernetes cluster (version 1.22+)
- `kubectl` configured to access your cluster
- `helm` 3.x (for Helm installation method)

## Option 1: Install via Helm (Recommended)

The easiest way to install the operator is using Helm:

```bash
helm upgrade -i kubesqlserver-operator \
  oci://ghcr.io/dotkube/chart/kubesqlserver-operator \
  --namespace sql-server \
  --create-namespace \
  --version 0.2.1
```

### Verify Installation

```bash
kubectl get pods -n sql-server
```

You should see the operator pod running:

```
NAME                                          READY   STATUS    RESTARTS   AGE
kubesqlserver-operator-xxxxx-yyyyy           1/1     Running   0          30s
```

## Option 2: Install via kubectl

You can also install the operator directly using kubectl:

```bash
# Create namespace
kubectl create ns sql-server

# Set as current namespace
kubectl config set-context --current --namespace=sql-server

# Apply the operator manifest
kubectl apply -f https://raw.githubusercontent.com/DotKube/KubeSQLServer-Operator/main/deploy/yaml/deploy.yaml

# Check the resources
kubectl get all
```

## Verify CRDs are Installed

After installation, verify that the Custom Resource Definitions (CRDs) are available:

```bash
kubectl get crds | grep sql-server.dotkube.io
```

You should see:

```
databases.sql-server.dotkube.io
externalsqlservers.sql-server.dotkube.io
sqlserverlogins.sql-server.dotkube.io
sqlservers.sql-server.dotkube.io
sqlserverschemas.sql-server.dotkube.io
sqlserverusers.sql-server.dotkube.io
```

## Configuration Options

### Helm Values

You can customize the installation by providing values:

```bash
helm upgrade -i kubesqlserver-operator \
  oci://ghcr.io/dotkube/chart/kubesqlserver-operator \
  --namespace sql-server \
  --create-namespace
```

## Uninstallation

### Helm

```bash
helm uninstall kubesqlserver-operator -n sql-server
kubectl delete ns sql-server
```

### kubectl

```bash
kubectl delete -f https://raw.githubusercontent.com/DotKube/KubeSQLServer-Operator/main/deploy/yaml/deploy.yaml
kubectl delete ns sql-server
```

## Next Steps

- [Quick Start Guide](./quick-start.md) - Create your first SQL Server instance
- [CRD Reference](../reference/crds/overview.md) - Learn about available resources
