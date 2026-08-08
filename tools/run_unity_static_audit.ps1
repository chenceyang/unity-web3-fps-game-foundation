param(
    [string]$UnityEditorPath = ""
)

$ErrorActionPreference = "Stop"
$validatedUnityVersion = "6000.3.21f1"

if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $runningEditors = @(Get-Process Unity -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty Path -Unique)
    $unityCandidates = @(
        "C:\Program Files\Unity\Hub\Editor\$validatedUnityVersion\Editor\Unity.exe",
        "D:\Unity Hub\$validatedUnityVersion\Editor\Unity.exe",
        "D:\Unity\Hub\Editor\$validatedUnityVersion\Editor\Unity.exe",
        "E:\Unity Hub\$validatedUnityVersion\Editor\Unity.exe",
        "E:\Unity\Hub\Editor\$validatedUnityVersion\Editor\Unity.exe"
    ) + $runningEditors
    $UnityEditorPath = $unityCandidates |
        Where-Object { $_ -and (Test-Path -LiteralPath $_) } |
        Select-Object -First 1
}

if (-not $UnityEditorPath -or -not (Test-Path -LiteralPath $UnityEditorPath)) {
    throw "Unity $validatedUnityVersion editor not found. Pass -UnityEditorPath explicitly."
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$packageRoot = Join-Path $repositoryRoot "UnityWeb3FpsGameFoundation"
$auditOutput = Join-Path $repositoryRoot "work\unity-static-audit"
$unityData = Join-Path (Split-Path -Parent $UnityEditorPath) "Data"
$monoRuntime = Join-Path $unityData "MonoBleedingEdge\bin\mono.exe"
$csharpCompiler = Join-Path $unityData "MonoBleedingEdge\lib\mono\msbuild\Current\bin\Roslyn\csc.exe"

New-Item -ItemType Directory -Force -Path $auditOutput | Out-Null

function Get-FrameworkReferences {
    $locations = @(
        (Join-Path $unityData "NetStandard\ref\2.1.0"),
        (Join-Path $unityData "NetStandard\compat\2.1.0\shims\netfx"),
        (Join-Path $unityData "NetStandard\compat\2.1.0\shims\netstandard")
    )
    return @($locations | ForEach-Object { Get-ChildItem -File -LiteralPath $_ -Filter "*.dll" })
}

function Invoke-UnityCompile {
    param(
        [string]$Name,
        [System.IO.FileInfo[]]$Sources,
        [System.IO.FileInfo[]]$References
    )

    $assemblyPath = Join-Path $auditOutput ($Name + ".dll")
    $responsePath = Join-Path $auditOutput ($Name + ".rsp")
    $responseLines = @(
        "/nologo",
        "/nostdlib+",
        "/target:library",
        "/langversion:latest",
        "/warn:4",
        ('/out:"' + $assemblyPath + '"')
    )
    $responseLines += @($References | Sort-Object FullName -Unique | ForEach-Object { '/reference:"' + $_.FullName + '"' })
    $responseLines += @($Sources | Sort-Object FullName | ForEach-Object { '"' + $_.FullName + '"' })
    Set-Content -LiteralPath $responsePath -Value $responseLines -Encoding UTF8

    & $monoRuntime $csharpCompiler ("@" + $responsePath) 2>&1 | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) { throw "$Name compilation failed with exit code $LASTEXITCODE" }
    return Get-Item -LiteralPath $assemblyPath
}

$frameworkReferences = @(Get-FrameworkReferences)
$unityEngineReferences = @(Get-ChildItem -File -LiteralPath (Join-Path $unityData "Managed\UnityEngine") -Filter "*.dll")
$runtimeSources = @(Get-ChildItem -Recurse -File -LiteralPath (Join-Path $packageRoot "Runtime") -Filter "*.cs")
$runtimeAssembly = Invoke-UnityCompile -Name "Web3Fps.GameFoundation" -Sources $runtimeSources -References ($frameworkReferences + $unityEngineReferences)

$unityEditorReferences = @(Get-ChildItem -Recurse -File -LiteralPath (Join-Path $unityData "Managed") -Filter "*.dll")
$editorSources = @(Get-ChildItem -Recurse -File -LiteralPath (Join-Path $packageRoot "Editor") -Filter "*.cs")
$editorAssembly = Invoke-UnityCompile -Name "Web3Fps.GameFoundation.Editor" -Sources $editorSources -References ($frameworkReferences + $unityEditorReferences + $runtimeAssembly)

$nunitAssembly = Get-Item -LiteralPath (Join-Path $unityData "Resources\PackageManager\BuiltInPackages\com.unity.ext.nunit\net40\unity-custom\nunit.framework.dll")
$testSources = @(Get-ChildItem -Recurse -File -LiteralPath (Join-Path $packageRoot "Tests\EditMode") -Filter "*.cs")
$testAssembly = Invoke-UnityCompile -Name "Web3Fps.GameFoundation.Tests" -Sources $testSources -References ($frameworkReferences + $unityEngineReferences + $runtimeAssembly + $nunitAssembly)

$runnerSource = Join-Path $PSScriptRoot "UnityStaticTestRunner.cs"
$runnerAssembly = Join-Path $auditOutput "UnityStaticTestRunner.exe"
& $monoRuntime $csharpCompiler "/nologo" "/target:exe" ("/out:" + $runnerAssembly) $runnerSource 2>&1 | ForEach-Object { Write-Host $_ }
if ($LASTEXITCODE -ne 0) { throw "Static test runner compilation failed with exit code $LASTEXITCODE" }

$previousMonoPath = $env:MONO_PATH
$env:MONO_PATH = @(
    $auditOutput,
    (Join-Path $unityData "Managed\UnityEngine"),
    (Split-Path -Parent $nunitAssembly.FullName),
    (Join-Path $unityData "MonoBleedingEdge\lib\mono\unityjit-win32\Facades")
) -join ";"
try
{
    & $monoRuntime $runnerAssembly $testAssembly.FullName
    if ($LASTEXITCODE -ne 0) { throw "Static EditMode test execution failed with exit code $LASTEXITCODE" }
}
finally
{
    $env:MONO_PATH = $previousMonoPath
}

Write-Output "Unity static audit compiled successfully:"
$runtimeAssembly, $editorAssembly, $testAssembly | Select-Object FullName, Length, LastWriteTime
