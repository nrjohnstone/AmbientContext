#addin "Newtonsoft.Json&version=11.0.1"
#addin "Cake.Powershell&version=0.4.3"
#addin "Cake.Incubator&version=1.7.2"
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
    var settings = new DotNetCoreCleanSettings
     {         
         Configuration = configuration      
     };

    DotNetCoreClean(solutionFile, settings);
});


Task("Restore-NuGet-Packages")
    .Does(() =>
{
    DotNetCoreRestore(solutionFile);
    NuGetRestore(solutionFile);
});


Task("Rebuild")
    .IsDependentOn("Clean")
    .IsDependentOn("Update-Version")
    .IsDependentOn("Restore-NuGet-Packages")
    .IsDependentOn("Build");


Task("Build")
    .IsDependentOn("Update-Version")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        NoRestore = true    
    };

    DotNetCoreBuild(solutionFile, settings);
});


Task("Pack-Nuget")
    .Does(() => 
{
    var nugetPackageDir = Directory("./artifacts");
    EnsureDirectoryExists(nugetPackageDir);    
    var version = GitVersion().NuGetVersionV2;

    var settings = new DotNetCorePackSettings
    {
        ArgumentCustomization = args=>args.Append("/p:PackageVersion=" + version),
        Configuration = configuration,
        OutputDirectory = nugetPackageDir,
        NoRestore = true,
        IncludeSymbols = true
    };

    DotNetCorePack("./src/AmbientContext/AmbientContext.csproj", settings);
});


Task("Run-Unit-Tests")
    .Does(() =>
{
    var netCoreTestSettings = new DotNetCoreTestSettings() {
        Configuration = configuration,
        NoBuild = true,
        NoRestore = true
    };

     var netCoreXunitTestSettings = new XUnit2Settings {
            HtmlReport = false,
            UseX86 = true
    };

    DotNetCoreTest(netCoreTestSettings, "./test/AmbientContext.Tests/AmbientContext.Tests.csproj", netCoreXunitTestSettings);
});


Task("Update-Version")
    .Does(() => 
{
     try 
    {    
        var assemblyInfoFile = Directory("./src/AmbientContext") + File("Properties/AssemblyVersionInfo.cs");
        if (!FileExists(assemblyInfoFile))
        {
            Information("Assembly version file does not exist : " + assemblyInfoFile.Path);
            CopyFile("./src/AssemblyVersionInfo.template.cs", assemblyInfoFile);
        }
        
        GitVersion(new GitVersionSettings { 
            NoFetch = false,
            OutputType = GitVersionOutput.BuildServer,
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFilePath = assemblyInfoFile });                    
    }
    catch (Exception ex) {
        Information(ex.ToString());
        // Assume that we might be in a pull request build which cannot have the version calculated
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
