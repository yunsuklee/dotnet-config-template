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

var configFiles = FindConfigurationFiles(targetDirectory, logger);

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

static List<string> FindConfigurationFiles(string directory, ILogger logger)
{
    var files = new List<string>();

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

        logger.LogDebug("Pattern '{Pattern}' found {Count} files (after exclusions)", pattern, matchingFiles.Length);
        files.AddRange(matchingFiles);
    }

    // Exclude template files we might have already generated
    files = [.. files.Where(f => !f.EndsWith(".template.json"))];

    return files;
}

static bool IsInExcludedDirectory(string filePath, string[] excludedDirectories)
{
    var pathParts = filePath.Split(
        [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
        StringSplitOptions.RemoveEmptyEntries);

    return excludedDirectories.Any(excluded =>
        pathParts.Contains(excluded, StringComparer.OrdinalIgnoreCase));
}
