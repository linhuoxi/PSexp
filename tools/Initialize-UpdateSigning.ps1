param([Parameter(Mandatory=$true)][string]$Repository)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'UpdateSigning.ps1')
$Repository = (Resolve-Path -LiteralPath $Repository).Path
$keyFile = Get-PSexpSigningKeyPath
$publicPath = Join-Path $Repository 'PSdodo\Resources\Updates\update-public-key.json'
if ((Test-Path -LiteralPath $keyFile) -or (Test-Path -LiteralPath $publicPath)) {
    throw 'A signing identity already exists. Refusing to replace the key or public trust anchor.'
}
$keyDirectory = Split-Path -Parent $keyFile
$repoPrefix = $Repository.TrimEnd('\') + '\'
if ($keyFile.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Private signing keys must be outside the source repository.'
}
Add-Type -AssemblyName System.Security
$rsa = [Security.Cryptography.RSACryptoServiceProvider]::new(3072)
$rsa.PersistKeyInCsp = $false
$plain = $null
try {
    [IO.Directory]::CreateDirectory($keyDirectory) | Out-Null
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent().User
    $acl = [Security.AccessControl.DirectorySecurity]::new()
    $acl.SetOwner($identity)
    $acl.SetAccessRuleProtection($true, $false)
    $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($identity, 'FullControl',
        'ContainerInherit, ObjectInherit', 'None', 'Allow'))
    Set-Acl -LiteralPath $keyDirectory -AclObject $acl
    $plain = [Text.Encoding]::UTF8.GetBytes($rsa.ToXmlString($true))
    $protected = [Security.Cryptography.ProtectedData]::Protect($plain, $null,
        [Security.Cryptography.DataProtectionScope]::CurrentUser)
    $output = [IO.File]::Open($keyFile, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $output.Write($protected, 0, $protected.Length) } finally { $output.Dispose() }
    $publicXml = $rsa.ToXmlString($false)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $keyId = 'psexp-' + ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($publicXml))).Replace('-', '').ToLowerInvariant().Substring(0, 16)) }
    finally { $sha.Dispose() }
    $trusted = [ordered]@{ key_id=$keyId; algorithm='RSA-SHA256-PKCS1'; public_key_xml=$publicXml }
    [IO.Directory]::CreateDirectory((Split-Path -Parent $publicPath)) | Out-Null
    [IO.File]::WriteAllText($publicPath, ($trusted | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
    Write-Host ('Created update signing identity: ' + $keyId)
    Write-Host 'Only the public key was written to the source tree. Private key is DPAPI-protected for this Windows user.'
} finally {
    if ($null -ne $plain) { [Array]::Clear($plain, 0, $plain.Length) }
    $rsa.Dispose()
}
