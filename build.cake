#nullable enable
#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=5.5.1
#load "base.cake"
#load "version.cake"

var target = Argument("target", "Default");
var version = Argument("package_version", EnvironmentVariable("PACKAGE_VERSION") ?? "");
var imageName = Argument("image_name", EnvironmentVariable("IMAGE_NAME") ?? "");
var imageRef = Argument("image_ref", EnvironmentVariable("IMAGE_REF") ?? "");
var revision = Argument("revision", EnvironmentVariable("REVISION") ?? "");
private readonly string SOLUTION_FILE = GetSolutionFile();
private const string CONFIGURATION = "release";
private const DotNetVerbosity VERBOSITY = DotNetVerbosity.Minimal;

string RequiredValue(string value, string name)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new Exception($"{name} is required to build the container image.");
    }

    return value;
}

Action buildContainerImage = () =>
{
    var resolvedImageName = RequiredValue(imageName, "IMAGE_NAME");
    var resolvedImageRef = string.IsNullOrWhiteSpace(imageRef)
        ? $"{resolvedImageName}:{version}"
        : imageRef;
    var resolvedRevision = RequiredValue(revision, "REVISION");
    var repository = EnvironmentVariable("GITHUB_REPOSITORY") ?? resolvedImageName;
    var serverUrl = EnvironmentVariable("GITHUB_SERVER_URL") ?? "https://github.com";
    var runId = EnvironmentVariable("GITHUB_RUN_ID") ?? "local";

    Information($"Building production container image {resolvedImageRef}");
    RunCommand(
        "docker",
        "buildx build . " +
        "--file ./Dockerfile " +
        "--target production " +
        "--no-cache --provenance=false --sbom=false --load " +
        $"--tag \"{resolvedImageRef}\" " +
        $"--label \"defra.cdp.git.repo.url={serverUrl}/{repository}\" " +
        $"--label \"defra.cdp.git.repo.name={repository}\" " +
        $"--label \"defra.cdp.service.name={resolvedImageName}\" " +
        $"--label \"defra.cdp.build.run_id={runId}\" " +
        "--label \"defra.cdp.run_mode=service\" " +
        $"--label \"git.hash={resolvedRevision}\" " +
        $"--label \"org.opencontainers.image.version={version}\"");
};

Task("Clean")
    .Does(() => 
    {
        var settings = new DotNetCleanSettings
        {
            Verbosity = VERBOSITY,
            Configuration = CONFIGURATION
        };
        DotNetClean(SOLUTION_FILE, settings);
    });

Task("Version")
    .IsDependentOn("Clean")
    .Description("Calculates the npm package version")
    .Does(() =>
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            version = CalculateVersion();
        }

        Information($"Version {version}");
    });

Task("Install")
    .IsDependentOn("Version")
    .Description("Restoring the solution dependencies")
    .Does(() => {
        Information("Restoring the solution dependencies");
        var settings = new DotNetRestoreSettings
        {
            Verbosity = VERBOSITY,
            Sources = new [] 
            { 
                "https://api.nuget.org/v3/index.json",
            }
        };
        DotNetRestore(SOLUTION_FILE, settings);
    });

Task("Format")
    .IsDependentOn("Install")
    .Description("Executing dotnet format")
    .Does(() => {
        var settings = new DotNetFormatSettings
        {
            VerifyNoChanges = false
        };

        DotNetFormat(SOLUTION_FILE, settings);
    });

Task("Build")
    .IsDependentOn("Format")
    .IsDependentOn("Version")
    .Does(() => {
        var settings = new DotNetBuildSettings {
            Verbosity = VERBOSITY,
            Configuration = CONFIGURATION,
            ArgumentCustomization = args => args
                .Append($"/p:Version={version}")
                .Append("/p:WarningsAsErrors=true")
            };
        DotNetBuild(SOLUTION_FILE, settings);
     });

Task("Test")
    .IsDependentOn("Build")
    .Does(() => {
        var testSettings = new DotNetTestSettings  {
            Verbosity = VERBOSITY,
            Configuration = CONFIGURATION,
        };
        var coverageOutput = DirectoryPath.FromString("./coverage");
        
        var testProjects = GetFiles("./tests/**/*.csproj");
        if (!testProjects.Any())
        {
            testProjects = GetFiles("./tests/**/*.csproj");
        }

        testProjects.ToList().ForEach(project => {
            Information($"Testing Project : {project.ToString()}");
            
            Information($"Running Tests : { project.ToString()}");
            var projectCoverageDirectory = DirectoryPath.FromString(
                project.GetFilenameWithoutExtension().ToString());
            testSettings.ResultsDirectory = coverageOutput.Combine(projectCoverageDirectory);
            testSettings.ArgumentCustomization = args => args
                .AppendQuoted("--collect:XPlat Code Coverage")
                .Append("--logger trx");
            DotNetTest(project.ToString(), testSettings);
        });
        
        Information($"Directory Path : { coverageOutput.ToString()}");
        var glob = new GlobPattern($"./{coverageOutput}/**/coverage.cobertura.xml");
                 
        Information($"globpattern : { glob.ToString()}");
        var outputDirectory = Directory($"./coverage/reports");
        
        Information($"output Directory : { outputDirectory}");
        var reportSettings = new ReportGeneratorSettings
        {
            ArgumentCustomization = args => args.Append($"-reportTypes:HtmlInline_AzurePipelines_Dark;Cobertura")
        };
        
        ReportGenerator(glob, outputDirectory, reportSettings);
    });

Task("Pack")
    .IsDependentOn("Test")
    .Description("Validates the application and builds its production container image")
    .Does(buildContainerImage);

Task("PackOnly")
    .IsDependentOn("Version")
    .Description("Builds the production container image from previously validated source")
    .Does(buildContainerImage);

Task("Default")
    .IsDependentOn("Pack");

RunTarget(target);
