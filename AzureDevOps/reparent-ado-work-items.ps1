###############################
# Reparent work item
###############################

# Prerequisites:
# az devops login (then paste in PAT when prompted)

[CmdletBinding()]
param (
  [string]$org = 'Microsoft', # Azure devops org without the URL, eg: "MyAzureDevOpsOrg"
  [string]$project = 'OSGS', # Team project name that contains the work items, eg: "TailWindTraders"
  [string]$tag,
  [int[]]$ids,
  [Parameter(Mandatory)]
  [int]$newParentId # the ID of the new work item, eg: "223"
)

if ($tag -and $ids) {
  Write-Error 'You can only specify one of -tag or -ids'
  exit 1
}

az devops configure --defaults organization="https://dev.azure.com/$org" project="$project"

$wiql = 'select [ID], [Title] from workitems'

if ($tag) {
  $wiql += " where [System.Tags] contains '$tag'"
}
else {
  for ($i = 0; $i -lt $ids.Count; $i++) {
    if ($i -eq 0) {
      $wiql += ' where'
    }
    else {
      $wiql += ' or'
    }
    $wiql += " [Id] = '$($ids[$i])'"
  }
}

$query = az boards query --wiql $wiql | ConvertFrom-Json

ForEach ($workitem in $query) {
  $links = az boards work-item relation show --id $workitem.id | ConvertFrom-Json
  ForEach ($link in $links.relations) {
    if ($link.rel -eq 'Parent') {
      $parentId = $link.url.Split('/')[-1]
      if ($parentId -ne $newParentId) {
        Write-Host 'Unparenting' $links.id "from $parentId"
        az boards work-item relation remove --id $links.id --relation-type 'parent' --target-id $parentId --yes  | out-null

        Write-Host 'Parenting' $links.id "to $newParentId"
        az boards work-item relation add --id $links.id --relation-type 'parent' --target-id $newParentId | out-null
      }
      else {
        Write-Host 'Work item' $links.id "is already parented to $parentId"
      }
    }
  }
}