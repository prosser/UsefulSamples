namespace SyncAdoInstances;

using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

using System.Collections.Immutable;

internal class AdoClients : IDisposable
{
    private readonly VssConnection connection;
    private readonly Dictionary<string, ImmutableArray<string>> fieldsByWorkItemType = new();

    public AdoClients(AdoSettings settings, VssConnection connection)
    {
        this.Settings = settings;
        this.connection = connection;
        this.WorkItems = connection.GetClient<WorkItemTrackingHttpClient>();
        this.Projects = connection.GetClient<ProjectHttpClient>();
    }

    public string ProjectName => this.Settings.Project;
    public WorkItemTrackingHttpClient WorkItems { get; }
    public ProjectHttpClient Projects { get; }

    public async Task Initialize(HashSet<string> queriedWorkItemTypes)
    {
        this.CommonFields = ImmutableArray<string>.Empty;
        foreach (Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItemType? workItemType in await this.WorkItems.GetWorkItemTypesAsync(this.ProjectName))
        {
            string type = workItemType.Name;
            if (queriedWorkItemTypes.Contains(type))
            {
                this.fieldsByWorkItemType[type] = workItemType.Fields.Select(x => x.ReferenceName).ToImmutableArray();
                this.CommonFields = this.CommonFields.Length == 0
                    ? this.fieldsByWorkItemType[type]
                    : this.CommonFields.Intersect(this.fieldsByWorkItemType[type]).ToImmutableArray();
            }
        }

        this.ProjectId = (await this.Projects.GetProject(this.ProjectName)).Id;
    }

    public void Dispose()
    {
        ((IDisposable)this.Projects).Dispose();
        ((IDisposable)this.WorkItems).Dispose();
        this.connection.Dispose();
    }

    public Guid ProjectId { get; private set; } = Guid.Empty;

    public ImmutableArray<string> Fields(params string[] workItemType)
    {
        if (workItemType.Length == 0)
            return this.CommonFields;

        ImmutableArray<string> intersection = ImmutableArray<string>.Empty;

        for (int i = 0; i < workItemType.Length; i++)
        {
            string type = workItemType[i];
            intersection = i == 0
                ? this.fieldsByWorkItemType[type]
                : intersection.Intersect(this.fieldsByWorkItemType[type]).ToImmutableArray();
        }

        return intersection;
    }

    public ImmutableArray<string> CommonFields { get; private set; } = ImmutableArray<string>.Empty;
    public AdoSettings Settings { get; }

    public string GetFullFieldName(string fieldName, string workItemType)
    {
        string shortName = fieldName.ShortFieldName();
        ImmutableArray<string> fields = this.fieldsByWorkItemType[workItemType];
        return fields.First(x => x.ShortFieldName().Equals(shortName, StringComparison.OrdinalIgnoreCase));
    }
}