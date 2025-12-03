---
sidebar_position: 1
---

# CRD Overview

The KubeSQLServer Operator provides six Custom Resource Definitions (CRDs) for managing SQL Server resources declaratively in Kubernetes.

## Available CRDs

### SQLServer

Manages SQL Server instances running as StatefulSets inside your Kubernetes cluster.

**Use Case**: Deploy SQL Server with persistent storage for development, testing, or production workloads.

[View Full Reference →](./sqlserver.md)

### ExternalSQLServer

Manages connections to external SQL Server instances running outside your cluster.

**Use Case**: Connect to Azure SQL Database, AWS RDS SQL Server, on-premises SQL Server, or Docker containers.

[View Full Reference →](./external-sql-server.md)

### Database

Creates and manages databases on SQLServer or ExternalSQLServer instances.

**Use Case**: Provision databases declaratively for your applications.

[View Full Reference →](./database.md)

### SQLServerLogin

Creates and manages server-level logins.

**Use Case**: Create SQL Server authentication logins that can be used across multiple databases.

[View Full Reference →](./sql-server-login.md)

### SQLServerUser

Creates database users and assigns roles.

**Use Case**: Create database-specific users mapped to logins with specific permissions.

[View Full Reference →](./sql-server-user.md)

### SQLServerSchema

Creates and manages database schemas.

**Use Case**: Organize database objects into logical groups with specific ownership.

[View Full Reference →](./sql-server-schema.md)

## Resource Relationships

```
SQLServer (or ExternalSQLServer)
    ├── Database
    │   ├── SQLServerUser
    │   └── SQLServerSchema
    └── SQLServerLogin
```

## Status Fields

All CRDs include a `status` field with:

- **state**: Current state (`Ready`, `Error`, `Pending`)
- **message**: Descriptive message about the current state
- **lastChecked**: Timestamp of the last reconciliation

## Common Patterns

### Namespace Isolation

All resources should be created in the same namespace:

```yaml
metadata:
  name: my-resource
  namespace: my-namespace
```

### Referencing Resources

Resources reference each other by name within the same namespace:

```yaml
spec:
  instanceName: my-sqlserver  # References SQLServer or ExternalSQLServer
  databaseName: my-database   # References Database
```

### Secret Management

Passwords are stored in Kubernetes Secrets:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: sql-secret
type: Opaque
stringData:
  password: "YourPassword"
```

## API Version

All CRDs use the API version: `sql-server.dotkube.io/v1alpha1`

## Next Steps

- Explore individual CRD references
- See [Examples](../../guides/examples.md) for complete scenarios
