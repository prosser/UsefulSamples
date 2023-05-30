namespace SyncAdoInstances;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

internal static class WorkItemExtensions
{
    public static string Type(this WorkItem workItem)
    {
        return (string)workItem.Fields["System.WorkItemType"];
    }

    public static object? Field(this WorkItem workItem, string fieldName)
    {
        return workItem.Fields.TryGetValue(fieldName, out object? field)
            ? field
            : workItem.Fields.FirstOrDefault(x => x.Key.ShortFieldName() == fieldName.ShortFieldName()).Value;
    }

    public static T Field<T>(this WorkItem workItem, string fieldName)
    {
        return (T)workItem.Field(fieldName)!;
    }

    public static string ShortFieldName(this string fieldName)
    {
        return fieldName[(fieldName.LastIndexOf('.') + 1)..];
    }
}
