#addin "Newtonsoft.Json"
#addin "Cake.Powershell"
#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=xunit.runner.console"
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var solutionDir = Directory("./");
var solutionFile = solutionDir + File("AmbientContext.sln");
var buildDir = Directory("./src/AmbientContext/bin") + Directory(configuration);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////


Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
});


Task("Restore-NuGet-Packages")
    .Does(() =>
{
    NuGetRestore(solutionFile);
});



Task("Build")
    .IsDependentOn("Update-Version")
    .Does(() =>
{
    DotNetCoreRestore();

    if(IsRunningOnWindows())
    {
      // Use MSBuild
      MSBuild(solutionFile, settings =>
        settings.SetConfiguration(configuration));
    }
    else
    {
      // Use XBuild
      XBuild(solutionFile, settings =>
        settings.SetConfiguration(configuration));
    }
});


Task("Rebuild")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore-NuGet-Packages")
    .IsDependentOn("Build")
    .IsDependentOn("Run-Unit-Tests")
    .Does(() =>
{ });


Task("Run-Unit-Tests")
    .Does(() =>
{
    var testAssemblies = GetFiles(".\\test\\AmbientContext.Tests\\bin\\" + configuration + "\\net451\\*\\AmbientContext.Tests.dll");
    Console.WriteLine(testAssemblies.Count());
    XUnit2(testAssemblies);
});


Task("Get-DNV")
    .ContinueOnError()
    .Does(() =>
{
    if (!FileExists("./tools/dnv.exe")) {
        if (FileExists("./tools/updeps.exe"))
            DeleteFile("./tools/updeps.exe");
        DownloadFile("https://github.com/PhilipDaniels/dotnetversioning/raw/master/dnv.zip", "./tools/dnv.zip");
        Unzip("./tools/dnv.zip", "./tools");
    }
});


Task("Update-Version")
    .Does(() => 
{
    GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = true});
    
    string version = GitVersion().InformationalVersion;
    Environment.SetEnvironmentVariable("GitVersionInformational", version);    
});


Task("Get-DotNetCli")
    .Does(() =>
{         
    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    string dotNetPath = userProfile + @"Local\Microsoft\dotnet";
    
    if (!Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine).Contains(dotNetPath))
    {
        DownloadFile("https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.ps1", "./tools/dotnet-install.ps1");
        var version = "1.0.0-preview2-003121";
        StartPowershellFile("./tools/dotnet-install.ps1", args =>
            {
                args.Append("Version", version);
            });

        
        string path = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine) + ";" + dotNetPath;
        Console.WriteLine(path);
        Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Machine);
        Environment.SetEnvironmentVariable("Path", path);
    }

});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Run-Unit-Tests");
    
//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
