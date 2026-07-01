using ModAgent.Abstractions;
using System.Reflection;
using System.Text.Json;

namespace ModAgent.Mod.Sdk;

public sealed record ModManifestDocument(
    string Id,
    string Name,
    string Version,
    string Description,
    string Author,
    string HostApiVersionRange,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> Permissions,
    ModCapability Capabilities);

public static class ModManifestWriter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static string ToJson(IModManifest manifest)
    {
        var document = new ModManifestDocument(
            manifest.Id,
            manifest.Name,
            manifest.Version,
            manifest.Description,
            manifest.Author,
            manifest.HostApiVersionRange,
            manifest.Dependencies,
            manifest.Permissions,
            manifest.Capabilities);

        return JsonSerializer.Serialize(document, Options);
    }

    public static async Task WriteAsync(IModManifest manifest, string outputPath, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(outputPath, ToJson(manifest), cancellationToken).ConfigureAwait(false);
    }
}

public static class ModAssemblyInspector
{
    public static Type? FindEntryType(Assembly assembly)
    {
        return assembly.GetTypes()
            .FirstOrDefault(type => type.GetCustomAttribute<ModEntryAttribute>() is not null && typeof(IMod).IsAssignableFrom(type));
    }

    public static IReadOnlyList<string> ReadDeclaredPermissions(Assembly assembly)
    {
        return assembly.GetCustomAttributes<ModPermissionAttribute>()
            .Select(attribute => attribute.Permission)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permission => permission, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public static class ModPackageValidator
{
    public static IReadOnlyList<string> Validate(IModManifest manifest)
    {
        var errors = new List<string>();

        Require(manifest.Id, "Manifest Id is required.");
        Require(manifest.Name, "Manifest Name is required.");
        Require(manifest.Version, "Manifest Version is required.");
        Require(manifest.HostApiVersionRange, "HostApiVersionRange is required.");

        if (!Version.TryParse(manifest.Version, out _))
        {
            errors.Add("Manifest Version must be a semantic version compatible value, for example 1.0.0.");
        }

        return errors;

        void Require(string? value, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(message);
            }
        }
    }
}

public sealed class ModManifestBuilder
{
    private readonly List<string> dependencies = new();
    private readonly List<string> permissions = new();

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string HostApiVersionRange { get; set; } = ">=1.0.0 <2.0.0";
    public ModCapability Capabilities { get; set; } = ModCapability.None;

    public ModManifestBuilder AddDependency(string modId)
    {
        dependencies.Add(modId);
        return this;
    }

    public ModManifestBuilder AddPermission(string permission)
    {
        permissions.Add(permission);
        return this;
    }

    public IModManifest Build()
    {
        return new Manifest(Id, Name, Version, Description, Author, HostApiVersionRange, dependencies.ToArray(), permissions.ToArray(), Capabilities);
    }

    private sealed record Manifest(
        string Id,
        string Name,
        string Version,
        string Description,
        string Author,
        string HostApiVersionRange,
        IReadOnlyList<string> Dependencies,
        IReadOnlyList<string> Permissions,
        ModCapability Capabilities) : IModManifest;
}
