---
sidebar_position: 1
---

# Managing External SQL Servers

This guide shows you how to connect to and manage SQL Server instances running outside your Kubernetes cluster.

## Supported External SQL Servers

- Azure SQL Database
- AWS RDS SQL Server
- Google Cloud SQL for SQL Server
- On-premises SQL Server
- SQL Server in Docker containers
- SQL Server on VMs

## Prerequisites

- External SQL Server must be accessible from your Kubernetes cluster
- Network connectivity (firewalls, security groups configured)
- Valid credentials (username and password)

## Step 1: Create Credentials Secret

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: azure-sql-server-secret
  namespace: production
type: Opaque
stringData:
  username: "sqladmin"
  password: "SecurePassword123!"
```

## Step 2: Create ExternalSQLServer Resource

### Azure SQL Database Example

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: ExternalSQLServer
metadata:
  name: azure-production
  namespace: production
spec:
  host: "myserver.database.windows.net"
  port: 1433
  secretName: "azure-sql-server-secret"
  useEncryption: true
  trustServerCertificate: false  # Validate Azure cert
```

### AWS RDS Example

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: ExternalSQLServer
metadata:
  name: aws-production
  namespace: production
spec:
  host: "mydb.abc123.us-east-1.rds.amazonaws.com"
  port: 1433
  secretName: "azure-sql-server-secret"
  useEncryption: true
  trustServerCertificate: true
```

## Step 3: Verify Connection

```bash
kubectl get externalsqlservers -n production
kubectl describe externalsqlserver azure-production -n production
```

Look for `IsConnected: true` in the status.

## Step 4: Create Databases

```yaml
apiVersion: sql-server.dotkube.io/v1alpha1
kind: Database
metadata:
  name: production-db
  namespace: production
spec:
  instanceName: azure-production
  databaseName: ProductionDatabase
```

## Network Configuration

### Azure SQL Database

1. **Firewall Rules**: Add Kubernetes node IPs to Azure SQL firewall
2. **Service Endpoints**: Use Azure service endpoints for private connectivity
3. **Private Link**: For fully private connectivity

### AWS RDS

1. **Security Groups**: Allow inbound from Kubernetes cluster
2. **VPC Peering**: Connect Kubernetes VPC with RDS VPC
3. **Public Access**: Enable if cluster is outside VPC

### On-Premises

1. **VPN/Direct Connect**: Establish connectivity to on-prem network
2. **DNS Resolution**: Ensure Kubernetes can resolve internal hostnames
3. **Firewall**: Open SQL Server port (default 1433)

## Troubleshooting

### Connection Timeouts

```bash
# Test connectivity from a pod
kubectl run -it --rm debug \
  --image=mcr.microsoft.com/mssql-tools \
  --restart=Never -- /bin/bash

# Inside the pod
sqlcmd -S myserver.database.windows.net -U sqladmin -P 'password'
```

### Certificate Errors

If you see SSL/TLS errors:

```yaml
spec:
  useEncryption: true
  trustServerCertificate: true  # Bypass cert validation (dev only)
```

### DNS Resolution

```bash
# Test DNS resolution
kubectl run -it --rm debug \
  --image=busybox \
  --restart=Never -- nslookup myserver.database.windows.net
```

## Security Best Practices

1. **Use Encryption**: Set `useEncryption: true` for production
2. **Validate Certificates**: Set `trustServerCertificate: false` when possible
3. **Rotate Credentials**: Regularly update secrets
4. **Least Privilege**: Use dedicated service accounts
5. **Network Isolation**: Use private connectivity options

## Complete Example

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: production

---
apiVersion: v1
kind: Secret
metadata:
  name: azure-sql-secret
  namespace: production
type: Opaque
stringData:
  username: "app_service_account"
  password: "ComplexPassword123!@#"

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: ExternalSQLServer
metadata:
  name: azure-prod-sql
  namespace: production
spec:
  host: "prod-sql.database.windows.net"
  port: 1433
  secretName: "azure-sql-secret"
  useEncryption: true
  trustServerCertificate: false

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: Database
metadata:
  name: orders-db
  namespace: production
spec:
  instanceName: azure-prod-sql
  databaseName: OrdersDatabase

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerLogin
metadata:
  name: app-login
  namespace: production
spec:
  sqlServerName: azure-prod-sql
  loginName: orders_app
  authenticationType: SQL
  secretName: azure-sql-secret

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServerUser
metadata:
  name: app-user
  namespace: production
spec:
  sqlServerName: azure-prod-sql
  databaseName: OrdersDatabase
  loginName: orders_app
  roles:
    - db_datareader
    - db_datawriter
```

## Next Steps

- [Schema Management](./schema-management.md)
- [High Availability Setup](./ha-setup.md)
- [Monitoring](./monitoring.md)
