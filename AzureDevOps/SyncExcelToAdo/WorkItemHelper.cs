namespace SyncExcelToAdo;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using static ConsoleHelper;

internal class WorkItemHelper
{
    private readonly AdoClients ado;
    private readonly AppSettings settings;

    public WorkItemHelper(AdoClients ado, AppSettings settings)
    {
        this.ado = ado;
        this.settings = settings;
    }

    public async Task<IDictionary<int, WorkItem>> GetWorkItems(IEnumerable<int> ids)
    {
        try
        {
            string[] fields = this.settings.Excel.Mappings.WriteToAdo.Select(x => x.AdoField)
                .Concat(this.settings.Excel.Mappings.ReadFromAdo.Select(x => x.AdoField))
                .Distinct()
                .ToArray();

            var result = (await this.ado.WorkItems.GetWorkItemsAsync(ids, fields))
                .ToList();
            List<WorkItem>? resultsWithRelations = await this.ado.WorkItems.GetWorkItemsAsync(ids, expand: WorkItemExpand.Relations);

            Dictionary<int, WorkItem> dict = new();
            if (result is not null)
            {
                _ = new List<Task<WorkItem>>();
                foreach (WorkItem? item in result)
                {
                    if (item.Id.HasValue)
                    {
                        WorkItem? withRelations = resultsWithRelations?.FirstOrDefault(x => x.Id == item.Id);
                        if (withRelations is not null)
                            item.Relations = withRelations.Relations;

                        dict.Add(item.Id.Value, item);
                    }
                }
            }

            return dict;
        }
        catch (VssServiceResponseException ex)
        {
            if (ex.Message == "Not Found")
            {
                PrintLine("Work item not found", ConsoleColor.Red);
                PrintLine("Tried to get these ids:", ConsoleColor.DarkGray);
                foreach (int id in ids)
                {
                    PrintLine(id, ConsoleColor.DarkGray);
                }
            }

            throw;
        }
    }

    private async Task<WorkItem> PopulateRelations(WorkItem item)
    {
        IList<WorkItemRelation>? relations = item.Relations;
        return relations is null || relations.Count == 0
            ? item.Id is null
                ? throw new InvalidOperationException("no id on item")
                : await this.ado.WorkItems.GetWorkItemAsync(item.Id.Value, expand: WorkItemExpand.Relations)
            : item;
    }

    public async Task<WorkItem> SyncProperties(
        IEnumerable<ColumnMapping> sync,
        IDictionary<string, object> sourceData,
        WorkItem targetItem)
    {
        JsonPatchDocument patch = new();

        foreach (ColumnMapping syncItem in sync)
        {
            if (!sourceData.TryGetValue(syncItem.ExcelColumn, out object? valueInSource))
            {
                PrintLine($"Skipping {targetItem.Id}/{syncItem.AdoField} - value is not present in Excel column {syncItem.ExcelColumn}");
                continue;
            }

            object? oldValueInTarget = targetItem.Field(syncItem.AdoField);
            if (!Equals(sourceData[syncItem.ExcelColumn]?.ToString(), oldValueInTarget?.ToString()))
            {
                // only update if the value is different
                patch.Add(new JsonPatchOperation
                {
                    Operation = Operation.Replace,
                    Path = $"/fields/{syncItem.AdoField}",
                    Value = valueInSource
                });

                Print($"Will update {GetWorkItemUxHref(this.settings.Ado, targetItem.Id!.Value).OriginalString} ");
                Print(syncItem.AdoField, ConsoleColor.Cyan);
                Print(" from ");
                Print(oldValueInTarget, ConsoleColor.Yellow);
                Print(" to ");
                PrintLine(valueInSource, ConsoleColor.Green);
            }
            else
            {
                // uncomment for verbose output
                Print($"Skipping {this.settings.Ado.Org}/{targetItem.Id} ", ConsoleColor.DarkGray);
                Print(syncItem.AdoField, ConsoleColor.DarkCyan);
                Print(" because it is already ", ConsoleColor.DarkGray);
                PrintLine(oldValueInTarget, ConsoleColor.DarkGray);
            }
        }

        if (!this.settings.DryRun)
        {
            targetItem = patch.Count > 0
                ? await this.ado.WorkItems.UpdateWorkItemAsync(patch, this.settings.Ado.Project, targetItem.Id!.Value, expand: WorkItemExpand.All)
                : targetItem;
        }

        ColumnMapping[] recursiveSync = sync.Where(x => x.Recursive).ToArray();
        if (recursiveSync.Length > 0)
        {
            _ = await this.PopulateRelations(targetItem);
            WorkItemRelation[] children = targetItem.Relations.Where(x => x.Rel is "Child" or "System.LinkTypes.Hierarchy-Forward").ToArray();
            int[] childIds = children.Select(x => int.Parse(x.Url.Split('/')[^1])).ToArray();
            if (childIds.Length != 0)
            {
                IDictionary<int, WorkItem> childItems = await this.GetWorkItems(childIds);
                Task<WorkItem>[] tasks = childItems.Select(x => x.Value).Select(x => this.SyncProperties(recursiveSync, sourceData, x)).ToArray();
                _ = await Task.WhenAll(tasks);
            }
        }

        return targetItem;
    }

    private static Uri GetWorkItemUxHref(AdoSettings ado, int id)
    {
        string encodedProject = HttpUtility.UrlPathEncode(ado.Project);
        return new(ado.OrgUri, $"{encodedProject}/_workitems/edit/{id}");
    }
}