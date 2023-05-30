namespace SyncAdoInstances;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Web;

using static ConsoleHelper;

using Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation;

//
// Syncs work items from one project to another
//

// Prerequisites:
// az devops login (then paste in PAT when prompted)
internal class Program
{
    private static readonly JsonSerializerOptions serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly AppSettings config;
    private AdoClients? local;
    private AdoClients? remote;

    public Program(AppSettings config)
    {
        this.config = config;
    }

    private AdoClients Local => this.local ?? throw new InvalidOperationException("Local client not initialized.");

    private AdoClients Remote => this.remote ?? throw new InvalidOperationException("Remote client not initialized.");

    public async Task Run()
    {
        List<object> report = new();

        try
        {
            this.local = await VssConnectionHelper.Connect(this.config.Local);
            this.remote = await VssConnectionHelper.Connect(this.config.Remote);

            List<WorkItemRelationType> validLinkTypes = await this.local.WorkItems.GetRelationTypesAsync()
                ?? throw new InvalidOperationException("Could not get link types");

            // get work items from ADO
            IDictionary<int, WorkItem> remoteItems = await this.GetWorkItems(this.config.Items.Select(x => x.RemoteId), true, "Scenario");
            IDictionary<int, WorkItem> localItems = await this.GetWorkItems(this.config.Items.Select(x => x.Id), false, "Scenario");

            int[] duplicateIds = this.config.Items.GroupBy(x => x.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            int[] duplicateRemoteIds = this.config.Items.GroupBy(x => x.RemoteId).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            if (duplicateIds.Length > 0 || duplicateRemoteIds.Length > 0)
                throw new InvalidOperationException($"Duplicate items in config. Duplicate ids={JsonSerializer.Serialize(duplicateIds)}, Duplicate remoteIds={JsonSerializer.Serialize(duplicateRemoteIds)}");

            // process sync properties
            foreach (ConfigItem configItem in this.config.Items)
            {
                var localContext = new Dictionary<string, object?>()
                {
                    ["Href"] = GetWorkItemUxHref(this.config.Local, configItem.Id),
                };
                var remoteContext = new Dictionary<string, object?>()
                {
                    ["Href"] = GetWorkItemUxHref(this.config.Remote, configItem.RemoteId),
                };
                IDictionary<string, object?> context = new Dictionary<string, object?>()
                {
                    ["Local"] = localContext,
                    ["Remote"] = remoteContext,
                };
                report.Add(context);

                if (!localItems.TryGetValue(configItem.Id, out WorkItem? localItem))
                    throw new KeyNotFoundException($"No local item {configItem.Id}.\nLocal={GetWorkItemUxHref(this.config.Local, configItem.Id).OriginalString}\nRemote={GetWorkItemUxHref(this.config.Remote, configItem.RemoteId).OriginalString}");

                if (!remoteItems.TryGetValue(configItem.RemoteId, out WorkItem? remoteItem))
                    throw new KeyNotFoundException($"No remote item {configItem.RemoteId}.\nLocal={GetWorkItemUxHref(this.config.Local, configItem.Id).OriginalString}\nRemote={GetWorkItemUxHref(this.config.Remote, configItem.RemoteId).OriginalString}");

                SyncSettings[] syncToRemote = this.config.Sync?.Where(x => x.Authority == "local").ToArray() ?? Array.Empty<SyncSettings>();
                SyncSettings[] syncToLocal = this.config.Sync?.Where(x => x.Authority == "remote").ToArray() ?? Array.Empty<SyncSettings>();

                foreach (SyncSettings? syncItem in syncToLocal)
                {
                    UpdateContextProperty(remoteContext, syncItem.Property, remoteItem);
                }

                foreach (SyncSettings? syncItem in syncToRemote)
                {
                    UpdateContextProperty(localContext, syncItem.Property, localItem);
                }

                localItem = await this.SyncProperties(
                    remoteContext,
                    syncToLocal,
                    remoteItem,
                    this.config.Local,
                    localItem);

                remoteItem = await this.SyncProperties(
                    localContext,
                    syncToRemote,
                    localItem,
                    this.config.Remote,
                    remoteItem);

                foreach (string fullName in this.config.Report?.ReportIfNull ?? Array.Empty<string>())
                {
                    if (!localItem.Fields.TryGetValue(fullName, out object? value) || value is null)
                    {
                        // shouldReportChildren = true;
                        string warning = string.Format("{0} is null for {1} {2} {3}.\n{4}\nRecursing into children!",
                          fullName.ShortFieldName(),
                          localItem.Field<string>("System.WorkItemType"),
                          localItem.Id,
                          localItem.Field<string>("System.Title")[0..Console.BufferWidth],
                          GetWorkItemUxHref(this.config.Local, localItem.Id!.Value).OriginalString);

                        PrintLine(warning, ConsoleColor.Yellow);
                        AddContextArrayItem(context, "Warnings", warning);
                        break;
                    }
                }

                await this.DumpChildren(context, localItem, false);
            }

            for (int i = 0; i < this.config.Items.Count; i++)
            {
                ConfigItem configItem = this.config.Items[i];
                WorkItem item = localItems[configItem.Id];
                WorkItem remoteItem = remoteItems[configItem.RemoteId];

                try
                {
                    WorkItemRelationType strongLinkType = validLinkTypes.First(x => x.Name == configItem.Link);
                    Uri href = this.GetWorkItemApiHref(this.config.Remote, configItem.RemoteId);
                    WorkItemRelation? existingRelation = item.Relations.FirstOrDefault(x => x.Rel == strongLinkType.ReferenceName && x.Url == href.OriginalString);

                    if (existingRelation is null && strongLinkType is not null)
                    {
                        JsonPatchOperation op = new()
                        {
                            Operation = Operation.Add,
                            Path = "/relations/-",
                        };
                        JsonPatchDocument patch = new() { op };

                        string message = string.Format("{0} to {1} with {2}", configItem.Id, configItem.RemoteId, configItem.Link);
                        op.Value = strongLinkType.Attributes["remote"] is bool and true
                            ? (new { rel = strongLinkType.ReferenceName, url = href.OriginalString })
                            : (new { rel = strongLinkType.ReferenceName, id = configItem.RemoteId });

                        localItems[configItem.Id] = await this.local.WorkItems.UpdateWorkItemAsync(patch, configItem.Id, expand: WorkItemExpand.All);
                    }
                }
                catch
                {
                    PrintLine($"Failed to link {configItem.Id} to {configItem.RemoteId}", ConsoleColor.Red);
                }
            }

            // write report
            string reportPath = $"SyncAdoWorkItems-{DateTime.Now:yyyyMMddThhmmss}.json";
            using FileStream reportStream = File.OpenWrite(reportPath);
            await JsonSerializer.SerializeAsync(reportStream, report, serializerOptions);
            Print($"Report written to {reportPath}");
        }
        finally
        {
            this.remote?.Dispose();
            this.local?.Dispose();
        }
    }

    private static void AddContextArrayItem(IDictionary<string, object?> context, string property, object? value)
    {
        if (value is not null)
        {
            IList<object> list;
            if (context.TryGetValue(property, out object? objArray) && objArray is not null)
            {
                list = (IList<object>)objArray;
            }
            else
            {
                list = new List<object>();
                context.Add(property, list);
            }

            list.Add(value);
        }
    }

    private static Uri GetWorkItemUxHref(AdoSettings ado, int id)
    {
        string encodedProject = HttpUtility.UrlPathEncode(ado.Project);
        return new(ado.OrgUri, $"{encodedProject}/_workitems/edit/{id}");
    }

    private static async Task<int> Main(string[] args)
    {
        try
        {
            string configPath = args.FirstOrDefault() ?? "sync-work-items.json";
            string configJson = File.ReadAllText(configPath);
            AppSettings config = JsonSerializer.Deserialize<AppSettings>(configJson, serializerOptions)
                ?? throw new JsonException($"Unable to parse {configPath}");

            await new Program(config).Run();
            return 0;
        }
        catch (Exception ex)
        {
            PrintLine(ex.Message, ConsoleColor.Red, true);
            return 1;
        }
    }

    private static void UpdateContextProperty(IDictionary<string, object?> context, string property, WorkItem item)
    {
        string simpleName = property.Split(".")[^1];
        context[simpleName] = simpleName == "AssignedTo"
            ? ((Microsoft.VisualStudio.Services.WebApi.IdentityRef?)item.Field(property))?.DisplayName
            : item.Field(property);
    }

    private async Task DumpChildren(IDictionary<string, object?> reportContext, WorkItem parent, bool remote, string indent = "")
    {
        AdoSettings ado = this.GetAdo(remote);
        parent = await this.PopulateRelations(parent, remote);
        WorkItemRelation[] children = parent.Relations.Where(x => x.Rel is "Child" or "System.LinkTypes.Hierarchy-Forward").ToArray();
        int[] childIds = children.Select(x => int.Parse(x.Url.Split('/')[^1])).ToArray();
        if (childIds.Length == 0)
            return;

        IDictionary<int, WorkItem> childItems = await this.GetWorkItems(childIds, remote, "Feature", "Deliverable", "Task");
        foreach ((int id, WorkItem childItem) in childItems)
        {
            string childType = childItem.Type();
            if (childType is "Feature" or "Deliverable" or "Task")
            {
                Print(indent);
                Print($"Found child {childType} of {parent.Type()} {parent.Id}: ");

                PrintLine(this.GetWorkItemApiHref(this.config.Local, id), ConsoleColor.Blue);

                Dictionary<string, object?> reportedChild = new()
                {
                    ["Id"] = childItem.Id!.Value,
                    ["Type"] = childItem.Type(),
                    ["Href"] = GetWorkItemUxHref(ado, id),
                };

                AddContextArrayItem(reportContext, "Children", reportedChild);

                HashSet<string> recurseIfNullGroups = new();
                HashSet<string> notNull = new();
                if (this.config.Report is not null)
                {
                    foreach (ReportChildPropertiesSettings entry in this.config.Report.Properties)
                    {
                        string fullName = entry.Name;
                        string simpleName = fullName.Split(".")[^1];

                        Print(indent);
                        Print(simpleName, ConsoleColor.Cyan);
                        Print(": ");
                        string? childValue = childItem.Field(fullName)?.ToString();
                        UpdateContextProperty(reportedChild, fullName, childItem);

                        if (childValue is not null)
                        {
                            if (entry.RecurseIfNullGroup is not null)
                                _ = notNull.Add(entry.RecurseIfNullGroup);
                        }
                        else
                        {
                            childValue = "<null>";
                            if (entry.RecurseIfNullGroup is not null)
                                _ = recurseIfNullGroups.Add(entry.RecurseIfNullGroup);
                        }

                        PrintLine(childValue, ConsoleColor.Green);
                    }
                }

                foreach (string groupName in recurseIfNullGroups)
                {
                    if (!notNull.Contains(groupName))
                    {
                        if (childItem is null)
                            throw new InvalidOperationException("Child item is required");

                        await this.DumpChildren(reportedChild, childItem, remote, indent + "  ");
                    }
                }
            }
        }
    }

    private AdoSettings GetAdo(bool remote = false)
    {
        return remote ? this.config.Remote : this.config.Local;
    }

    private string[] GetPropertiesToQuery(string[] workItemTypes, bool remote = false)
    {
        string authority = remote ? "remote" : "local";
        AdoClients client = remote ? this.Remote : this.Local;

        HashSet<string> properties = new(workItemTypes.Length == 0 ? client.CommonFields : client.Fields(workItemTypes));
        HashSet<string> fuzzyMatches = new(properties.Select(x => x.ShortFieldName()));

        foreach (SyncSettings sync in this.config.Sync)
        {
            if (sync.Authority == authority || sync.TargetProperty is null)
            {
                if (fuzzyMatches.Add(sync.Property.ShortFieldName()))
                    _ = properties.Add(sync.Property);
            }
            else
            {
                if (fuzzyMatches.Add(sync.TargetProperty.ShortFieldName()))
                    _ = properties.Add(sync.TargetProperty);
            }
        }

        if (!remote && this.config.Report is not null)
        {
            foreach (string property in this.config.Report.ReportIfNull)
            {
                if (fuzzyMatches.Add(property.ShortFieldName()))
                    _ = properties.Add(property);
            }

            foreach (string? property in this.config.Report.Properties.Select(x => x.Name))
            {
                if (fuzzyMatches.Add(property.ShortFieldName()))
                    _ = properties.Add(property);
            }
        }

        return properties.OrderBy(x => x).ToArray();
    }

    private Uri GetWorkItemApiHref(AdoSettings ado, int id)
    {
        AdoClients client = ado == this.config.Remote ? this.Remote : this.Local;
        return new(ado.OrgUri, $"{client.ProjectId}/_apis/wit/workItems/{id}");
    }

    private async Task<IDictionary<int, WorkItem>> GetWorkItems(IEnumerable<int> ids, bool remote = false, params string[] workItemTypes
        )
    {
        string[] fields = this.GetPropertiesToQuery(workItemTypes, remote);

        AdoClients client = remote ? this.Remote : this.Local;
        _ = remote ? this.config.Remote.Project : this.config.Local.Project;
        var result = (await client.WorkItems.GetWorkItemsAsync(ids, fields))
            .Where(x => x.Type() == "Scenario")
            .ToList();
        List<WorkItem>? resultsWithRelations = await client.WorkItems.GetWorkItemsAsync(ids, expand: WorkItemExpand.Relations);

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

    private async Task<WorkItem> PopulateRelations(WorkItem item, bool remote = false)
    {
        IList<WorkItemRelation>? relations = item.Relations;
        if (relations is null || relations.Count == 0)
        {
            AdoClients client = remote ? this.Remote : this.Local;
            return item.Id is null
                ? throw new InvalidOperationException("no id on item")
                : await client.WorkItems.GetWorkItemAsync(item.Id.Value, expand: WorkItemExpand.Relations);
        }

        return item;
    }

    private async Task<WorkItem> SyncProperties(
        IDictionary<string, object?> reportContext,
        IList<SyncSettings> sync,
        WorkItem sourceItem,
        AdoSettings targetAdo,
        WorkItem targetItem)
    {
        AdoClients sourceClient = targetAdo == this.config.Remote ? this.Local : this.Remote;
        AdoClients targetClient = targetAdo == this.config.Remote ? this.Remote : this.Local;

        JsonPatchDocument patch = new();

        foreach (SyncSettings syncItem in sync)
        {
            string propertyInSource = sourceClient.GetFullFieldName(syncItem.Property, sourceItem.Type());
            string propertyInTarget = targetClient.GetFullFieldName(syncItem.TargetProperty, targetItem.Type());

            object? valueInSource;
            if (propertyInSource == "id")
            {
                // id is a special case (it's not a field)
                valueInSource = sourceItem.Id!.Value;
            }
            else
            {
                valueInSource = sourceItem.Field(propertyInSource);
            }

            object? oldValueInTarget = targetItem.Field(propertyInTarget);
            if (!Equals(valueInSource?.ToString(), oldValueInTarget?.ToString()))
            {
                // only update if the value is different
                patch.Add(new JsonPatchOperation
                {
                    Operation = Operation.Replace,
                    Path = $"/fields/{propertyInTarget}",
                    Value = valueInSource
                });

                Print($"Will update {GetWorkItemUxHref(targetAdo, targetItem.Id!.Value).OriginalString} ");
                Print(propertyInTarget.ShortFieldName(), ConsoleColor.Cyan);
                Print(" from ");
                Print(oldValueInTarget, ConsoleColor.Yellow);
                Print(" to ");
                PrintLine(valueInSource, ConsoleColor.Green);

                reportContext[propertyInTarget] = valueInSource;
            }
            else
            {
                // uncomment for verbose output
                Print($"Skipping {targetAdo.Org}/{targetItem.Id} ", ConsoleColor.DarkGray);
                Print(propertyInTarget.ShortFieldName(), ConsoleColor.DarkCyan);
                Print(" because it is already ", ConsoleColor.DarkGray);
                PrintLine(oldValueInTarget, ConsoleColor.DarkGray);
            }
        }

        return patch.Count > 0
            ? await targetClient.WorkItems.UpdateWorkItemAsync(patch, targetAdo.Project, targetItem.Id!.Value, expand: WorkItemExpand.All)
            : targetItem;
    }
}