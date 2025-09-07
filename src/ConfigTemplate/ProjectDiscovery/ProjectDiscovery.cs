using ConfigTemplate.ProjectDiscovery.Models;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;

namespace ConfigTemplate.ProjectDiscovery;

public class ProjectDiscovery(ILogger<ProjectDiscovery> logger)
{
    public List<ProjectConfig> FindProjects(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            logger.LogError("Directory path cannot be null or empty.");
            return [];
        }

        if (!Directory.Exists(directoryPath))
        {
            logger.LogError("Directory does not exist: {DirectoryPath}", directoryPath);
            return [];
        }

        var projects = new List<ProjectConfig>();

        // Directories to skip during search (common build/cache directories)
        var skipDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", ".vs", ".git", ".svn", "node_modules", "packages",
            ".vscode", ".idea", "TestResults", ".nuget", "artifacts",
            "runtimes", "ref", "refint", "staticwebassets", ".azurefunctions"
        };

        SearchDirectory(directoryPath, projects, skipDirectories);

        projects.ForEach(project =>
        {
            project.ConfigFiles = GetConfigFiles(project.DirectoryPath);
            project.UserSecretsFile = GetUserSecretsFile(project.FilePath);
        });

        logger.LogDebug("Found {ProjectCount} projects", projects.Count);

        return projects;
    }

    private void SearchDirectory(string currentPath, List<ProjectConfig> projects, HashSet<string> skipDirectories)
    {
        try
        {
            logger.LogDebug("Searching directory: {CurrentPath}", currentPath);

            var projectFiles = Directory.GetFiles(
                currentPath, 
                $"*.csproj", 
                SearchOption.TopDirectoryOnly);

            projects.AddRange(projectFiles.Select(projectFile => new ProjectConfig
            {
                FilePath = projectFile,
                FileName = Path.GetFileName(projectFile),
                DirectoryPath = Path.GetDirectoryName(projectFile)!
            }));

            // If we found a .csproj file in this directory, we typically don't need to go deeper
            // since .NET projects don't usually nest project files within the same project structure
            if (projectFiles.Length > 0)
                return;

            // Get subdirectories and filter out the ones we want to skip
            var subdirectories = Directory.GetDirectories(currentPath)
                .Where(dir => !skipDirectories.Contains(Path.GetFileName(dir)))
                .ToArray();

            // Recursively search subdirectories
            foreach (var subdirectory in subdirectories)
            {
                SearchDirectory(subdirectory, projects, skipDirectories);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Access denied to directory: {CurrentPath}", currentPath);
        }
        catch (DirectoryNotFoundException ex)
        {
            logger.LogWarning(ex, "Directory not found during search: {CurrentPath}", currentPath);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching directory: {CurrentPath}", currentPath);
        }
    }

    private static List<string> GetConfigFiles(string projectDir)
    {
        var configFiles = new List<string>();

        var appSettings = Directory.GetFiles(projectDir, "appsettings*.json", SearchOption.TopDirectoryOnly)
            .Where(f => !f.EndsWith(".template.json"));
        configFiles.AddRange(appSettings);

        var localSettings = Path.Combine(projectDir, "local.settings.json");
        if (File.Exists(localSettings))
        {
            configFiles.Add(localSettings);
        }

        return configFiles;
    }

    private string? GetUserSecretsFile(string projectFile)
    {
        try
        {
            var userSecretsId = GetUserSecretsId(projectFile);
            if (!string.IsNullOrEmpty(userSecretsId))
            {
                var secretsPath = GetUserSecretsPath(userSecretsId);
                if (File.Exists(secretsPath))
                {
                    logger.LogDebug("Found user secrets for project {Project}: {SecretsPath}",
                        Path.GetFileName(projectFile), secretsPath);
                    return secretsPath;
                }
                else
                {
                    logger.LogDebug("User secrets configured for {Project} but file doesn't exist: {SecretsPath}",
                        Path.GetFileName(projectFile), secretsPath);
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to check user secrets for project {ProjectFile}", projectFile);
            return null;
        }
    }

    private static string? GetUserSecretsId(string projectFilePath)
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

    private static string GetUserSecretsPath(string userSecretsId)
    {
        var userSecretsRoot = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".microsoft", "usersecrets");

        return Path.Combine(userSecretsRoot, userSecretsId, "secrets.json");
    }

    private static bool IsInExcludedDirectory(string filePath, string[] excludedDirectories)
    {
        var pathParts = filePath.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        return excludedDirectories.Any(excluded =>
            pathParts.Contains(excluded, StringComparer.OrdinalIgnoreCase));
    }
}
