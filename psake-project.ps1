Framework 4.5.1
Include "packages\Hangfire.Build.0.2.6\tools\psake-common.ps1"

Properties {
    $solution = "stdump.sln"
}

Task Default -Depends Pack

Task CleanCore {
    Exec { dotnet clean /v:minimal }
}

Task CompileCore -Depends CleanCore, Restore -Description "Compile all the projects in a solution." {
    Exec { dotnet publish -f net451 -r win7-x86 -c Release }
    Exec { dotnet publish -f net451 -r win7-x64 -c Release }
}

Task Merge -Depends CompileCore -Description "Run ILMerge /internalize to merge assemblies." {
    Repack-Exe @("stdump", "net451\win7-x86") @("Microsoft.Diagnostics.Runtime")
    Repack-Exe @("stdump", "net451\win7-x64") @("Microsoft.Diagnostics.Runtime")
}

Task Collect -Depends Merge -Description "Copy all artifacts to the build folder." {
    Write-Host "Copying 'stdump-x86.exe'..." -ForegroundColor "Green"
    Copy-Files ((Get-SrcOutputDir "stdump" "net451\win7-x86") + "\stdump.exe") "$build_dir\stdump-x86.exe"
    
    Write-Host "Copying 'stdump.exe'..." -ForegroundColor "Green"
    Copy-Files ((Get-SrcOutputDir "stdump" "net451\win7-x64") + "\stdump.exe") "$build_dir\stdump.exe"

    Write-Host "Copying LICENSE.md" -ForegroundColor "Green"
    Copy-Files "$base_dir\LICENSE" $build_dir
}

Task Pack -Depends Collect -Description "Create NuGet packages and archive files." {
    $version = Get-PackageVersion

    Create-Archive "stdump-$version"
    Create-Package "stdump" $version
}

function Repack-Exe($projectWithOptionalTarget, $internalizeAssemblies, $target) {
    $project = $projectWithOptionalTarget
    $target = $null

    $base_dir = resolve-path .
    $ilrepack = "$base_dir\packages\ilrepack.*\tools\ilrepack.exe"

    if ($projectWithOptionalTarget -Is [System.Array]) {
        $project = $projectWithOptionalTarget[0]
        $target = $projectWithOptionalTarget[1]
    }

    Write-Host "Merging '$project'/$target with $internalizeAssemblies..." -ForegroundColor "Green"

    $internalizePaths = @()

    $projectOutput = Get-SrcOutputDir $project $target

    foreach ($assembly in $internalizeAssemblies) {
        $internalizePaths += "$assembly.dll"
    }

    $primaryAssemblyPath = "$project.exe"
    $temp_dir = "$base_dir\temp"

    Create-Directory $temp_dir

    Push-Location
    Set-Location -Path $projectOutput

    Exec { .$ilrepack /out:"$temp_dir\$project.exe" /internalize $primaryAssemblyPath $internalizePaths }

    Pop-Location

    Move-Files "$temp_dir\$project.*" $projectOutput
}
