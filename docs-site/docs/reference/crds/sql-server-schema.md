---
sidebar_position: 7
---

# SQLServerSchema

The `SQLServerSchema` CRD creates and manages database schemas for organizing database objects.

## Specification

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerSchema
```

### Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `instanceName` | string | Yes | Name of SQLServer or ExternalSQLServer |
| `databaseName` | string | Yes | Database where schema should be created |
| `schemaName` | string | Yes | Name of the schema |
| `schemaOwner` | string | Yes | Database user who owns the schema |

### Status

| Field | Type | Description |
|-------|------|----------|
| `state` | string | `Ready`, `Error`, or `Pending` |
| `message` | string | Status details |
| `lastChecked` | DateTime | Last reconciliation time |

## Example

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerSchema
metadata:
  name: reporting-schema
  namespace: default
spec:
  instanceName: my-sqlserver
  databaseName: ApplicationDB
  schemaName: Reporting
  schemaOwner: appuser
```

## Behavior

Creates schema with specified owner:

```sql
IF NOT EXISTS (
    SELECT schema_name 
    FROM information_schema.schemata 
    WHERE schema_name = 'SchemaName'
)
BEGIN
    CREATE SCHEMA [SchemaName] AUTHORIZATION [SchemaOwner]
END
```

## Use Cases

### Logical Organization

```yaml
# Separate schemas for different application modules
---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerSchema
metadata:
  name: orders-schema
spec:
  instanceName: my-sqlserver
  databaseName: ApplicationDB
  schemaName: Orders
  schemaOwner: appuser

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerSchema
metadata:
  name: inventory-schema
spec:
  instanceName: my-sqlserver
  databaseName: ApplicationDB
  schemaName: Inventory
  schemaOwner: appuser
```

### Security Isolation

Different schemas with different owners for access control:

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerSchema
metadata:
  name: sensitive-data-schema
spec:
  instanceName: my-sqlserver
  databaseName: ApplicationDB
  schemaName: Sensitive
  schemaOwner: admin_user  # Only admin has access
```

## Schema Ownership

The `schemaOwner` must be:
- An existing database user
- Created before the schema (use SQLServerUser CRD)

## Object References

Once created, reference tables with:

```sql
SELECT * FROM Reporting.SalesData
SELECT * FROM Orders.OrderHeader
```

## Deletion

When deleted, the operator attempts to drop the schema. This fails if objects exist in the schema.

## Best Practices

1. **Plan schema structure** before creating tables
2. **Use meaningful names** for schemas
3. **Assign appropriate owners** for security
4. **Document schema purpose** in metadata

## Next Steps

- [Complete Example](../../guides/examples.md)
- [Database Design Patterns](../../guides/patterns.md)
