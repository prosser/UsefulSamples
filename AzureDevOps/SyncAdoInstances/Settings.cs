namespace SyncAdoInstances;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using System.Collections.Generic;

public record ConfigItem(int Id, int RemoteId, string? Link = null);
public record AdoSettings(string Org, string Project)
{
    public Uri OrgUri => new($"https://dev.azure.com/{this.Org}/");
}

public record ReportSettings(IList<string> ReportIfNull, IList<ReportChildPropertiesSettings> Properties);
public record ReportChildPropertiesSettings(string Name, string RecurseIfNullGroup);

public class AppSettings
{
    public IList<ConfigItem> Items { get; set; } = new List<ConfigItem>();
    public AdoSettings Local { get; set; } = default!;
    public AdoSettings Remote { get; set; } = default!;
    public ReportSettings? Report { get; set; }
    public IList<SyncSettings> Sync { get; set; } = new List<SyncSettings>();
}

public record SyncSettings
{
    private string? targetProperty;

    public string Authority { get; init; } = default!;
    public string Property { get; init; } = default!;

    public string TargetProperty
    {
        get => this.targetProperty ?? this.Property;
        init => this.targetProperty = value;
    }
}

public record Report(IList<ReportItem> Items);

public record ReportItem(WorkItem Scenario, AdoSettings Remote, AdoSettings Local)
{
    public IList<string>? Warnings { get; set; }
}