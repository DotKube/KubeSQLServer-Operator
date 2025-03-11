#! /bin/bash

NAMESPACE="kubesql-operator"
RAW_URL="https://raw.githubusercontent.com/DotKube/KubeSQLServer-Operator/main/deploy/yaml/deploy.yaml"

# Create namespace
echo "Creating namespace '$NAMESPACE'..."
kubectl create namespace "$NAMESPACE"

# Apply the YAML file
echo "Applying deploy.yaml from $RAW_URL..."
kubectl apply -f "$RAW_URL"
