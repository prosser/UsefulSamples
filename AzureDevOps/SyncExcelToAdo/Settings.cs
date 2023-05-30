namespace SyncExcelToAdo;

using System;
using System.Collections.Generic;

public record AdoSettings(string Org, string Project, string PersonalAccessToken)
{
    public Uri OrgUri => new($"https://dev.azure.com/{this.Org}/");
}

public class AppSettings
{
    public AdoSettings Ado { get; set; } = default!;
    public ExcelSettings Excel { get; set; } = new();
}

public class ExcelMappings
{
    public ColumnMappings ReadFromAdo { get; set; } = new();
    public WriteToAdoSettings[] WriteToAdo { get; set; } = Array.Empty<WriteToAdoSettings>();
}

public class ExcelSettings
{
    public string AdoIdColumn { get; set; } = default!;
    public ExcelMappings Mappings { get; set; } = new();
    public string Path { get; set; } = default!;
    public string SheetName { get; set; } = default!;
}

/// <summary>
/// Key = ADO property name, Value = Excel column name
/// </summary>
public class ColumnMappings : Dictionary<string, string>
{ }

public class WriteToAdoSettings
{
    public string AdoField { get; set; } = default!;
    public string ExcelColumn { get; set; } = default!;
    public bool Recursive { get; set; }
}