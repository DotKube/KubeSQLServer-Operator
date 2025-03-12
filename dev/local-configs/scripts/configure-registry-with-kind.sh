#!/bin/sh
set -o errexit

# Define variables
reg_name="kind-registry"
reg_port="5000"
REGISTRY_DIR="/etc/containerd/certs.d/localhost:$reg_port"

# 1. Add the registry config to the nodes
for node in $(kind get nodes); do
  echo "Configuring registry directory for node: $node"
  docker exec "${node}" mkdir -p "${REGISTRY_DIR}"
  cat <<EOF | docker exec -i "${node}" cp /dev/stdin "${REGISTRY_DIR}/hosts.toml"
[host."http://${reg_name}:5000"]
EOF
done

# 2. Connect the registry to the cluster network if not already connected
if [ "$(docker inspect -f='{{json .NetworkSettings.Networks.kind}}' "${reg_name}")" = 'null' ]; then
  echo "Connecting registry to the Kind network"
  docker network connect "kind" "${reg_name}"
else
  echo "Registry already connected to the Kind network"
fi

# 3. Document the local registry
cat <<EOF | kubectl apply -f -
apiVersion: v1
kind: ConfigMap
metadata:
  name: local-registry-hosting
  namespace: kube-public
data:
  localRegistryHosting.v1: |
    host: "localhost:${reg_port}"
    help: "https://kind.sigs.k8s.io/docs/user/local-registry/"
EOF
