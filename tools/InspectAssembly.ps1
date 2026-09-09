param(
    [Parameter(Mandatory = $true)]
    [string]$AssemblyPath,

    [Parameter(Mandatory = $true)]
    [string[]]$TypeName,

    [string[]]$MemberName = @()
)

$ErrorActionPreference = "Stop"

$assembly = [Reflection.Assembly]::LoadFrom($AssemblyPath)

foreach ($name in $TypeName) {
    $name = $name -split "," | Select-Object -First 1
    $type = $assembly.GetType($name, $false)
    if ($null -eq $type) {
        Write-Output "TYPE NOT FOUND: $name"
        continue
    }

    Write-Output ("=" * 80)
    Write-Output ("TYPE: " + $type.FullName)
    Write-Output ("BASE: " + $type.BaseType)

    $flags = [Reflection.BindingFlags]::Public -bor
        [Reflection.BindingFlags]::NonPublic -bor
        [Reflection.BindingFlags]::Instance -bor
        [Reflection.BindingFlags]::Static

    $members = @()
    $members += $type.GetFields($flags)
    $members += $type.GetProperties($flags)
    $members += $type.GetMethods($flags) |
        Where-Object { -not $_.IsSpecialName }
    $members += $type.GetConstructors($flags)

    foreach ($member in $members | Sort-Object Name) {
        if ($MemberName.Count -gt 0) {
            $matches = $false
            foreach ($pattern in ($MemberName -join "," -split ",")) {
                if ($member.Name -like $pattern) {
                    $matches = $true
                    break
                }
            }
            if (-not $matches) {
                continue
            }
        }

        if ($member -is [Reflection.FieldInfo]) {
            Write-Output ("  FIELD  " + $member.FieldType + " " + $member.Name)
            continue
        }

        if ($member -is [Reflection.PropertyInfo]) {
            Write-Output ("  PROP   " + $member.PropertyType + " " + $member.Name)
            continue
        }

        if ($member -is [Reflection.MethodBase]) {
            $returnType = ""
            if ($member -is [Reflection.MethodInfo]) {
                $returnType = $member.ReturnType.ToString() + " "
            }

            $parameters = @()
            foreach ($parameter in $member.GetParameters()) {
                $parameters += ($parameter.ParameterType.ToString() + " " + $parameter.Name)
            }

            Write-Output ("  METHOD " + $returnType + $member.Name + "(" + ($parameters -join ", ") + ")")
        }
    }
}
