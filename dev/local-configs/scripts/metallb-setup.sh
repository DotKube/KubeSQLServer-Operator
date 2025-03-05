#!/bin/sh


# Create Kind network if needed
docker network create kind || true


# Calculate MetalLB IP range
KIND_NET_CIDR=$(docker network inspect kind -f '{{(index .IPAM.Config 0).Subnet}}')
KIND_NET_BASE=$(echo "${KIND_NET_CIDR}" | awk -F'.' '{print $1"."$2"."$3}')
METALLB_IP_START="${KIND_NET_BASE}.200"
METALLB_IP_END="${KIND_NET_BASE}.254"
METALLB_IP_RANGE="${METALLB_IP_START}-${METALLB_IP_END}"


echo "KIND_NET_CIDR: ${KIND_NET_CIDR}"
echo "KIND_NET_BASE: ${KIND_NET_BASE}"
echo "METALLB_IP_RANGE: ${METALLB_IP_RANGE}"



# Apply MetalLB configuration
kubectl apply -f - <<EOF
apiVersion: metallb.io/v1beta1
kind: IPAddressPool
metadata:
    namespace: metallb-system
    name: default-address-pool
spec:
    addresses:
    - ${METALLB_IP_RANGE}
---
apiVersion: metallb.io/v1beta1
kind: L2Advertisement
metadata:
    namespace: metallb-system
    name: default
EOF
