---
sidebar_position: 2
---

# Schema Management

Learn how to organize your database using schemas for better structure and security.

## What are Schemas?

Schemas are containers for database objects (tables, views, procedures) that provide:

- **Logical Organization**: Group related objects together
- **Security Boundaries**: Control access at the schema level
- **Namespace Management**: Avoid naming conflicts

## Basic Schema Creation

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerSchema
metadata:
  name: sales-schema
  namespace: default
spec:
  instanceName: my-sqlserver
  databaseName: ApplicationDB
  schemaName: Sales
  schemaOwner: dbo
```

## Multi-Schema Application

### Microservices Pattern

Separate schemas for each microservice:

```yaml
# Orders Service Schema
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerSchema
metadata:
  name: orders-schema
spec:
  instanceName: my-sqlserver
  databaseName: ApplicationDB
  schemaName: Orders
  schemaOwner: orders_user

---
# Inventory Service Schema
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerSchema
metadata:
  name: inventory-schema
spec:
  instanceName: my-sqlserver
  databaseName: ApplicationDB
  schemaName: Inventory
  schemaOwner: inventory_user

---
# Customer Service Schema
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerSchema
metadata:
  name: customer-schema
spec:
  instanceName: my-sqlserver
  databaseName: ApplicationDB
  schemaName: Customer
  schemaOwner: customer_user
```

## Security with Schemas

### Read-Only Reporting Schema

```yaml
# Create reporting user
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerLogin
metadata:
  name: reporting-login
spec:
  sqlServerName: my-sqlserver
  loginName: reporting_ro
  authenticationType: SQL
  secretName: reporting-secret

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerUser
metadata:
  name: reporting-user
spec:
  sqlServerName: my-sqlserver
  databaseName: ApplicationDB
  loginName: reporting_ro
  roles:
    - db_datareader  # Read-only

---
# Reporting schema owned by read-only user
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerSchema
metadata:
  name: reporting-schema
spec:
  instanceName: my-sqlserver
  databaseName: ApplicationDB
  schemaName: Reporting
  schemaOwner: reporting_ro
```

## Naming Conventions

### Best Practices

1. **Use PascalCase**: `Sales`, `Inventory`, `CustomerManagement`
2. **Be Descriptive**: Schema name should indicate its purpose
3. **Avoid Abbreviations**: `Reporting` not `Rpt`
4. **Group Related**: `Sales`, `SalesReporting`, `SalesArchive`

### Examples

```text
Good:
- Sales
- Inventory
- CustomerManagement
- Reporting
- Archive

Avoid:
- sch1
- data
- temp
- test
```

## Schema Usage in SQL

Once schemas are created, reference objects with:

```sql
-- Create table in schema
CREATE TABLE Sales.Orders (
    OrderID INT PRIMARY KEY,
    CustomerID INT
);

-- Query from schema
SELECT * FROM Sales.Orders;

-- Cross-schema query
SELECT 
    o.OrderID,
    c.CustomerName
FROM Sales.Orders o
JOIN Customer.Customers c ON o.CustomerID = c.CustomerID;
```

## Complete Example

```yaml
# Database setup
apiVersion: sql-server.dotkube.io/v1alpha1
kind: Database
metadata:
  name: ecommerce-db
spec:
  instanceName: my-sqlserver
  databaseName: ECommerce

---
# Application user
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerLogin
metadata:
  name: app-login
spec:
  sqlServerName: my-sqlserver
  loginName: ecommerce_app
  authenticationType: SQL
  secretName: app-secret

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerUser
metadata:
  name: app-user
spec:
  sqlServerName: my-sqlserver
  databaseName: ECommerce
  loginName: ecommerce_app
  roles:
    - db_datareader
    - db_datawriter
    - db_ddladmin

---
# Business domain schemas
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerSchema
metadata:
  name: products-schema
spec:
  instanceName: my-sqlserver
  databaseName: ECommerce
  schemaName: Products
  schemaOwner: ecommerce_app

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerSchema
metadata:
  name: orders-schema
spec:
  instanceName: my-sqlserver
  databaseName: ECommerce
  schemaName: Orders
  schemaOwner: ecommerce_app

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerSchema
metadata:
  name: customers-schema
spec:
  instanceName: my-sqlserver
  databaseName: ECommerce
  schemaName: Customers
  schemaOwner: ecommerce_app

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerSchema
metadata:
  name: payment-schema
spec:
  instanceName: my-sqlserver
  databaseName: ECommerce
  schemaName: Payment
  schemaOwner: ecommerce_app
```

## Schema Ownership

The schema owner has full control over the schema and its objects. Choose owners carefully:

- **Application Users**: For app-managed schemas
- **DBO**: For shared/system schemas
- **Service Accounts**: For specific service schemas

## Migration Strategy

When adding schemas to existing databases:

1. **Create schema** using CRD
2. **Migrate objects** to new schema (manual SQL)
3. **Update application** connection strings/queries
4. **Remove old objects** after validation

## Troubleshooting

### Schema Already Exists

The operator checks if schema exists before creating. Safe to apply multiple times.

### Ownership Issues

Ensure the owner user exists:

```bash
kubectl get sqlserverusers
```

### Permission Denied

The SA account (or connecting account) must have permissions to create schemas.

## Next Steps

- [Database Design Patterns](./patterns.md)
- [Security Best Practices](./security.md)
- [CRD Reference](../reference/crds/overview.md)
