

using k8s.Models;

namespace SqlServerOperator.Builders;

public class StatefulSetBuilder
{
    private readonly V1StatefulSet _statefulSet = new();

    public StatefulSetBuilder WithMetadata(string name, string namespaceName, IDictionary<string, string> labels)
    {
        _statefulSet.Metadata = new V1ObjectMeta
        {
            Name = name,
            NamespaceProperty = namespaceName,
            Labels = labels
        };
        return this;
    }

    public StatefulSetBuilder WithSpec(V1StatefulSetSpec spec)
    {
        _statefulSet.Spec = spec;
        return this;
    }

    public V1StatefulSet Build() => _statefulSet;
}