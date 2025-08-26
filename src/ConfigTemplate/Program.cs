using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger<Program>();

if (args.Length == 0)
{
    logger.LogInformation("Usage: config-template <directory>");
    return;
}

string targetDirectory = args[0];

if (!Directory.Exists(targetDirectory))
{
    logger.LogError("Directory '{Directory}' does not exist", targetDirectory);
    return;
}

logger.LogInformation("Scanning directory: {Directory}", targetDirectory);

var configFiles = FindConfigurationFiles(targetDirectory);

logger.LogInformation("Found {Count} configuration files", configFiles.Count);

if (configFiles.Count == 0) return;

foreach (var configFile in configFiles)
{
    try
    {
        var relativePath = Path.GetRelativePath(targetDirectory, configFile);
        logger.LogInformation("Processing: {RelativePath}", relativePath);
        // TODO(sergio): generate template file
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing {ConfigFile}", configFile);
    }
}

logger.LogInformation("Template generation complete");

// Replace these lines:
// static readonly string[] ConfigFilePatterns =
// [
//     "appsettings*.json",
//     "local.settings.json"
// ];

// static readonly string[] ExcludedDirectories =
// [
//     "bin",
//     "obj",
//     ".git",
//     "node_modules",
//     ".vs",
//     ".vscode"
// ];

// With the following (remove 'static' and 'readonly', use 'string[]' for local variables):

List<string> FindConfigurationFiles(string directory)
{
    var files = new List<string>();

    GatherRegularConfigFiles(directory, files);
    GatherUserSecretsFiles(directory, files);

    // Exclude template files we might have already generated
    files = [.. files.Where(f => !f.EndsWith(".template.json"))];

    return files;
}

void GatherRegularConfigFiles(string directory, List<string> files)
{
    string[] ConfigFilePatterns =
    [
        "appsettings*.json",
        "local.settings.json"
    ];

    string[] ExcludedDirectories =
    [
        "bin",
        "obj",
        ".git",
        "node_modules",
        ".vs",
        ".vscode"
    ];

    foreach (var pattern in ConfigFilePatterns)
    {
        var matchingFiles = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories)
            .Where(file => !IsInExcludedDirectory(file, ExcludedDirectories))
            .ToArray();

        logger.LogDebug("Pattern '{Pattern}' found {Count} files (after exclusions)",
            pattern, matchingFiles.Length);

        files.AddRange(matchingFiles);
    }
}

void GatherUserSecretsFiles(string directory, List<string> files)
{
    // Find all .csproj files
    var projectFiles = Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories)
        .Where(file => !IsInExcludedDirectory(file, ["bin", "obj"]));
    
    foreach (var projectFile in projectFiles)
    {
        try
        {
            var userSecretsId = GetUserSecretsId(projectFile);
            if (!string.IsNullOrEmpty(userSecretsId))
            {
                var secretsPath = GetUserSecretsPath(userSecretsId);
                if (File.Exists(secretsPath))
                {
                    files.Add(secretsPath);
                    logger.LogDebug("Found user secrets for project {Project}: {SecretsPath}", 
                        Path.GetFileName(projectFile), secretsPath);
                }
                else
                {
                    logger.LogDebug("User secrets configured for {Project} but file doesn't exist: {SecretsPath}", 
                        Path.GetFileName(projectFile), secretsPath);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to check user secrets for project {ProjectFile}", projectFile);
        }
    }
}

static string? GetUserSecretsId(string projectFilePath)
{
    try
    {
        var doc = XDocument.Load(projectFilePath);
        return doc.Descendants("UserSecretsId").FirstOrDefault()?.Value;
    }
    catch
    {
        return null;
    }
}

static string GetUserSecretsPath(string userSecretsId)
{
    var userSecretsRoot = Environment.OSVersion.Platform == PlatformID.Win32NT
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".microsoft", "usersecrets");
    
    return Path.Combine(userSecretsRoot, userSecretsId, "secrets.json");
}

static bool IsInExcludedDirectory(string filePath, string[] excludedDirectories)
{
    var pathParts = filePath.Split(
        [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
        StringSplitOptions.RemoveEmptyEntries);

    return excludedDirectories.Any(excluded =>
        pathParts.Contains(excluded, StringComparer.OrdinalIgnoreCase));
}
