#tool "nuget:?package=ilmerge"

var target = Argument("target", "Default");

Task("Clean")
  .Does(() =>
  {
    MSBuild("./stdump.sln", config => config
      .WithTarget("Clean"));
  });

Task("Compile")
  .IsDependentOn("Clean")
  .Does(() =>
  {
    MSBuild("./stdump.sln", config => config
      .SetConfiguration("Release")
      .WithProperty("Platform", "x64"));

    MSBuild("./stdump.sln", config => config
      .SetConfiguration("Release")
      .WithProperty("Platform", "x86"));
  });

Task("Merge")
  .IsDependentOn("Compile")
  .Does(() =>
  {
    CreateDirectory("./artifacts");

    var assemblyPaths = GetFiles("./packages/**/net40/Microsoft.Diagnostics.Runtime.dll");

    ILMerge("./artifacts/stdump.exe", "./stdump/bin/x64/Release/stdump.exe", assemblyPaths);
    ILMerge("./artifacts/stdump-x86.exe", "./stdump/bin/x86/Release/stdump.exe", assemblyPaths);
  });

Task("Default")
  .IsDependentOn("Merge");

RunTarget(target);
