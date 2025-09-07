using System;

record ProjectConfig(
    string ProjectFile,
    ProjectType Type,
    IReadOnlyList<string> ConfigFiles,
    string? UserSecretsFile = null);
