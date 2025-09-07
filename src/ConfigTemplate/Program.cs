using ConfigTemplate.ProjectDiscovery;
using ConfigTemplate.TemplateGeneration;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
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

logger.LogDebug("Scanning directory: {Directory}", targetDirectory);

var projects = new ProjectDiscovery(loggerFactory.CreateLogger<ProjectDiscovery>())
    .FindProjects(targetDirectory);

foreach (var project in projects)
{
    logger.LogDebug("Found project:\n\t{ProjectFilePath}\n\tConfigFiles: {ConfigFiles}\n\tUserSecretsFile: {UserSecretsFile}",
        project.FilePath,
        string.Join(", ", project.ConfigFiles),
        project.UserSecretsFile);
}

//new TemplateGeneration(loggerFactory.CreateLogger<TemplateGeneration>())
//    .GenerateTemplates(targetDirectory, projects);
