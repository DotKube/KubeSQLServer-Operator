---
sidebar_position: 2
---

# Quick Start

This guide will walk you through deploying your first SQL Server instance with the KubeSQLServer Operator.

## Create an In-Cluster SQL Server

Let's create a SQL Server instance running inside your Kubernetes cluster.

### Step 1: Create a Secret

First, create a secret to store the SA password:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: sqlserver-secret
  namespace: default
type: Opaque
stringData:
  password: "YourStrongPassword123!"
```

Apply it:

```bash
kubectl apply -f secret.yaml
```

### Step 2: Deploy SQL Server

Create a SQLServer resource:

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServer
metadata:
  name: my-sqlserver
  namespace: default
spec:
  image: "mcr.microsoft.com/mssql/server:2022-latest"
  storageClass: "standard"
  storageSize: "10Gi"
  secretName: "sqlserver-secret"
  serviceType: "NodePort"
```

Apply it:

```bash
kubectl apply -f sqlserver.yaml
```

### Step 3: Verify Deployment

Check the status:

```bash
kubectl get sqlservers
kubectl get pods
kubectl get svc
```

### Step 4: Create a Database

Now create a database:

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: Database
metadata:
  name: my-database
  namespace: default
spec:
  instanceName: my-sqlserver
  databaseName: ApplicationDB
```

Apply it:

```bash
kubectl apply -f database.yaml
```

### Step 5: Create a Login and User

Create a SQL Server login:

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerLogin
metadata:
  name: app-login
  namespace: default
spec:
  sqlServerName: my-sqlserver
  loginName: appuser
  authenticationType: SQL
  secretName: sqlserver-secret
```

Create a database user:

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerUser
metadata:
  name: app-user
  namespace: default
spec:
  sqlServerName: my-sqlserver
  databaseName: ApplicationDB
  loginName: appuser
  roles:
    - db_datareader
    - db_datawriter
```

Apply both:

```bash
kubectl apply -f login.yaml
kubectl apply -f user.yaml
```

## Connect to Your SQL Server

Get the NodePort:

```bash
kubectl get svc my-sqlserver-service
```

Connect using SQL Server Management Studio or Azure Data Studio:

- **Server**: `localhost,<NodePort>`
- **Authentication**: SQL Server Authentication
- **Login**: `appuser`
- **Password**: `YourStrongPassword123!`
- **Database**: `ApplicationDB`

## Complete Example

Save all resources in a single file `quickstart.yaml`:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: sqlserver-secret
  namespace: default
type: Opaque
stringData:
  password: "YourStrongPassword123!"

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServer
metadata:
  name: my-sqlserver
  namespace: default
spec:
  image: "mcr.microsoft.com/mssql/server:2022-latest"
  storageClass: "standard"
  storageSize: "10Gi"
  secretName: "sqlserver-secret"
  serviceType: "NodePort"

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: Database
metadata:
  name: my-database
  namespace: default
spec:
  instanceName: my-sqlserver
  databaseName: ApplicationDB

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerLogin
metadata:
  name: app-login
  namespace: default
spec:
  sqlServerName: my-sqlserver
  loginName: appuser
  authenticationType: SQL
  secretName: sqlserver-secret

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerUser
metadata:
  name: app-user
  namespace: default
spec:
  sqlServerName: my-sqlserver
  databaseName: ApplicationDB
  loginName: appuser
  roles:
    - db_datareader
    - db_datawriter
```

Deploy everything at once:

```bash
kubectl apply -f quickstart.yaml
```

## Cleanup

To remove all resources:

```bash
kubectl delete -f quickstart.yaml
```

## Next Steps

- [Managing External SQL Servers](../guides/external-sql-server.md)
- [Schema Management](../guides/schema-management.md)
- [CRD Reference](../reference/crds/overview.md)
