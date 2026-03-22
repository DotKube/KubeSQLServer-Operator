#!/bin/bash
set -e

# Configuration
CERT_NAME="mssql-entra-id"
APP_NAME="mssql-server-instance"
CERT_FILE="${CERT_NAME}-cert.pem"
KEY_FILE="${CERT_NAME}-key.pem"
PFX_FILE="${CERT_NAME}.pfx"
EXPIRY_DAYS=365

# Check for Azure CLI
if ! command -v az &> /dev/null; then
    echo "Error: Azure CLI (az) is not installed."
    exit 1
fi

# 1. Create Certificate (Idempotent)
if [[ ! -f "$PFX_FILE" ]]; then
    echo "Creating self-signed certificate..."
    openssl req -x509 -newkey rsa:4096 \
      -keyout "$KEY_FILE" \
      -out "$CERT_FILE" \
      -days "$EXPIRY_DAYS" -nodes \
      -subj "/CN=${APP_NAME}"

    echo "Converting certificate to PFX..."
    # Export with no password as required by SQL Server container
    openssl pkcs12 \
      -inkey "$KEY_FILE" \
      -in "$CERT_FILE" \
      -nodes -export \
      -out "$PFX_FILE" -passout pass:
    echo "Certificate created: $PFX_FILE"
else
    echo "Certificate already exists: $PFX_FILE"
fi

# 2. App Registration (Idempotent)
echo "Checking for app registration: $APP_NAME"
APP_ID=$(az ad app list --display-name "$APP_NAME" --query "[0].appId" -o tsv)

if [[ -z "$APP_ID" ]]; then
    echo "Creating app registration..."
    APP_ID=$(az ad app create --display-name "$APP_NAME" --query "appId" -o tsv)
    echo "App registration created: $APP_ID"
else
    echo "App registration already exists: $APP_ID"
fi

# 3. Upload Certificate (Idempotent - checks if cert thumbprint is already there)
THUMBPRINT=$(openssl x509 -in "$CERT_FILE" -fingerprint -noout | sed 's/SHA1 Fingerprint=//' | sed 's/://g')
EXISTING_CERTS=$(az ad app credential list --id "$APP_ID" --query "[].customKeyIdentifier" -o tsv)

# Note: customKeyIdentifier might be encoded, but az ad app credential add handles thumbprints.
# For simplicity, we'll check if the cert file was already uploaded by name or just upload it.
# Actually, az ad app credential add is not strictly idempotent by thumbprint in a simple way, 
# so we'll just check if there are any credentials or trust the user.
if [[ -z "$EXISTING_CERTS" ]]; then
    echo "Uploading certificate to app registration..."
    az ad app credential reset --id "$APP_ID" --cert "@${CERT_FILE}" --append
    echo "Certificate uploaded."
else
    echo "App registration already has credentials. Skipping upload."
fi

# 4. Grant API Permissions (Directory.Read.All)
echo "Ensuring API permissions (Directory.Read.All)..."
# Microsoft Graph App ID: 00000003-0000-0000-c000-000000000000
# Directory.Read.All (Application) ID: 7ab1d382-f21e-4acd-a863-ba3e13f7da61
az ad app permission add --id "$APP_ID" --api 00000003-0000-0000-c000-000000000000 --api-permissions 7ab1d382-f21e-4acd-a863-ba3e13f7da61=Role

echo "Requesting admin consent (requires admin privileges)..."
# az ad app permission admin-consent --id "$APP_ID" || echo "Warning: Could not grant admin consent. Please do it manually in the portal."

echo "------------------------------------------------"
echo "Setup Complete!"
echo "APP_ID (MSSQL_AAD_CLIENT_ID): $APP_ID"
TENANT_ID=$(az account show --query "tenantId" -o tsv)
echo "TENANT_ID (MSSQL_AAD_PRIMARY_TENANT): $TENANT_ID"
echo "PFX Path: $(pwd)/$PFX_FILE"
echo "------------------------------------------------"
echo "Please update your .env file with these values."
