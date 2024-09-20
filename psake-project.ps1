Include "packages\Hangfire.Build.0.5.0\tools\psake-common.ps1"

Task Default -Depends Pack

Task Publish -Depends Restore -Description "Compile all the projects in a solution." {
    Exec { dotnet publish --no-restore -f net461 -r win7-x86 -c Release }
    Exec { dotnet publish --no-restore -f net461 -r win7-x64 -c Release }
    Exec { dotnet publish --no-restore -f netcoreapp3.1 -c Release }
    Exec { dotnet publish --no-restore -f net5.0 -c Release }
    Exec { dotnet publish --no-restore -f net6.0 -c Release }
    Exec { dotnet publish --no-restore -f net8.0 -c Release }
}

Task Merge -Depends Publish -Description "Run ILMerge /internalize to merge assemblies." {
    Repack-Exe @("stdump", "net461\win7-x86") @("Microsoft.Diagnostics.Runtime", "Microsoft.Extensions.CommandLineUtils", "System.Collections.Immutable", "System.Memory", "System.Runtime.CompilerServices.Unsafe", "System.Buffers", "System.Reflection.Metadata", "System.Runtime.InteropServices.RuntimeInformation", "System.ValueTuple", "System.Numerics.Vectors")
    Repack-Exe @("stdump", "net461\win7-x64") @("Microsoft.Diagnostics.Runtime", "Microsoft.Extensions.CommandLineUtils", "System.Collections.Immutable", "System.Memory", "System.Runtime.CompilerServices.Unsafe", "System.Buffers", "System.Reflection.Metadata", "System.Runtime.InteropServices.RuntimeInformation", "System.ValueTuple", "System.Numerics.Vectors")
}

Task Collect -Depends Merge -Description "Copy all artifacts to the build folder." {
    Write-Host "Copying 'stdump.dll'..." -ForegroundColor "Green"

    Create-Directory "$build_dir\netcoreapp3.1\any"
    Copy-Files ((Get-SrcOutputDir "stdump" "netcoreapp3.1\publish") + "\*") "$build_dir\netcoreapp3.1\any"
    Copy-Files "$base_dir\DotnetToolSettings.xml" "$build_dir\netcoreapp3.1\any"

    Create-Directory "$build_dir\net5.0\any"
    Copy-Files ((Get-SrcOutputDir "stdump" "net5.0\publish") + "\*") "$build_dir\net5.0\any"
    Copy-Files "$base_dir\DotnetToolSettings.xml" "$build_dir\net5.0\any"

    Create-Directory "$build_dir\net6.0\any"
    Copy-Files ((Get-SrcOutputDir "stdump" "net6.0\publish") + "\*") "$build_dir\net6.0\any"
    Copy-Files "$base_dir\DotnetToolSettings.xml" "$build_dir\net6.0\any"

    Create-Directory "$build_dir\net8.0\any"
    Copy-Files ((Get-SrcOutputDir "stdump" "net8.0\publish") + "\*") "$build_dir\net8.0\any"
    Copy-Files "$base_dir\DotnetToolSettings.xml" "$build_dir\net8.0\any"

    Write-Host "Copying 'stdump.exe'..." -ForegroundColor "Green"
    Copy-Files ((Get-SrcOutputDir "stdump" "net461\win7-x64") + "\stdump.exe") "$build_dir\stdump.exe"

    Write-Host "Copying 'stdump-x86.exe'..." -ForegroundColor "Green"
    Copy-Files ((Get-SrcOutputDir "stdump" "net461\win7-x86") + "\stdump.exe") "$build_dir\stdump-x86.exe"

    Write-Host "Copying LICENSE.txt" -ForegroundColor "Green"
    Copy-Files "$base_dir\LICENSE.txt" $build_dir

    Collect-File "README.md"
}

Task Pack -Depends Collect -Description "Create NuGet packages and archive files." {
    $version = Get-PackageVersion

    Create-Package "stdump" $version
    Create-Archive "stdump-$version"
}

Task Sign -Depends Pack -Description "Sign artifacts." {
    $version = Get-PackageVersion

    Sign-ArchiveContents "stdump-$version" "hangfire" "stdump-package-zip-file"
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
