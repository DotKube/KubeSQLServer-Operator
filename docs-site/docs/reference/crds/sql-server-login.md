---
sidebar_position: 5
---

# SQLServerLogin

The `SQLServerLogin` CRD creates and manages server-level logins with SQL Server authentication.

## Specification

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerLogin
```

### Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sqlServerName` | string | Yes | Name of SQLServer or ExternalSQLServer |
| `loginName` | string | Yes | Name of the login to create |
| `authenticationType` | string | Yes | Authentication type (currently only `SQL` supported) |
| `secretName` | string | Yes | Secret containing the password |

### Status

| Field | Type | Description |
|-------|------|----------|---|
| `state` | string | `Ready`, `Error`, or `Pending` |
| `message` | string | Status details |
| `lastChecked` | DateTime | Last reconciliation time |

## Example

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: login-secret
type: Opaque
stringData:
  password: "LoginPassword123!"

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
  secretName: login-secret
```

## Behavior

Creates a SQL Server login using:

```sql
IF NOT EXISTS (SELECT name FROM sys.sql_logins WHERE name = 'LoginName')
BEGIN
    CREATE LOGIN [LoginName] WITH PASSWORD = 'Password'
END
```

## Deletion

When deleted, the operator drops the login from SQL Server.

## Next Steps

- [Create Database Users](./sql-server-user.md) mapped to this login
