# Setting up Microsoft Entra ID for Local Development

This guide provides instructions on how to configure your local development environment to work with SQL Server's Microsoft Entra ID authentication.

## Prerequisites

- An Azure account with permissions to create app registrations.
- [Azure CLI (az)](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) installed and logged in.
  ```bash
  az login
  ```

## Setup Instructions

1. **Run the Setup Script**
   Run the following task to create an app registration and a self-signed certificate:
   ```bash
   task dev:local-cluster:setup-entra-id
   ```
   This script will:
   - Generate a self-signed PFX certificate (required by SQL Server).
   - Create an app registration in your Azure tenant.
   - Upload the certificate to the app registration.
   - Assign the necessary Graph API permissions (`Directory.Read.All`).

2. **Update your .env file**
   The script will output several values (Application ID, Tenant ID, and Certificate Path). Update your `.env` file in the root of the repository with these values:
   ```dotenv
   MSSQL_AAD_CLIENT_ID=00000000-0000-0000-0000-000000000000
   MSSQL_AAD_PRIMARY_TENANT=00000000-0000-0000-0000-000000000000
   # The default path is usually /var/opt/mssql/mssql-entra-id.pfx inside the container
   MSSQL_AAD_CERTIFICATE_FILE_PATH=/var/opt/mssql/mssql-entra-id.pfx
   ```

3. **Enable Entra ID in the Taskfile**
   Open the root `Taskfile.yml` and change the value of `USE_ENTRA_ID_BASED_CONTAINER` to `true`:
   ```yaml
   vars:
     USE_ENTRA_ID_BASED_CONTAINER: true
   ```

4. **Launch the Environment**
   Run the quick development task:
   ```bash
   task quick-dev
   ```
   This will now launch a SQL Server container on port **1436** configured with your Entra ID app registration.
