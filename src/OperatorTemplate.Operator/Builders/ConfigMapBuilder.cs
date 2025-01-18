
using k8s.Models;


namespace SqlServerOperator.Builders;


public class ConfigMapBuilder
{
    private readonly V1ConfigMap _configMap = new();

    public ConfigMapBuilder WithMetadata(string name, string namespaceName, IDictionary<string, string> labels)
    {
        _configMap.Metadata = new V1ObjectMeta
        {
            Name = name,
            NamespaceProperty = namespaceName,
            Labels = labels
        };
        return this;
    }

    public ConfigMapBuilder WithData(string key, string data)
    {
        _configMap.Data ??= new Dictionary<string, string>();
        _configMap.Data[key] = data;
        return this;
    }

    public V1ConfigMap Build() => _configMap;
}