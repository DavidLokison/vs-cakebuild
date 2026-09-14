using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core;
using Cake.Frosting;
using Cake.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public string ProjectName { get; }
    public string BuildConfiguration { get; set; }
    public bool SkipJsonValidation { get; set; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        if (context.HasArgument("project"))
        {
            ProjectName = context.Argument<string>("project");
        }
        else
        {
            var projectFiles = context.GetFiles("*.csproj");
            if (projectFiles.Count > 1)
            {
                throw new Exception("Found more than one .csproj file. Automatic project selection failed.");
            }
            foreach (var file in projectFiles)
            {
                ProjectName = file.GetFilenameWithoutExtension().ToString();
            }
        }
        BuildConfiguration = context.Argument("configuration", "Release");
        SkipJsonValidation = context.Argument("skipJsonValidation", false);
    }
}

[TaskName("ValidateJson")]
public sealed class ValidateJsonTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (context.SkipJsonValidation)
        {
            return;
        }
        var jsonFiles = context.GetFiles($"assets/**/*.json");
        foreach (var file in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(file.FullPath);
                JToken.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Validation failed for JSON file: {file.FullPath}{Environment.NewLine}{ex.Message}", ex);
            }
        }
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(ValidateJsonTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetClean($"{context.ProjectName}.csproj",
            new DotNetCleanSettings
            {
                Configuration = context.BuildConfiguration
            });

        context.DotNetPublish($"{context.ProjectName}.csproj",
            new DotNetPublishSettings
            {
                Configuration = context.BuildConfiguration
            });
    }
}

[TaskName("Package")]
[IsDependentOn(typeof(BuildTask))]
public sealed class PackageTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.EnsureDirectoryExists($"dist/{context.BuildConfiguration}");
        context.CleanDirectory($"dist/{context.BuildConfiguration}");
        Assembly mod = Assembly.LoadFrom($"bin/{context.BuildConfiguration}/{context.ProjectName}.dll");
        ModInfoAttribute modInfoAttr = mod.GetCustomAttribute<ModInfoAttribute>();
        List<ModDependency> dependencies = new List<ModDependency>();
        foreach (ModDependencyAttribute depsAttr in mod.GetCustomAttributes<ModDependencyAttribute>())
        {
            dependencies.Add(new ModDependency(depsAttr.ModID, depsAttr.Version));
        }
        ModInfo modInfo = new ModInfo(
                EnumModType.Code,
                modInfoAttr.Name,
                modInfoAttr.ModID,
                modInfoAttr.Version,
                modInfoAttr.Description,
                modInfoAttr.Authors,
                modInfoAttr.Contributors,
                modInfoAttr.Website,
                Enum.Parse<EnumAppSide>(modInfoAttr.Side),
                modInfoAttr.RequiredOnClient,
                modInfoAttr.RequiredOnServer,
                dependencies
                ) {
            NetworkVersion = modInfoAttr.NetworkVersion,
            IconPath = modInfoAttr.IconPath,
        };
        context.CopyDirectory($"bin/{context.BuildConfiguration}/publish", $"dist/{context.BuildConfiguration}/{modInfo.ModID}");
        context.SerializeJsonToPrettyFile<ModInfo>($"dist/{context.BuildConfiguration}/{modInfo.ModID}/modinfo.json", modInfo);
        if (modInfoAttr.WorldConfig != null)
        {
            ModWorldConfiguration modWorldConfig = context.DeserializeJson<ModWorldConfiguration>(modInfoAttr.WorldConfig);
            context.SerializeJsonToPrettyFile<ModWorldConfiguration>($"dist/{context.BuildConfiguration}/{modInfo.ModID}/worldconfig.json", modWorldConfig);
        }
        if (context.DirectoryExists("assets"))
        {
            context.CopyDirectory($"assets", $"dist/{context.BuildConfiguration}/{modInfo.ModID}/assets");
        }
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(PackageTask))]
public class DefaultTask : FrostingTask
{
}
