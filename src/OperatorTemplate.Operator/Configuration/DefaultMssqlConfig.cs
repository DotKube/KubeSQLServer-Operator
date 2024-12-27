namespace SqlServerOperator.Configuration;


// DefaultMssqlConfig Singleton
public class DefaultMssqlConfig
{
    public string DefaultConfigMapData = @"
    [EULA]
    accepteula = Y
    accepteulaml = Y

    [coredump]
    captureminiandfull = true
    coredumptype = full

    [hadr]
    hadrenabled = 1

    [language]
    lcid = 1033
";

    public int DefaultRequeueTimeMinutes = 5;
}