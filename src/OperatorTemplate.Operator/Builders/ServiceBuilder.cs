
using k8s.Models;

public class ServiceBuilder
{
    private readonly V1Service _service = new();

    public ServiceBuilder WithMetadata(string name, string namespaceName, IDictionary<string, string> labels)
    {
        _service.Metadata = new V1ObjectMeta
        {
            Name = name,
            NamespaceProperty = namespaceName,
            Labels = labels
        };
        return this;
    }

    public ServiceBuilder WithSpec(V1ServiceSpec spec)
    {
        _service.Spec = spec;
        return this;
    }

    public V1Service Build() => _service;
}