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
    .IsDependentOn("Clean")
    .IsDependentOn("Update-Version")
    .IsDependentOn("Restore-NuGet-Packages")
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


Task("Pack-Nuget")
    .Does(() => 
{
    EnsureDirectoryExists("./artifacts");
    string version = GitVersion().NuGetVersion;

    var binDir = Directory("./bin") ;
    var nugetPackageDir = Directory("./artifacts");

    var nugetFilePaths = GetFiles("./src/AmbientContext/*.csproj");

    var nuGetPackSettings = new NuGetPackSettings
    {   
        Version = version,
        BasePath = binDir + Directory(configuration),
        OutputDirectory = nugetPackageDir,
        ArgumentCustomization = args => args.Append("-Prop Configuration=" + configuration)
    };

    NuGetPack(nugetFilePaths, nuGetPackSettings);
});


Task("Run-Unit-Tests")
    .Does(() =>
{
    var testAssemblies = GetFiles(".\\test\\AmbientContext.Tests\\bin\\" + configuration + "\\AmbientContext.Tests.dll");
    Console.WriteLine(testAssemblies.Count());
    XUnit2(testAssemblies);
});


Task("Update-Version")
    .Does(() => 
{
    GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = true});
    string version = GitVersion().NuGetVersion;
	Console.WriteLine("Current NuGetVersion=" + version);
    var projectFiles = System.IO.Directory.EnumerateFiles(@".\", "project.json", SearchOption.AllDirectories).ToArray();

    foreach(var file in projectFiles)
    {
        var project = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(file, Encoding.UTF8));

        project["version"].Replace(version);

        System.IO.File.WriteAllText(file, project.ToString(), Encoding.UTF8);
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
