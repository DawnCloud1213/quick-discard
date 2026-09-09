param(
    [Parameter(Mandatory = $true)]
    [string]$AssemblyPath,

    [Parameter(Mandatory = $true)]
    [string]$TypeName,

    [Parameter(Mandatory = $true)]
    [string]$MethodName
)

$ErrorActionPreference = "Stop"

$assembly = [Reflection.Assembly]::LoadFrom($AssemblyPath)
$type = $assembly.GetType($TypeName, $true)
$flags = [Reflection.BindingFlags]::Public -bor
    [Reflection.BindingFlags]::NonPublic -bor
    [Reflection.BindingFlags]::Instance -bor
    [Reflection.BindingFlags]::Static

$method = $type.GetMethods($flags) |
    Where-Object Name -eq $MethodName |
    Select-Object -First 1

if ($null -eq $method) {
    throw "Method not found: $TypeName.$MethodName"
}

$body = $method.GetMethodBody()
if ($null -eq $body) {
    Write-Output "No IL body."
    exit 0
}

$il = $body.GetILAsByteArray()
$module = $method.Module
$index = 0

function Read-Int32 {
    param([int]$Offset)
    return [BitConverter]::ToInt32($il, $Offset)
}

function Read-Int64 {
    param([int]$Offset)
    return [BitConverter]::ToInt64($il, $Offset)
}

function Read-UInt16 {
    param([int]$Offset)
    return [BitConverter]::ToUInt16($il, $Offset)
}

function Read-SByte {
    param([int]$Offset)
    return [SByte]$il[$Offset]
}

function Read-Single {
    param([int]$Offset)
    return [BitConverter]::ToSingle($il, $Offset)
}

function Read-Double {
    param([int]$Offset)
    return [BitConverter]::ToDouble($il, $Offset)
}

function Resolve-Token {
    param([int]$Token)
    try {
        return $module.ResolveMethod($Token)
    }
    catch {
        try {
            return $module.ResolveField($Token)
        }
        catch {
            try {
                return $module.ResolveType($Token)
            }
            catch {
                try {
                    return '"' + $module.ResolveString($Token) + '"'
                }
                catch {
                    return "token 0x" + $Token.ToString("X8")
                }
            }
        }
    }
}

$singleByteNoOperand = @{
    0x00 = "nop"; 0x01 = "break"; 0x02 = "ldarg.0"; 0x03 = "ldarg.1";
    0x04 = "ldarg.2"; 0x05 = "ldarg.3"; 0x06 = "ldloc.0"; 0x07 = "ldloc.1";
    0x08 = "ldloc.2"; 0x09 = "ldloc.3"; 0x0A = "stloc.0"; 0x0B = "stloc.1";
    0x0C = "stloc.2"; 0x0D = "stloc.3"; 0x14 = "ldnull"; 0x15 = "ldc.i4.m1";
    0x16 = "ldc.i4.0"; 0x17 = "ldc.i4.1"; 0x18 = "ldc.i4.2"; 0x19 = "ldc.i4.3";
    0x1A = "ldc.i4.4"; 0x1B = "ldc.i4.5"; 0x1C = "ldc.i4.6"; 0x1D = "ldc.i4.7";
    0x1E = "ldc.i4.8"; 0x25 = "dup"; 0x26 = "pop"; 0x2A = "ret";
    0x46 = "ldind.i1"; 0x47 = "ldind.u1"; 0x48 = "ldind.i2"; 0x49 = "ldind.u2";
    0x4A = "ldind.i4"; 0x4B = "ldind.u4"; 0x4C = "ldind.i8"; 0x4D = "ldind.i";
    0x4E = "ldind.r4"; 0x4F = "ldind.r8"; 0x50 = "ldind.ref"; 0x51 = "stind.ref";
    0x52 = "stind.i1"; 0x53 = "stind.i2"; 0x54 = "stind.i4"; 0x55 = "stind.i8";
    0x56 = "stind.r4"; 0x57 = "stind.r8"; 0x58 = "add"; 0x59 = "sub";
    0x5A = "mul"; 0x5B = "div"; 0x5C = "div.un"; 0x5D = "rem"; 0x5E = "rem.un";
    0x5F = "and"; 0x60 = "or"; 0x61 = "xor"; 0x62 = "shl"; 0x63 = "shr";
    0x64 = "shr.un"; 0x65 = "neg"; 0x66 = "not"; 0x67 = "conv.i1";
    0x68 = "conv.i2"; 0x69 = "conv.i4"; 0x6A = "conv.i8"; 0x6B = "conv.r4";
    0x6C = "conv.r8"; 0x6D = "conv.u4"; 0x6E = "conv.u8"; 0x82 = "conv.r.un";
}

while ($index -lt $il.Length) {
    $offset = $index
    $opcode = $il[$index]
    $index++
    $name = $singleByteNoOperand[$opcode]

    if ($null -ne $name) {
        Write-Output ("IL_{0:X4}: {1}" -f $offset, $name)
        continue
    }

    switch ($opcode) {
        0x0E { Write-Output ("IL_{0:X4}: ldarg.s {1}" -f $offset, $il[$index]); $index += 1 }
        0x0F { Write-Output ("IL_{0:X4}: ldarga.s {1}" -f $offset, $il[$index]); $index += 1 }
        0x10 { Write-Output ("IL_{0:X4}: starg.s {1}" -f $offset, $il[$index]); $index += 1 }
        0x11 { Write-Output ("IL_{0:X4}: ldloc.s {1}" -f $offset, $il[$index]); $index += 1 }
        0x12 { Write-Output ("IL_{0:X4}: ldloca.s {1}" -f $offset, $il[$index]); $index += 1 }
        0x13 { Write-Output ("IL_{0:X4}: stloc.s {1}" -f $offset, $il[$index]); $index += 1 }
        0x1F { Write-Output ("IL_{0:X4}: ldc.i4.s {1}" -f (Read-SByte $index)); $index += 1 }
        0x20 { Write-Output ("IL_{0:X4}: ldc.i4 {1}" -f $offset, (Read-Int32 $index)); $index += 4 }
        0x21 { Write-Output ("IL_{0:X4}: ldc.i8 {1}" -f $offset, (Read-Int64 $index)); $index += 8 }
        0x22 { Write-Output ("IL_{0:X4}: ldc.r4 {1}" -f $offset, (Read-Single $index)); $index += 4 }
        0x23 { Write-Output ("IL_{0:X4}: ldc.r8 {1}" -f $offset, (Read-Double $index)); $index += 8 }
        0x27 { $target = $index + 1 + (Read-SByte $index); Write-Output ("IL_{0:X4}: br.s IL_{1:X4}" -f $offset, $target); $index += 1 }
        0x28 { $token = Read-Int32 $index; Write-Output ("IL_{0:X4}: call {1}" -f $offset, (Resolve-Token $token)); $index += 4 }
        0x29 { $token = Read-Int32 $index; Write-Output ("IL_{0:X4}: calli {1}" -f $offset, (Resolve-Token $token)); $index += 4 }
        0x2B { $target = $index + 1 + (Read-SByte $index); Write-Output ("IL_{0:X4}: br IL_{1:X4}" -f $offset, $target); $index += 1 }
        0x2C { $target = $index + 1 + (Read-SByte $index); Write-Output ("IL_{0:X4}: brfalse.s IL_{1:X4}" -f $offset, $target); $index += 1 }
        0x2D { $target = $index + 1 + (Read-SByte $index); Write-Output ("IL_{0:X4}: brtrue.s IL_{1:X4}" -f $offset, $target); $index += 1 }
        0x38 { $target = $index + 4 + (Read-Int32 $index); Write-Output ("IL_{0:X4}: br IL_{1:X4}" -f $offset, $target); $index += 4 }
        0x39 { $target = $index + 4 + (Read-Int32 $index); Write-Output ("IL_{0:X4}: brfalse IL_{1:X4}" -f $offset, $target); $index += 4 }
        0x3A { $target = $index + 4 + (Read-Int32 $index); Write-Output ("IL_{0:X4}: brtrue IL_{1:X4}" -f $offset, $target); $index += 4 }
        0x6F { $token = Read-Int32 $index; Write-Output ("IL_{0:X4}: callvirt {1}" -f $offset, (Resolve-Token $token)); $index += 4 }
        0x72 { $token = Read-Int32 $index; Write-Output ("IL_{0:X4}: ldstr {1}" -f $offset, (Resolve-Token $token)); $index += 4 }
        0x73 { $token = Read-Int32 $index; Write-Output ("IL_{0:X4}: newobj {1}" -f $offset, (Resolve-Token $token)); $index += 4 }
        0x74 { $token = Read-Int32 $index; Write-Output ("IL_{0:X4}: castclass {1}" -f $offset, (Resolve-Token $token)); $index += 4 }
        0x75 { $token = Read-Int32 $index; Write-Output ("IL_{0:X4}: isinst {1}" -f $offset, (Resolve-Token $token)); $index += 4 }
        0x7B { $token = Read-Int32 $index; Write-Output ("IL_{0:X4}: ldfld {1}" -f $offset, (Resolve-Token $token)); $index += 4 }
        0x7C { $token = Read-Int32 $index; Write-Output ("IL_{0:X4}: ldflda {1}" -f $offset, (Resolve-Token $token)); $index += 4 }
        0x7D { $token = Read-Int32 $index; Write-Output ("IL_{0:X4}: stfld {1}" -f $offset, (Resolve-Token $token)); $index += 4 }
        0x7E { $token = Read-Int32 $index; Write-Output ("IL_{0:X4}: ldsfld {1}" -f $offset, (Resolve-Token $token)); $index += 4 }
        0x80 { $token = Read-Int32 $index; Write-Output ("IL_{0:X4}: stsfld {1}" -f $offset, (Resolve-Token $token)); $index += 4 }
        0x8C { $token = Read-Int32 $index; Write-Output ("IL_{0:X4}: box {1}" -f $offset, (Resolve-Token $token)); $index += 4 }
        0x8D { $token = Read-Int32 $index; Write-Output ("IL_{0:X4}: newarr {1}" -f $offset, (Resolve-Token $token)); $index += 4 }
        0x8E { Write-Output ("IL_{0:X4}: ldlen" -f $offset) }
        0xA2 { $token = Read-Int32 $index; Write-Output ("IL_{0:X4}: stelem.ref" -f $offset); $index += 4 }
        0xA5 { $token = Read-Int32 $index; Write-Output ("IL_{0:X4}: unbox.any {1}" -f $offset, (Resolve-Token $token)); $index += 4 }
        0xDE { $index += 1; Write-Output ("IL_{0:X4}: leave.s" -f $offset) }
        default {
            Write-Output ("IL_{0:X4}: <opcode 0x{1:X2}>" -f $offset, $opcode)
            break
        }
    }
}
