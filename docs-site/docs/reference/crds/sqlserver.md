---
sidebar_position: 2
---

# SQLServer

The `SQLServer` CRD manages SQL Server instances running as StatefulSets inside your Kubernetes cluster with persistent storage.

## Specification

### API Version

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServer
```

### Fields

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `image` | string | No | `mcr.microsoft.com/mssql/server:2022-latest` | Container image for SQL Server |
| `storageClass` | string | No | `standard` | Storage class for persistent volume |
| `storageSize` | string | No | `20Gi` | Size of the persistent storage volume |
| `secretName` | string | Yes | - | Name of the secret containing the SA password |
| `serviceType` | string | No | `None` | Service type: `None`, `ClusterIP`, `NodePort`, or `LoadBalancer` |

### Status

| Field | Type | Description |
|-------|------|-------------|
| `state` | string | Current state: `Pending`, `Ready`, or `Error` |
| `message` | string | Details about the current status |

## Example

### Basic Deployment

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: sqlserver-secret
  namespace: default
type: Opaque
stringData:
  password: "MySecurePassword123!"

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServer
metadata:
  name: production-sql
  namespace: default
spec:
  image: "mcr.microsoft.com/mssql/server:2022-latest"
  storageClass: "ssd"
  storageSize: "100Gi"
  secretName: "sqlserver-secret"
  serviceType: "LoadBalancer"
```

### Development Instance

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServer
metadata:
  name: dev-sql
  namespace: development
spec:
  image: "mcr.microsoft.com/mssql/server:2022-latest"
  storageClass: "standard"
  storageSize: "10Gi"
  secretName: "dev-sql-secret"
  serviceType: "NodePort"
```

## Behavior

### What It Creates

When you create a SQLServer resource, the operator automatically creates:

1. **StatefulSet** - Running SQL Server with 1 replica
2. **Headless Service** - For StatefulSet pod identity (always created)
3. **Service** - For external access (if `serviceType` is not `None`)
4. **ConfigMap** - SQL Server configuration
5. **PersistentVolumeClaim** - Managed by the StatefulSet

### Service Types

- **`None`** (default) - Only headless service for internal access
- **`ClusterIP`** - Internal cluster access
- **`NodePort`** - Access via node IP and port
- **`LoadBalancer`** - External load balancer (cloud provider)

### Naming Conventions

For a SQLServer named `my-sqlserver`:

- StatefulSet: `my-sqlserver-statefulset`
- Headless Service: `my-sqlserver-headless`
- Service (if enabled): `my-sqlserver-service`
- ConfigMap: `my-sqlserver-config`
- PVC: Managed automatically by StatefulSet

## Connection

### Internal (from within cluster)

```
Server: my-sqlserver-headless.namespace.svc.cluster.local,1433
```

### External (NodePort)

```bash
# Get the NodePort
kubectl get svc my-sqlserver-service

# Connect using
Server: <node-ip>,<node-port>
```

### External (LoadBalancer)

```bash
# Get the external IP
kubectl get svc my-sqlserver-service

# Connect using
Server: <external-ip>,1433
```

## Secret Format

The secret must contain a `password` field:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: sqlserver-secret
type: Opaque
stringData:
  password: "YourStrongPassword123!"
```

## Storage

SQL Server data is persisted in a PersistentVolumeClaim. The data survives pod restarts and rescheduling.

### Storage Classes

Choose appropriate storage class for your needs:

- **Development**: `standard` HDD
- **Production**: `ssd` or `premium-ssd` SSD
- **High Performance**: NVMe-based storage classes

## Security

### Pod Security Context

The operator configures:
- `fsGroup: 10001`
- `runAsUser: 10001`
- `runAsGroup: 0`

### Istio

Sidecar injection is disabled: `sidecar.istio.io/inject: "false"`

## Limitations

- Single replica only (no HA)
- SA password must be provided via secret
- Developer edition (can be changed via image)

## Troubleshooting

### Check Pod Status

```bash
kubectl get pods -l app=my-sqlserver
kubectl logs my-sqlserver-statefulset-0
```

### Check Events

```bash
kubectl describe sqlserver my-sqlserver
```

### Verify PVC

```bash
kubectl get pvc
```

## Next Steps

- [Create a Database](./database.md)
- [Create Logins](./sql-server-login.md)
- [External SQL Server](./external-sql-server.md)
