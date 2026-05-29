# Fail fast
$ErrorActionPreference = "Stop"

# Rutas hardcodeadas a los proyectos a empaquetar (relativas a la raíz)
$projectsToPack = @(
    ".\src\BetterEnums\YACL.BetterEnums.Abstractions\YACL.BetterEnums.Abstractions.csproj" #BetterEnums
)

# Configuración común
$configuration = "Release"
$packageOutput = ".\nugets"

# Asegurar que el directorio de salida exista
if (-not (Test-Path $packageOutput)) {
    New-Item -ItemType Directory -Force -Path $packageOutput | Out-Null
}

# Limpia el directorio destino
Remove-Item -Path $packageOutput -Recurse -Force

foreach ($project in $projectsToPack) {

    Write-Host "Building $project..."

    dotnet build $project -c $configuration

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed for $project"
    }

    Write-Host "Packing $project..."

    dotnet pack $project `
        -c $configuration `
        --no-build `
        -o $packageOutput `
        /p:PackAnalyzers=true `
        /p:AnalyzerPackagePath="analyzers/dotnet/cs"

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed for $project"
    }
}

Write-Host "All packages packed successfully."
