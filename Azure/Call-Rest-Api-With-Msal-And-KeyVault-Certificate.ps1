<#
.DESCRIPTION
Sample script that calls a REST API using Azure Active Directory (AAD) authentication using the MSAL library and a client certificate.

.PARAMETER VaultName
The key vault name containing the client certificate to authenticate with.

.PARAMETER CertificateName
The name of the certificate in the key vault.

.PARAMETER CertificateVersion
Optional certificate version to use. If omitted, the latest is used.

.PARAMETER ClientId
The client id of the Azure Enterprise Application to authenticate to.

.PARAMETER Scopes
Optional string array of scopes to pass to MSAL. If omitted, a default scope of {uri}/.default is used.

.PARAMETER Uri
The fully-qualified API URI to call.

.PARAMETER Method
The HTTP method to use when calling the API.

.PARAMETER Body
Optional body to pass to the API.

.PARAMETER Headers
Optional additional HTTP headers to pass to the API.
#>
param(
  [Parameter(Mandatory)]
  [string] $VaultName,

  [Parameter(Mandatory)]
  [string] $CertificateName,

  [string] $CertificateVersion,

  [string[]] $Scopes,

  [Parameter(Mandatory)]
  [string] $ClientId,

  [Parameter(Mandatory)]
  [string] $Uri,

  [Parameter(Mandatory)]
  [ValidateSet("Delete", "Get", "Patch", "Post", "Put")]
  [string] $Method,

  [string] $Body,

  [Hashtable] $Headers
)

try {
  Get-InstalledModule -Name "MSAL.PS" -MinimumVersion 4.37.0.0 -ErrorAction Stop | Out-Null
}
catch {
  Write-Warning 'The MSAL.PS module is not installed. Run Install-Module MSAL.PS and run this script again.'
  throw 'Missing dependency'
}

function Get-ClientCertificate {
  #retrieve the client certificate from Key Vault
  if ($CertificateVersion) {
    $secret = Get-AzKeyVaultSecret -VaultName $VaultName -Name $CertificateName -Version $CertificateVersion -AsPlainText
  }
  else {
    $secret = Get-AzKeyVaultSecret -VaultName $VaultName -Name $CertificateName -AsPlainText
  }

  if (!$secret) {
    throw "Failed to retrieve certificate $CertificateName from Key Vault $VaultName"
  }

  $bytes = [Convert]::FromBase64String($secret)
  return [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($bytes)
}

Import-Module 'MSAL.PS'

# login to azure if not already authenticated
$context = Get-AzContext
if (!$context) {
  Connect-AzAccount
  $context = Get-AzContext
  if (!$context) {
    throw "Login failed"
  }
}

$tenantId = $context.Subscription.TenantId

$clientCertificate = Get-ClientCertificate

$clientApplication = Get-MsalClientApplication -ClientId $ClientId -ClientCertificate $clientCertificate -TenantId $tenantId

#use the client certificate to get the access token for the client id
if (!$Scopes) {
  [string] $defaultScope = $null
  if ($Uri.EndsWith('/')) {
    $defaultScope = $Uri + '/.default'
  }
  else {
    $defaultScope = $Uri + '.default'
  }
  $Scopes = @($defaultScope)
}

$accessToken = $clientApplication | Get-MsalToken -Scopes $Scopes

if (!$Headers) {
  $Headers = @{}
}

$Headers.Authentication = "Bearer $($accessToken.AccessToken)"

# use the access token to call a REST API
$result = (`
  Invoke-RestMethod `
    -Method $Method `
    -Uri $Uri `
    -Headers $Headers `
    -Body $Body `
).value

Write-Host $result
