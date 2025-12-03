---
sidebar_position: 6
---

# SQLServerUser

The `SQLServerUser` CRD creates database users mapped to logins and assigns database roles.

## Specification

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerUser
```

### Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sqlServerName` | string | Yes | Name of SQLServer or ExternalSQLServer |
| `databaseName` | string | Yes | Database where user should be created |
| `loginName` | string | Yes | Name of the login to map |
| `roles` | []string | Yes | Database roles to assign |

### Status

| Field | Type | Description |
|-------|------|----------|
| `state` | string | `Ready`, `Error`, or `Pending` |
| `message` | string | Status details |
| `lastChecked` | DateTime | Last reconciliation time |

## Example

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

## Available Database Roles

Common built-in database roles:

- `db_owner` - Full control over database
- `db_datareader` - Read all data
- `db_datawriter` - Insert, update, delete data
- `db_ddladmin` - Create, alter, drop objects
- `db_securityadmin` - Manage permissions
- `db_backupoperator` - Backup database
- `db_denydatareader` - Deny read access
- `db_denydatawriter` - Deny write access

## Behavior

Creates user and assigns roles:

```sql
IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'LoginName')
BEGIN
    CREATE USER [LoginName] FOR LOGIN [LoginName]
END

EXEC sp_addrolemember 'db_datareader', 'LoginName'
EXEC sp_addrolemember 'db_datawriter', 'LoginName'
```

## Multiple Roles

Assign multiple roles for granular permissions:

```yaml
spec:
  loginName: poweruser
  roles:
    - db_datareader
    - db_datawriter
    - db_ddladmin
```

## Deletion

When deleted, the operator drops the user from the database.

## Next Steps

- [Manage Schemas](./sql-server-schema.md)
- [Role-Based Access Control Guide](../../guides/rbac.md)
