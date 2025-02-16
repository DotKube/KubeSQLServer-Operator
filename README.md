# KubeSQLServer Operator

KubeSQLServer Operator is a completely free and open-source (MIT licensed) Kubernetes operator designed to help you run and manage Microsoft SQL Server seamlessly.

This project is intended to be an open-source alternative to D2HI's Dx Operator, which requires a license [D2HI link](https://support.dh2i.com/dxoperator/guides/dxoperator-qsg/).

### GitOpsify local/staging/production SQL Servers or exisiting SQL Server instances

```yaml

apiVersion: v1
kind: Namespace
metadata:
  name: sqlserver-example

---

apiVersion: v1
kind: Secret
metadata:
  name: sqlserver-secret
  namespace: sqlserver-example
type: Opaque
stringData:
  password: JoeMontana4292#

# or 

apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: sqlserver-secret
  namespace: sqlserver-example
spec:
  refreshInterval: "1h"
  secretStoreRef:
    name: sqlserver-secret-store
    kind: ClusterSecretStore
  target:
    name: sqlserver-secret
    creationPolicy: Owner
  data:
    - secretKey: password
      remoteRef:
        key: /sqlserver/password



---

apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServer
metadata:
  name: sqlserver-instance
  namespace: sqlserver-example
spec:
  version: "2022"
  storageClass: "longhorn"
  storageSize: "6Gi"
  secretName: sqlserver-secret
  enableHighAvailibility: true
  enableFullTextSearch: true
  serviceType: LoadBalancer

# or 

apiVersion: sql-server.dotkube.io/v1alpha1
kind: ExternallyManagedSQLServer
metadata:
  name: external-sqlserver-instance
  namespace: sqlserver-example
spec:
  hostname: "sqlserver-instance.database.windows.net"
  port: 1433
  authentication:
    secretName: sqlserver-secret

---

apiVersion: sql-server.dotkube.io/v1alpha1
kind: Database
metadata:
  name: foo
  namespace: sqlserver-example
spec:
  instanceName: sqlserver-instance
  databaseName: Foo

---

apiVersion: sql-server.dotkube.io/v1alpha1
kind: DatabaseSchema
metadata:
  name: foo-schema
  namespace: sqlserver-example
spec:
  instanceName: sqlserver-instance
  databaseName: Foo

---

apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerLogin
metadata:
  name: app-login
  namespace: sqlserver-example
spec:
  sqlServerName: sqlserver-instance
  loginName: appuser
  authenticationType: SQL
  secretName: sqlserver-secret

---

apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerUser
metadata:
  name: app-user
  namespace: sqlserver-example
spec:
  sqlServerName: sqlserver-instance
  databaseName: Foo
  loginName: appuser
  roles:
    - db_owner



```

## Planned Features and Roadmap

Here are the planned features and milestones for KubeSQLServer Operator:

- Manage existing SQL Server instances
- CLI Tooling
- Helm Chart in a public repo
- Documentation Site
- Data API Integration
- Testing Strategies
- Pipeline Automation


## Development Workflow

This repository contains the following components:

1. **KubeOps-based .NET Operator**  
   - Located in `src/OperatorTemplate.Operator`.  
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