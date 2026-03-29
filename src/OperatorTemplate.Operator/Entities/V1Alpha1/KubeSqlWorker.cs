using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace SqlServerOperator.Entities.V1Alpha1;

[KubernetesEntity(Group = "sql-server.dotkube.io", ApiVersion = "v1alpha1", Kind = "KubeSqlWorker")]
public class V1Alpha1KubeSqlWorker : CustomKubernetesEntity<V1Alpha1KubeSqlWorker.V1Alpha1KubeSqlWorkerSpec, V1Alpha1KubeSqlWorker.V1Alpha1KubeSqlWorkerStatus>
{
    [Description("Spec of the KubeSqlWorker configuration.")]
    public class V1Alpha1KubeSqlWorkerSpec
    {
    }

    [Description("Status of the KubeSqlWorker configuration.")]
    public class V1Alpha1KubeSqlWorkerStatus
    {
        [Description("The current state of the worker configuration.")]
        public string State { get; set; } = "Pending";
    }
}
