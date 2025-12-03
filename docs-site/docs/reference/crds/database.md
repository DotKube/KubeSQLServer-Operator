---
sidebar_position: 4
---

# Database

The `Database` CRD creates and manages databases on SQLServer or ExternalSQLServer instances.

## Specification

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: Database
```

### Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `instanceName` | string | Yes | Name of SQLServer or ExternalSQLServer |
| `databaseName` | string | Yes | Name of the database to create |

### Status

| Field | Type | Description |
|-------|------|-------------|
| `state` | string | `Ready`, `Error`, or `Pending` |
| `message` | string | Status details |
| `lastChecked` | DateTime | Last reconciliation time |

## Examples

### On In-Cluster SQL Server

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: Database
metadata:
  name: application-db
  namespace: default
spec:
  instanceName: my-sqlserver  # References SQLServer CRD
  databaseName: ApplicationDatabase
```

### On External SQL Server

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: Database
metadata:
  name: external-db
  namespace: default
spec:
  instanceName: azure-sql  # References ExternalSQLServer CRD
  databaseName: ProductionDB
```

## Behavior

The operator executes:

```sql
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'DatabaseName')
BEGIN
    CREATE DATABASE [DatabaseName]
END
```

## Deletion

Deleting the Database CRD does **not** drop the database. This prevents accidental data loss.

## Next Steps

- [Create Users](./sql-server-user.md)
- [Create Schemas](./sql-server-schema.md)
