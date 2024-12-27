# KubeSQLServer Operator

KubeSQLServer Operator is a completely free and open-source (MIT licensed) Kubernetes operator designed to help you run and manage Microsoft SQL Server seamlessly.

This project is intended to be an open-source alternative to D2HI's Dx Operator, which requires a license. KubeSQLServer Operator aims to provide a no-license-required solution for SQL Server management in Kubernetes.

> **Note:** This project is in the very early stages of development and is not yet ready for distribution. Stay tuned for updates as the project progresses!

---

## Planned Features and Roadmap

Here are the planned features and milestones for KubeSQLServer Operator:

- **API and CRD Scope Definition**  
  Design and define the scope of APIs and Custom Resource Definitions (CRDs).

- **Testing Strategies**  
  Establish robust testing strategies for the operator, CRDs, and related components.

- **Helm Chart**  
  Create and host a Helm chart for easy deployment of the operator.

- **Documentation**  
  Develop comprehensive documentation and decide how it will be hosted.

- **Base Container Images**  
  Build base images for the operator:
  - Rootless SQL Server container instances (Ubuntu and RHEL-based).
  - Rootless MS SQL client container instance.

- **CLI Tooling**  
  Develop a CLI for managing the operator and resources.

---

## Development Workflow

This repository contains the following components:

1. **KubeOps-based .NET Operator**  
   - Located in `src/OperatorTemplate.ApiService`.  
   - Implements controllers and CRDs to manage custom Kubernetes resources.

2. **Helm Chart**  
   - Located in `operator-chart`.  
   - Includes CRD definitions, RBAC configurations, and deployment manifests for the operator.

3. **Taskfile Workflow**  
   - Simplifies cluster setup, CRD management, and operator deployment through automated tasks.

---

## Local Development - Key Commands

### Local Development (`task quick-dev`)
Run the operator **locally** on your laptop while applying necessary CRDs and creating an instance in a Kind cluster.  
This is ideal for debugging and real-time development.

```bash
task quick-dev
```

### In-Cluster Deployment (`task quick-deploy`)
Build and deploy the operator **to a Kind cluster**, replicating a production-like environment for testing.

```bash
task quick-deploy
```

---

## Additional Commands

- **Cluster Management**  
  - Create: `task create-cluster`  
  - Delete: `task delete-cluster`

- **CRD Management**  
  - Generate and copy CRDs: `task create-crds-and-copy`  
  - Apply CRDs: `task apply-crds-from-helm-chart`

- **Helm Deployment**  
  - Install: `task install-helm-chart`  
  - Uninstall: `task uninstall-helm-chart`
