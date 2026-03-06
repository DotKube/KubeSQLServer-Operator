---
sidebar_position: 3
---

# ExternalSQLServer

The `ExternalSQLServer` CRD manages connections to SQL Server instances running outside your Kubernetes cluster.

## Specification

### API Version

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: ExternalSQLServer
```

### Fields

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `host` | string | Yes | - | Hostname or IP address of the SQL Server |
| `port` | int | No | `1433` | Port number for SQL Server |
| `secretName` | string | Yes | - | Name of secret containing credentials |
| `useEncryption` | bool | No | `false` | Enable TLS encryption |
| `trustServerCertificate` | bool | No | `true` | Trust server certificate (disable cert validation) |
| `additionalConnectionProperties` | map[string]string | No | - | Additional SQL connection string properties |

### Status

| Field | Type | Description |
|-------|------|-------------|
| `state` | string | Current state: `Ready` or `Error` |
| `message` | string | Details about connection status |
| `isConnected` | bool | Whether connection verification succeeded |
| `lastChecked` | DateTime | Timestamp of last connection check |

## Examples

### Azure SQL Database

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: azure-sql-secret
type: Opaque
stringData:
  username: "sqladmin"
  password: "YourPassword123!"

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: ExternalSQLServer
metadata:
  name: azure-sql
  namespace: default
spec:
  host: "myserver.database.windows.net"
  port: 1433
  secretName: "azure-sql-secret"
  useEncryption: true
  trustServerCertificate: false
```

### AWS RDS SQL Server

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: rds-sql-secret
type: Opaque
stringData:
  username: "admin"
  password: "YourPassword123!"

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: ExternalSQLServer
metadata:
  name: rds-sql
  namespace: default
spec:
  host: "mydb.abc123.us-east-1.rds.amazonaws.com"
  port: 1433
  secretName: "rds-sql-secret"
  useEncryption: true
  trustServerCertificate: true
```

### On-Premises SQL Server

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: onprem-sql-secret
type: Opaque
stringData:
  username: "sa"
  password: "YourPassword123!"

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: ExternalSQLServer
metadata:
  name: onprem-sql
  namespace: default
spec:
  host: "sqlserver.company.local"
  port: 1433
  secretName: "onprem-sql-secret"
  useEncryption: false
  trustServerCertificate: true
```

### Docker Container on Host

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: docker-sql-secret
type: Opaque
stringData:
  username: "sa"
  password: "YourPassword123!"

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: ExternalSQLServer
metadata:
  name: docker-sql
  namespace: default
spec:
  host: "host.docker.internal"  # or "localhost" with Kind
  port: 1433
  secretName: "docker-sql-secret"
  useEncryption: false
  trustServerCertificate: true
```

## Behavior

### Connection Verification

The operator periodically verifies the connection by executing:

```sql
SELECT @@VERSION
```

The status is updated based on the result.

### No Resource Creation

Unlike `SQLServer`, this CRD doesn't create any infrastructure. It only manages the connection metadata.

## Secret Format

The secret must contain `username` and `password`:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: external-sql-secret
type: Opaque
stringData:
  username: "sa"
  password: "YourPassword123!"
```

Note: If `username` is not provided, it defaults to `sa`.

## Additional Connection Properties

You can specify additional connection string properties:

```yaml
spec:
  host: "myserver.database.windows.net"
  port: 1433
  secretName: "sql-secret"
  additionalConnectionProperties:
    ApplicationName: "MyKubernetesApp"
    ConnectTimeout: "30"
    MultipleActiveResultSets: "true"
```

## Use Cases

- **Azure SQL Database** - Managed SQL in Azure
- **AWS RDS** - Managed SQL in AWS
- **On-Premises** - Corporate SQL Server instances
- **Docker Containers** - SQL Server running on Docker host
- **VM-hosted** - SQL Server on virtual machines

## Security Considerations

### Encryption

For production use with cloud providers:

```yaml
spec:
  useEncryption: true
  trustServerCertificate: false  # Validates certificate
```

For development/testing:

```yaml
spec:
  useEncryption: false
  trustServerCertificate: true  # Skips cert validation
```

### Network Access

Ensure your Kubernetes cluster can reach the external SQL Server:

- Network policies allow egress traffic
- Firewall rules permit connections from cluster
- DNS resolution works

## Troubleshooting

### Check Connection Status

```bash
kubectl get externalsqlservers
kubectl describe externalsqlserver my-external-sql
```

### Test Connection

```bash
# From a pod in the cluster
kubectl run -it --rm debug --image=mcr.microsoft.com/mssql-tools --restart=Never -- /bin/bash
sqlcmd -S myserver.database.windows.net -U sa -P 'password'
```

### Common Issues

1. **Connection Timeout** - Check network policies and firewall rules
2. **Certificate Error** - Set `trustServerCertificate: true` or install proper cert
3. **Authentication Failed** - Verify credentials in secret

## Next Steps

- [Create Databases](./database.md) on external servers
- [Manage Logins](./sql-server-login.md)
- [Create Users](./sql-server-user.md)
