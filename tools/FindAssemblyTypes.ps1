param(
    [Parameter(Mandatory = $true)]
    [string]$AssemblyPath,

    [Parameter(Mandatory = $true)]
    [string[]]$Pattern,

    [switch]$Members
)

$ErrorActionPreference = "Stop"

$assembly = [Reflection.Assembly]::LoadFrom($AssemblyPath)

try {
    $types = $assembly.GetTypes()
}
catch [Reflection.ReflectionTypeLoadException] {
    $types = $_.Exception.Types | Where-Object { $null -ne $_ }
}

Write-Host ("Loaded " + @($types).Count + " types from " + $assembly.GetName().Name)

$flags = [Reflection.BindingFlags]::Public -bor
    [Reflection.BindingFlags]::NonPublic -bor
    [Reflection.BindingFlags]::Instance -bor
    [Reflection.BindingFlags]::Static

foreach ($type in $types | Sort-Object FullName) {
    $name = $type.FullName
    if ($null -eq $name) {
        continue
    }

    $matched = $false
    foreach ($p in $Pattern) {
        if ($name -like $p) {
            $matched = $true
            break
        }
    }

    if (-not $matched) {
        continue
    }

    Write-Output ("TYPE " + $name + "  (base " + $type.BaseType + ")")

    if ($Members) {
        $rectFields = $type.GetFields($flags) |
            Where-Object { $_.FieldType.FullName -match 'RectTransform|Transform|GameObject' }

        foreach ($field in $rectFields) {
            Write-Output ("    FIELD " + $field.FieldType.Name + " " + $field.Name)
        }
    }
}
