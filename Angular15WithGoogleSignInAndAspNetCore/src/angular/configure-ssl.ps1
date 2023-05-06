using namespace System.IO
# Script that generates the files necessary to `ng serve` using SSL.
# You also need to:
#  - update launch.json to change the port
#  - add the hostname to the Google Console for the OAuth2 credential's JavaScript Authorized Origins
#    e.g., if you use https://myapp.mydomain.com:4201, add that.
#  - Configure DNS or add a HOSTS entry (c:\windows\system32\drivers\etc\hosts on a Windows machine) so
#    that myapp.mydomain.com resolves to 127.0.0.1
#  - Add the issuer cert (saved as ssl/rootCA.crt) to:
#     Windows: the Trusted Root Certificate Authorities certificate store
#     Ubuntu:  see https://ubuntu.com/server/docs/security-trust-store
#     Mac:     see https://apple.stackexchange.com/questions/80623/import-certificates-into-the-system-keychain-via-the-command-line

param(
  [switch] $Force,
  [switch] $EnableSslInAngular,
  [string[]] $DnsNames = @('localhost'),
  [string[]] $IpAddresses = @('127.0.0.1'),
  [string] $CommonName = 'localhost',
  [string] $IssuerCommonName = 'ca.local',
  [string] $Org = 'My organization',
  [string] $OrgUnit = 'DevOps',
  [string] $Country = 'US',
  [string] $StateOrProvince = 'Washington',
  [string] $Locality = 'Redmond',
  [int] $SslPort = 4201
)


if ([Environment]::OSVersion.Platform -eq 'Win32NT') {
  $openSsl = Join-Path $env:ProgramFiles 'Git/usr/bin/openssl.exe'
}
if (!$openSsl -or !(Test-Path $openSsl)) {
  $openSslCommand = Get-Command openssl -ErrorAction SilentlyContinue
  if ($openSslCommand) {
    $openSsl = $openSslCommand.Source
  }
  else {
    throw 'openssl is missing. Install it and make sure it is in your PATH'
  }
}

class IssuerConfig {
  [string] $CommonName
  [string] $Org
  [string] $OrgUnit
  [string] $Country
  [string] $State
  [string] $Locality

  IssuerConfig($commonName, $org, $orgUnit, $country, $state, $locality) {
    $this.CommonName = $commonName
    $this.Org = $org
    $this.OrgUnit = $orgUnit
    $this.Country = $country
    $this.State = $state
    $this.Locality = $locality
  }
}
class CertificateConfig : IssuerConfig {
  [string[]] $DnsNames
  [string[]] $IpAddresses = @('127.0.0.1')

  CertificateConfig($commonName, $org, $orgUnit, $country, $state, $locality, $dnsNames, $ipAddresses) : base($commonName, $org, $orgUnit, $country, $state, $locality) {
    $this.DnsNames = $dnsNames
    $this.IpAddresses = $ipAddresses
  }

  [string] GetAltNamesConfigSection() {  
    $altNames = @()
    $num = 1
    foreach ($name in $this.DnsNames) {
      $altNames += "DNS.$num = $name"
      $num++
    }
  
    $num = 1
    foreach ($ip in $this.IpAddresses) {
      $altNames += "IP.$num = $ip"
      $num++
    }
  
    return [string]::Join([Environment]::NewLine, $altNames)
  }
}

class CertificatePaths {
  [string] $BaseName
  [string] $PrivateKey
  [string] $Certificate
  [string] $Serial
  [string] $CertRequest

  CertificatePaths($baseName) {
    $this.BaseName = $baseName
    $sslPath = Join-Path $PSScriptRoot 'ssl'
    $this.PrivateKey = Join-Path $sslPath ($baseName + '.key')
    $this.Certificate = Join-Path $sslPath ($baseName + '.crt')
    $this.Serial = Join-Path $sslPath ($baseName + '.srl')
    $this.CertRequest = Join-Path $sslPath ($baseName + '.csr')
  }

  [boolean] Exists() {
    return (Test-Path $this.PrivateKey) -and (Test-Path $this.Certificate)
  }
}

function CreateIssuerCertificate {
  param([string] $BaseName, [IssuerConfig] $Config)
  $certPaths = [CertificatePaths]::new($BaseName)

  # generate the issuer certificate and private key
  if ($Force -or !$certPaths.Exists()) {
    $subject = [string]::Format('/CN={0}/O={1}/OU={2}/C={3}/ST={4}/L={5}',
      $Config.CommonName,
      $Config.Org,
      $Config.OrgUnit,
      $Config.Country,
      $Config.State,
      $Config.Locality)
    & $openSsl req -x509 -sha256 -days 365 -nodes -newkey rsa:2048 -subj "$subject" -keyout $certPaths.PrivateKey -out $certPaths.Certificate
    Write-Host "*** You should install $($certPaths.Certificate) into your trusted root certificate authorities for the local machine."
  }
  else {
    Write-Host 'Skipping issuer certificate generation: already exists! To overwrite, use -Force'
  }

  return $certPaths
}

function UpdateAngularJson {
  param(
    [CertificatePaths] $certPaths
  )
  $angularJson = Get-Content $PSScriptRoot/angular.json | ConvertFrom-Json -Depth 100

  $keyPath = $certPaths.PrivateKey.Replace($PSScriptRoot, '.').Replace('\', '/')
  $crtPath = $certPaths.Certificate.Replace($PSScriptRoot, '.').Replace('\', '/')
  
  $firstProjectName = ($angularJson.projects.PSObject.Members | Where-Object MemberType -Like 'noteproperty' | Select-Object -First 1).Name
  $firstProject = $angularJson.projects.$($firstProjectName)
  if (!$firstProject) {
    throw 'no firstproject'
  }
  if (!$firstProject.architect) {
    throw 'no architect'
  }
  $serve = $firstProject.architect.serve

  if (!$serve) {
    throw 'no serve'
  }

  $options = [ordered]@{
    host    = 'localhost'
    sslKey  = $keyPath
    sslCert = $crtPath
    ssl     = $true
    port    = $SslPort
  }

  if ($serve.options) {
    $serve.options = $options
  }
  else {
    $serve | Add-Member -MemberType NoteProperty -Name options -Value $options
  }

  $json = $angularJson | ConvertTo-Json -Depth 100
  Set-Content $PSScriptRoot/angular.json $json
}

$issuerConfig = [IssuerConfig]::new(
  $IssuerCommonName,
  $Org,
  $OrgUnit,
  $Country,
  $StateOrProvince,
  $Locality)

$serverConfig = [CertificateConfig]::new(
  $CommonName,
  $Org,
  $Org,
  $Country,
  $StateOrProvince,
  $Locality,
  $DnsNames,
  $IpAddresses)

function CreateServerCertificate {
  param(
    [string] $BaseName,
    [CertificatePaths] $IssuerResult,
    [CertificateConfig] $Config
  )
  
  $certPaths = [CertificatePaths]::new($BaseName)
  # generate the private key for the SSL certificate
  if (!$Force -and $certPaths.Exists()) {
    Write-Host 'Skipping server certificate generation: already exists! To overwrite, use -Force'
  }
  else {
    & $openSsl genrsa -out $certPaths.PrivateKey 2048

    # create an OpenSSL configuration file for the CSR 
    $csrConfContent = @'
[ req ]
default_bits = 2048
prompt = no
default_md = sha256
req_extensions = req_ext
distinguished_name = dn

[ dn ]
C = {0}
ST = {1}
L = {2}
O = {3}
OU = {4}
CN = {5}

[ req_ext ]
subjectAltName = @alt_names

[ alt_names ]
{6}
'@

    $csrConfContent = [string]::Format(
      $csrConfContent,
      $Config.Country,
      $Config.State,
      $Config.Locality,
      $Config.Org,
      $Config.OrgUnit,
      $Config.CommonName,
      $Config.GetAltNamesConfigSection())

    $tempFile = [Path]::GetTempFileName()
    Set-Content -Path $tempFile -Value $csrConfContent -Force

    Write-Host 'Using the SSL private key and CSR config file to create the cert request'
    & $openSsl req -new -key $certPaths.PrivateKey -out $certPaths.CertRequest -config $tempFile

    Remove-Item $tempFile

    # create an OpenSSL config file for the x509 signing operation
    $extConfigContent = @'
authorityKeyIdentifier=keyid,issuer
basicConstraints=CA:FALSE
keyUsage = digitalSignature, nonRepudiation, keyEncipherment, dataEncipherment
subjectAltName = @alt_names

[alt_names]

'@
    $extConfigContent += $Config.GetAltNamesConfigSection()
    $tempFile = [Path]::GetTempFileName()
    Set-Content -Path $tempFile -Force -Value $extConfigContent
  
    # using the CSR and issuer private key, create the private and public keys for the server
    & $openSsl x509 -req -in $certPaths.CertRequest -CA $IssuerResult.Certificate -CAkey $IssuerResult.PrivateKey -CAcreateserial -out $certPaths.Certificate -days 365 -sha256 -extfile $tempFile
    Remove-Item $tempFile
  }

  return $certPaths
}

$issuerResult = CreateIssuerCertificate -BaseName rootCA -Config $issuerConfig
Write-Host "Issuer certificate: $($issuerResult.Certificate)"
Write-Host "Issuer private key: $($issuerResult.PrivateKey)"

$serverResult = CreateServerCertificate -BaseName server -IssuerResult $issuerResult -Config $serverConfig
Write-Host "Server certificate: $($serverResult.Certificate)"
Write-Host "Server private key: $($serverResult.PrivateKey)"

if ($EnableSslInAngular) {
  Write-Host 'Updating angular.json'
  UpdateAngularJson -CertPaths $serverResult
}