namespace LimboDancer.MCP.Storage;

public sealed class StorageOptions
{
    public string ConnectionString { get; set; } = "";
    public bool ApplyMigrationsAtStartup { get; set; } = false;
}