Framework 4.5.1
Include "packages\Hangfire.Build.0.2.5\tools\psake-common.ps1"

Properties {
    $solution = "stdump.sln"
}

Task Default -Depends Pack

Task CompileAll -Depends Clean, Restore -Description "Compile all the projects in a solution." {
    Run-MSBuild "x86"
    Run-MSBuild "x64"
}

Task Merge -Depends CompileAll -Description "Run ILMerge /internalize to merge assemblies." {
    Run-ILMerge @("stdump", "x86") @("Microsoft.Diagnostics.Runtime")
    Run-ILMerge @("stdump", "x64") @("Microsoft.Diagnostics.Runtime")
}

Task Collect -Depends Merge -Description "Copy all artifacts to the build folder." {
    Write-Host "Copying 'stdump-x86.exe'..." -ForegroundColor "Green"
    Copy-Files ((Get-SrcOutputDir "stdump" "x86") + "\stdump.exe") "$build_dir\stdump-x86.exe"
    
    Write-Host "Copying 'stdump-x64.exe'..." -ForegroundColor "Green"
    Copy-Files ((Get-SrcOutputDir "stdump" "x64") + "\stdump.exe") "$build_dir\stdump-x64.exe"

    Write-Host "Copying LICENSE.md" -ForegroundColor "Green"
    Copy-Files "$base_dir\LICENSE" $build_dir
}

Task Pack -Depends Collect -Description "Create NuGet packages and archive files." {
    $version = Get-PackageVersion

    Create-Archive "stdump-$version"
    Create-Package "stdump" $version
}

function Run-MSBuild($platform) {
    Write-Host "Compiling '$solution' using $platform platform..." -ForegroundColor "Green"

    $extra = $null
    if ($env:APPVEYOR) {
        $extra = "/logger:C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"
    }

    Exec { msbuild $solution_path /p:Configuration=$config /p:Platform=$platform /nologo /verbosity:minimal $extra }
}

function Run-ILMerge($projectWithOptionalTarget, $internalizeAssemblies) {
    $project = $projectWithOptionalTarget
    $target = $null

    if ($projectWithOptionalTarget -Is [System.Array]) {
        $project = $projectWithOptionalTarget[0]
        $target = $projectWithOptionalTarget[1]
    }

    Write-Host "Merging '$project' ($target) with $internalizeAssemblies..." -ForegroundColor "Green"

    $internalizePaths = @()
    $projectOutput = Get-SrcOutputDir $project $target

    foreach ($assembly in $internalizeAssemblies) {
        $internalizePaths += "$projectOutput\$assembly.dll"
    }

    $primaryAssemblyPath = "$projectOutput\$project.exe"

    Create-Directory $temp_dir
    
    Exec { .$ilmerge /targetplatform:"v4,$framework_dir" `
        /out:"$temp_dir\$project.exe" `
        /target:exe `
        /internalize `
        $primaryAssemblyPath `
        $internalizePaths `
    }

    Move-Files "$temp_dir\$project.*" $projectOutput
}
