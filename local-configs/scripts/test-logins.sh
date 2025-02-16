#!/bin/bash

# Get the SQL Server LoadBalancer IP dynamically
SQL_SERVER=$(kubectl get svc sqlserver-instance-service -n sqlserver-example -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
SQL_PORT="1433"

if [[ -z "$SQL_SERVER" ]]; then
    echo "❌ Could not retrieve SQL Server LoadBalancer IP."
    exit 1
fi

CREDENTIALS=(
    "sa SuperSecretPassword123! master"
    "adminuser SuperSecretPassword123! HelloWorld1"
    "adminuser SuperSecretPassword123! HelloWorld2"
    "datauser SuperSecretPassword123! HelloWorld1"
    "datauser SuperSecretPassword123! HelloWorld2"
)

echo "Testing SQL Server connections on $SQL_SERVER:$SQL_PORT..."

for entry in "${CREDENTIALS[@]}"; do
    read -r USERNAME PASSWORD DATABASE <<< "$entry"

    echo "🔐 Testing $DATABASE as $USERNAME..."
    echo "   Querying: SELECT 1;"

    OUTPUT=$(sqlcmd -S $SQL_SERVER -U "$USERNAME" -P "$PASSWORD" -d "$DATABASE" -Q "SELECT 1;" -C -N 2>&1)

    if [ $? -eq 0 ]; then
        echo "✅ Successfully connected to $DATABASE as $USERNAME"
    else
        echo "❌ Failed to connect to $DATABASE as $USERNAME"
        echo "   Error: $OUTPUT"
    fi
done
