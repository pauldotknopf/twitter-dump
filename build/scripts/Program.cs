using Build.Buildary;
using static Build.Buildary.Log;
using static Build.Buildary.GitVersion;
using static Build.Buildary.Path;
using static Build.Buildary.Directory;
using static Build.Buildary.File;
using static Build.Buildary.Shell;
using static Bullseye.Targets;

namespace Build
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = Runner.ParseOptions<Runner.RunnerOptions>(args);
            
            Info($"Configuration: {options.Config}");
            var gitVersion = GetGitVersion(ExpandPath("./"));
            Info($"Version: {gitVersion.FullVersion}");
            
            var commandBuildArgs = $"--configuration {options.Config}";
            var commandBuildArgsWithVersion = commandBuildArgs;
            if (!string.IsNullOrEmpty(gitVersion.PreReleaseTag))
            {
                commandBuildArgsWithVersion += $" --version-suffix \"{gitVersion.PreReleaseTag}\"";
            }
            
            Target("clean", () =>
            {
                CleanDirectory(ExpandPath("./output"));
            });
            
            Target("update-version", () =>
            {
                if (FileExists("./build/version.props"))
                {
                    DeleteFile("./build/version.props");
                }
                
                WriteFile("./build/version.props",
                    $@"<Project>
    <PropertyGroup>
        <VersionPrefix>{gitVersion.Version}</VersionPrefix>
    </PropertyGroup>
</Project>");
            });

            Target("build", () =>
            {
                RunShell($"dotnet build {commandBuildArgs} {ExpandPath("./AptTool.sln")}");
            });

            Target("deploy", () =>
            {
                RunShell($"dotnet pack --output {ExpandPath("./output")} {commandBuildArgsWithVersion} {ExpandPath("./src/TwitterDump/TwitterDump.csproj")}");
            });
            
            Target("ci", DependsOn("clean", "update-version", "deploy"));
            
            Target("default", DependsOn("build"));
            
            Runner.Execute(options);
        }
    }
}