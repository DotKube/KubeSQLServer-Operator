#! /bin/bash

NAMESPACE="kubesql-operator"
RAW_URL="https://raw.githubusercontent.com/DotKube/KubeSQLServer-Operator/main/deploy/yaml/deploy.yaml"

# Create namespace
echo "Creating namespace '$NAMESPACE'..."
kubectl create namespace "$NAMESPACE"

# Switch to the namespace
echo "Switching to namespace '$NAMESPACE'..."
kubectl config set-context --current --namespace="$NAMESPACE"

# Apply the YAML file
echo "Applying deploy.yaml from $RAW_URL..."
kubectl apply -f "$RAW_URL"
