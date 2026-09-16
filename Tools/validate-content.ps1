param(
    [string]$ContentRoot = "$PSScriptRoot\..\UnityProject\Assets\StreamingAssets\DigestiveSimulator"
)

$ErrorActionPreference = 'Stop'
$resolvedRoot = (Resolve-Path -LiteralPath $ContentRoot).Path

function Read-Json([string]$Path) {
    Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
}

$jsonFiles = Get-ChildItem -LiteralPath $resolvedRoot -Recurse -Filter *.json
foreach ($file in $jsonFiles) { $null = Read-Json $file.FullName }

$speciesCatalog = Read-Json "$resolvedRoot\catalogs\species_catalog.json"
$foodCatalog = Read-Json "$resolvedRoot\catalogs\food_catalog.json"
$referenceLibrary = Read-Json "$resolvedRoot\references\references.json"
if ($speciesCatalog.schemaVersion -ne '1.0' -or $foodCatalog.schemaVersion -ne '1.0') { throw 'Unsupported catalog version.' }

$referenceIds = @($referenceLibrary.references | ForEach-Object { $_.id })
if ($referenceIds.Count -ne @($referenceIds | Sort-Object -Unique).Count) { throw 'Duplicate reference ID.' }
foreach ($reference in $referenceLibrary.references) {
    if ([string]::IsNullOrWhiteSpace($reference.id)) { throw 'Empty reference ID.' }
    if ([string]::IsNullOrWhiteSpace($reference.authors) -or [string]::IsNullOrWhiteSpace($reference.title)) {
        throw "Incomplete reference: $($reference.id)"
    }
    if ([string]::IsNullOrWhiteSpace($reference.url)) { throw "Reference has no URL: $($reference.id)" }
}

function Assert-ReferencesExist([object[]]$Ids, [string]$Owner) {
    foreach ($referenceId in @($Ids)) {
        if ([string]::IsNullOrWhiteSpace($referenceId)) { throw "Empty reference ID in $Owner." }
        if ($referenceId -notin $referenceIds) { throw "Unknown reference '$referenceId' in $Owner." }
    }
}

foreach ($entry in $foodCatalog.foods) {
    $foodPath = Join-Path $resolvedRoot ($entry.definitionPath -replace '/', '\')
    if (-not (Test-Path -LiteralPath $foodPath)) { throw "Missing food definition: $($entry.id)" }
    $food = Read-Json $foodPath
    if ($food.id -ne $entry.id) { throw "Food ID mismatch: $($entry.id)" }
    Assert-ReferencesExist $food.compositionReferenceIds "food $($food.id)"
}

foreach ($entry in $speciesCatalog.species) {
    $speciesPath = Join-Path $resolvedRoot ($entry.definitionPath -replace '/', '\')
    if (-not (Test-Path -LiteralPath $speciesPath)) { throw "Missing species definition: $($entry.id)" }
    $species = Read-Json $speciesPath
    if ($species.id -ne $entry.id) { throw "Species ID mismatch: $($entry.id)" }
    $speciesRoot = Split-Path -Parent $speciesPath
    foreach ($modelFile in @($species.model.exteriorFile, $species.model.digestiveSystemFile)) {
        if ([string]::IsNullOrWhiteSpace($modelFile)) { throw "Missing model path for $($species.id)." }
        if ([IO.Path]::GetExtension($modelFile) -ne '.glb') { throw "Model must use GLB format: $modelFile" }
        $modelPath = Join-Path $speciesRoot ($modelFile -replace '/', '\')
        if (-not (Test-Path -LiteralPath $modelPath -PathType Leaf)) { throw "Missing model file: $modelFile" }
        if ((Get-Item -LiteralPath $modelPath).Length -le 0) { throw "Empty model file: $modelFile" }
    }
    $graph = Read-Json (Join-Path $speciesRoot $species.digestiveSystem.graphFile)
    $nodeIds = @($graph.nodes.id)
    if ($graph.entryNodeId -notin $nodeIds) { throw "Missing graph entry node for $($species.id)." }
    foreach ($organId in $species.digestiveSystem.organs) {
        $organPath = Join-Path $speciesRoot "organs\$organId\organ.json"
        if (-not (Test-Path -LiteralPath $organPath)) { throw "Missing organ definition: $organId" }
        $organ = Read-Json $organPath
        if ($organ.id -ne $organId -or $organ.speciesId -ne $species.id) { throw "Organ identity mismatch: $organId" }
        Assert-ReferencesExist $organ.referenceIds "organ $organId"
        foreach ($signal in @($organ.regulation.signals)) {
            if ([string]::IsNullOrWhiteSpace($signal.name) -or [string]::IsNullOrWhiteSpace($signal.type) -or [string]::IsNullOrWhiteSpace($signal.origin) -or [string]::IsNullOrWhiteSpace($signal.response)) {
                throw "Incomplete regulatory signal in organ $organId"
            }
            if ($signal.type -notin @('hormone', 'reflex', 'neural')) { throw "Unsupported regulatory signal type '$($signal.type)' in organ $organId" }
            if ($signal.targetOrganId -notin $species.digestiveSystem.organs) { throw "Regulatory signal references unknown target organ: $organId+$($signal.targetOrganId)" }
            if (-not [string]::IsNullOrWhiteSpace($signal.referenceId)) {
                Assert-ReferencesExist @($signal.referenceId) "signal '$($signal.name)' in organ $organId"
            }
            else { throw "Missing reference in regulatory signal '$($signal.name)' in organ $organId" }
        }
    }
    Assert-ReferencesExist $species.referenceIds "species $($species.id)"
    $routeIds = @($species.absorptionRoutes | ForEach-Object { $_.id })
    if ($routeIds.Count -ne @($routeIds | Sort-Object -Unique).Count) { throw "Duplicate absorption route ID for $($species.id)." }
    foreach ($route in @($species.absorptionRoutes)) {
        if ($route.targetOrganId -notin $species.digestiveSystem.organs) { throw "Absorption route references unknown target organ: $($route.id)" }
        foreach ($siteOrganId in @($route.siteOrganIds)) {
            if ($siteOrganId -notin $species.digestiveSystem.organs) { throw "Absorption route references unknown site organ: $($route.id)+$siteOrganId" }
        }
        Assert-ReferencesExist $route.referenceIds "absorption route $($route.id)"
    }
    foreach ($node in $graph.nodes) {
        if ($node.organId -notin $species.digestiveSystem.organs) { throw "Graph references unknown organ: $($node.organId)" }
        $validProcesses = @('ingestion', 'initial_processing', 'mechanical_assistance', 'secretion', 'transit', 'storage', 'chemical_digestion', 'mechanical_digestion', 'digestion_absorption', 'absorption', 'landmark', 'fermentation', 'water_recovery', 'elimination')
        if ([string]::IsNullOrWhiteSpace($node.process) -or $node.process -notin $validProcesses) { throw "Invalid digestive process in graph node: $($node.id)" }
        foreach ($next in $node.nextNodeIds) { if ($next -notin $nodeIds) { throw "Graph references unknown node: $next" } }
    }
    foreach ($foodId in $foodCatalog.foods.id) {
        $profilePath = Join-Path $resolvedRoot "profiles\$($species.id)\$foodId.json"
        if (-not (Test-Path -LiteralPath $profilePath)) { throw "Missing profile: $($species.id)+$foodId" }
        $profile = Read-Json $profilePath
        if ($profile.speciesId -ne $species.id -or $profile.foodId -ne $foodId) { throw "Profile identity mismatch: $($species.id)+$foodId" }
        Assert-ReferencesExist $profile.referenceIds "profile $($species.id)+$foodId"
        $percentageFields = @('dryMatter', 'protein', 'starch', 'fat')
        foreach ($field in $percentageFields) {
            $value = $profile.digestibility.$field
            if ($null -ne $value -and ($value -lt 0 -or $value -gt 100)) {
                throw "Digestibility $field must be between 0 and 100 in profile $($species.id)+$foodId"
            }
        }
        $hasPercentage = @($percentageFields | Where-Object { $null -ne $profile.digestibility.$_ }).Count -gt 0
        if ($hasPercentage -and [string]::IsNullOrWhiteSpace($profile.digestibility.units)) {
            throw "Missing percentage units in profile $($species.id)+$foodId"
        }
        if ($null -ne $profile.digestibility.energy -and [string]::IsNullOrWhiteSpace($profile.digestibility.energyUnits)) {
            throw "Missing energy units in profile $($species.id)+$foodId"
        }
        $stageOverrides = @($profile.simulation.stageOverrides)
        if ($stageOverrides.Count -lt 3) { throw "Profile must contain at least three food-specific stage messages: $($species.id)+$foodId" }
        $overrideOrgans = @($stageOverrides | ForEach-Object { $_.organId })
        if ($overrideOrgans.Count -ne @($overrideOrgans | Sort-Object -Unique).Count) { throw "Duplicate stage override organ in profile $($species.id)+$foodId" }
        foreach ($stageOverride in $stageOverrides) {
            if ($stageOverride.organId -notin $species.digestiveSystem.organs) { throw "Stage override references unknown organ: $($species.id)+$foodId+$($stageOverride.organId)" }
            if ($stageOverride.durationSeconds -le 0) { throw "Stage override duration must be positive: $($species.id)+$foodId+$($stageOverride.organId)" }
            if ([string]::IsNullOrWhiteSpace($stageOverride.message)) { throw "Missing food-specific stage message: $($species.id)+$foodId+$($stageOverride.organId)" }
        }
    }
}

Write-Output "Content validation passed: $($jsonFiles.Count) JSON files, $($speciesCatalog.species.Count) species, $($foodCatalog.foods.Count) foods, $($referenceIds.Count) references."
