using ConfigTemplate.ProjectDiscovery.Models;
using Microsoft.Extensions.Logging;

namespace ConfigTemplate.TemplateGeneration;

public class TemplateGeneration(ILogger<TemplateGeneration> logger)
{
    public void GenerateTemplates(string targetDirectory, IReadOnlyList<ProjectConfig> projects)
    {
        if (projects.Count == 0) return;

        foreach (var project in projects)
        {
            try
            {
                var relativePath = Path.GetRelativePath(targetDirectory, project.DirectoryPath);
                logger.LogDebug("Processing project: {RelativePath}", relativePath);

                foreach (var configFile in project.ConfigFiles)
                {
                    var configRelativePath = Path.GetRelativePath(targetDirectory, configFile);
                    logger.LogDebug("  - Config file: {ConfigFile}", configRelativePath);
                }

                if (project.UserSecretsFile != null)
                {
                    logger.LogDebug("  - User secrets: {SecretsFile}", Path.GetFileName(project.UserSecretsFile));
                }

                // TODO(sergio): generate template file
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing project {RelativePath}", project.DirectoryPath);
            }
        }

        logger.LogInformation("Template generation complete");
    }
}
