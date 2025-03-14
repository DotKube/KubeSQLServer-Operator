# KubeSQLServer Operator

KubeSQLServer Operator is a completely free and open-source (MIT licensed) Kubernetes operator designed to help you run and manage Microsoft SQL Server seamlessly.

This project is intended to be an open-source alternative to D2HI's Dx Operator, which requires a license [D2HI link](https://support.dh2i.com/dxoperator/guides/dxoperator-qsg/).

## 😎 GitOpsify New or Existing SQL Server instances

Simply deploy into your Kubernetes cluster like so

```bash
kubectl create namespace sql-server
kubectl config set-context --current --namespace=sql-server
kubectl apply -f https://raw.githubusercontent.com/DotKube/KubeSQLServer-Operator/main/deploy/yaml/deploy.yaml
kubectl get all
```

For those on windows, you can run this to quickly install kubesql operator

```powershell
docker run --rm -it `
  --network host `
  -v $env:USERPROFILE\.kube:/root/.kube `
  fedora:41 bash -c "
    dnf install -y curl sudo && \
    curl -LO https://dl.k8s.io/release/v1.22.2/bin/linux/amd64/kubectl && \
    sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl && \
    export KUBECONFIG=/root/.kube/config && \
    kubectl create namespace sql-server && \
    kubectl config set-context --current --namespace=sql-server && \
    kubectl apply -f https://raw.githubusercontent.com/DotKube/KubeSQLServer-Operator/main/deploy/yaml/deploy.yaml && \
    kubectl get all
  "

```

and then start creating SQL Server instances using the CRDs provided by the operator.

```yaml
# ... yaml omitted for brevity

apiVersion: sql-server.dotkube.io/v1alpha1
kind: SQLServer
metadata:
  name: sqlserver-instance
  namespace: sqlserver-example
spec:
  version: "2022"
  storageClass: "longhorn"
  storageSize: "6Gi"
  secretName: sqlserver-secret
  enableHighAvailibility: true
  enableFullTextSearch: true
  serviceType: LoadBalancer

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: Database
metadata:
  name: foo
  namespace: sqlserver-example
spec:
  instanceName: sqlserver-instance
  databaseName: Foo

---
apiVersion: sql-server.dotkube.io/v1alpha1
kind: Database
metadata:
  name: bar
  namespace: sqlserver-example
spec:
  instanceName: sqlserver-instance
  databaseName: Bar

```

and you're good to go! You should be able to see the effect of the CRDs in your SQL Server instance.

![Azure Data Studio](assets/ads-screenshot.png)

## Planned Features and Roadmap

Here are the planned features and milestones for KubeSQLServer Operator:

- Manage existing SQL Server instances
- CLI Tooling
- Helm Chart in a public repo
- Documentation Site
- Data API Integration
- Testing Strategies

---

## Local Development - Key Commands

### Local Development (`task quick-dev`)

Run the operator **locally** on your laptop while applying necessary CRDs and creating an instance in a Kind cluster.  
This is ideal for debugging and real-time development.

```bash
task quick-dev
```

### In-Cluster Deployment (`task quick-deploy`)

Build and deploy the operator **to a Kind cluster**, replicating a production-like environment for testing.

```bash
task quick-deploy
```

---

## Support

If you have any questions or need help, please feel free to reach out to us on our [Slack Channel](https://join.slack.com/t/dotkube/shared_invite/zt-31u3vjhnn-5Wna5GDTW6tJTBzSf1PhyA)
