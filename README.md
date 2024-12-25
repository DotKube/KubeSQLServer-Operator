
# OperatorTemplate Development Workflow

This repository contains a **KubeOps-based .NET Operator** and a Helm chart for deploying it. The setup supports both local and in-cluster workflows for development and testing.

---

## Components

1. **KubeOps Operator**  
   - Located in `src/OperatorTemplate.ApiService`.
   - Manages custom Kubernetes resources with controllers and CRDs.

2. **Helm Chart**  
   - Located in `operator-chart`.
   - Includes CRD definitions, RBAC configurations, and a deployment for the operator.

3. **Taskfile Workflow**  
   - Simplifies cluster management, CRD handling, and operator deployment with convenient tasks.

---

## Key Commands

### `task quick-dev`
Runs the operator **locally on your laptop** while applying all necessary CRDs and creating an instance in the Kind cluster.  
Ideal for real-time debugging and development.

```bash
task quick-dev
```

### `task quick-deploy`
Builds and deploys the operator **into the Kind cluster**.  
Perfect for testing the operator as it would run in production.

```bash
task quick-deploy
```

---

## Other Useful Tasks
- **Cluster Management**: `create-cluster`, `delete-cluster`
- **CRD Management**: `create-crds-and-copy`, `apply-crds-from-helm-chart`
- **Helm Deployment**: `install-helm-chart`, `uninstall-helm-chart`

---

## Summary
This repo streamlines Kubernetes operator development. Use:
- **`quick-dev`** for local development.
- **`quick-deploy`** for in-cluster testing.  
These commands simplify the workflow so you can focus on building your operator!