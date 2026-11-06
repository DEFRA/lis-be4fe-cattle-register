#load "version.cake"

var target = Argument("target", "Default");
var tagVersion = Argument("tag_version", "");

Task("CreateTag")
    .Description("Calculates the version, creates a Git tag, and pushes it")
    .Does(() => {
        var version = tagVersion;
        var tag = version;
        var message = $"Release version {version}";

        Information($"Calculated Version: {version}");

        if (GitTagExists(tag))
        {
            Warning($"Tag {tag} already exists. Skipping tagging.");
            return;
        }

        Information($"Creating tag: {tag}");
        RunGit($"tag -a {tag} -m \"{message}\"");

        Information($"Pushing tag: {tag}");
        RunGit($"push origin {tag}");
    });

Task("Default")
    .IsDependentOn("CreateTag");

RunTarget(target);
