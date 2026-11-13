[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameDirectory,
    [Parameter(Mandatory = $true)]
    [string]$ModDll,
    [Parameter(Mandatory = $true)]
    [string]$CecilDll,
    [string[]]$ReferenceDirectory = @()
)

$ErrorActionPreference = 'Stop'
Add-Type -LiteralPath (Resolve-Path -LiteralPath $CecilDll).Path

$gameData = Join-Path $GameDirectory 'data_sts2_windows_x86_64'
$gameDll = (Resolve-Path -LiteralPath (Join-Path $gameData 'sts2.dll')).Path
$modPath = (Resolve-Path -LiteralPath $ModDll).Path
$resolver = [Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory($gameData)
$resolver.AddSearchDirectory((Split-Path -Parent $modPath))
foreach ($directory in $ReferenceDirectory) {
    $resolver.AddSearchDirectory((Resolve-Path -LiteralPath $directory).Path)
}

$reader = [Mono.Cecil.ReaderParameters]::new()
$reader.AssemblyResolver = $resolver
$modAssembly = $null
$gameAssembly = $null

function Get-AllTypes {
    param($Types)
    foreach ($type in $Types) {
        $type
        if ($type.HasNestedTypes) {
            Get-AllTypes -Types $type.NestedTypes
        }
    }
}

function Get-ParameterKey {
    param($Method)
    return ($Method.Parameters | ForEach-Object { $_.ParameterType.FullName }) -join '|'
}

try {
    $gameAssembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($gameDll, $reader)
    $modAssembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($modPath, $reader)
    $missingMembers = [System.Collections.Generic.List[string]]::new()
    $unresolvedMembers = [System.Collections.Generic.List[string]]::new()
    $unmatchedOverrides = [System.Collections.Generic.List[string]]::new()
    $unresolvedBases = [System.Collections.Generic.HashSet[string]]::new()
    $memberCount = 0
    $overrideCount = 0

    foreach ($member in $modAssembly.MainModule.GetMemberReferences()) {
        if ($member.DeclaringType.Scope.Name -ne $gameAssembly.Name.Name) {
            continue
        }

        $memberCount++
        try {
            if ($null -eq $member.Resolve()) {
                $missingMembers.Add($member.FullName)
            }
        }
        catch {
            $unresolvedMembers.Add($member.FullName + ': ' + $_.Exception.Message)
        }
    }

    foreach ($type in (Get-AllTypes -Types $modAssembly.MainModule.Types)) {
        foreach ($method in $type.Methods) {
            if (!$method.IsVirtual -or $method.IsNewSlot -or $method.HasOverrides) {
                continue
            }

            $overrideCount++
            $baseReference = $type.BaseType
            $found = $false
            $incomplete = $false
            $parameterKey = Get-ParameterKey -Method $method
            $visitedBases = [System.Collections.Generic.HashSet[string]]::new()

            while ($null -ne $baseReference -and !$found) {
                if (!$visitedBases.Add($baseReference.FullName)) {
                    [void]$unresolvedBases.Add('Base type cycle: ' + $type.FullName)
                    $incomplete = $true
                    break
                }
                try {
                    $baseType = $baseReference.Resolve()
                    if ($null -eq $baseType) {
                        throw "Base type not found: $($baseReference.FullName)"
                    }
                }
                catch {
                    [void]$unresolvedBases.Add($baseReference.FullName + ': ' + $_.Exception.Message)
                    $incomplete = $true
                    break
                }

                foreach ($candidate in $baseType.Methods) {
                    if ($candidate.IsVirtual -and
                        $candidate.Name -eq $method.Name -and
                        $candidate.GenericParameters.Count -eq $method.GenericParameters.Count -and
                        (Get-ParameterKey -Method $candidate) -eq $parameterKey) {
                        $found = $true
                        break
                    }
                }
                $baseReference = $baseType.BaseType
            }

            if (!$found -and !$incomplete) {
                $unmatchedOverrides.Add($method.FullName)
            }
        }
    }

    [pscustomobject]@{
        ModDll = $modPath
        GameDll = $gameDll
        GameMemberReferencesChecked = $memberCount
        MissingGameMembers = $missingMembers.ToArray()
        UnresolvedGameMembers = $unresolvedMembers.ToArray()
        VirtualOverridesChecked = $overrideCount
        UnmatchedOverrideCandidates = $unmatchedOverrides.ToArray()
        UnresolvedBaseTypes = @($unresolvedBases | Sort-Object)
        Limitations = 'Metadata only; excludes reflection/Harmony targets, explicit overrides, resources, and behavior. Generic base substitutions and covariant returns need manual review.'
    } | ConvertTo-Json -Depth 5
}
finally {
    if ($null -ne $modAssembly) { $modAssembly.Dispose() }
    if ($null -ne $gameAssembly) { $gameAssembly.Dispose() }
    $resolver.Dispose()
}
