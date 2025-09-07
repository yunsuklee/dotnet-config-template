namespace ConfigTemplate.ProjectDiscovery.Models;

public class ProjectConfig
{
    public required string FilePath { get; set; }  // Full path to .csproj file
    public required string FileName { get; set; }  // Just the filename (e.g., "MyProject.csproj")
    public required string DirectoryPath { get; set; } // Directory path containing the .csproj
    public IReadOnlyList<string> ConfigFiles { get; set; } = [];
    public string? UserSecretsFile { get; set; }
}