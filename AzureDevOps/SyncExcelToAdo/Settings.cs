namespace SyncExcelToAdo;

using System;
using System.Collections.Generic;

/// <summary>
/// ADO configuration settings
/// </summary>
/// <param name="Org">ADO Organization Name</param>
/// <param name="Project">ADO Project Name</param>
/// <param name="PersonalAccessToken">ADO Personal Access Token with API Read/Write access to work items</param>
public record AdoSettings(string Org, string Project, string PersonalAccessToken)
{
    /// <summary>
    /// Gets the ADO Organization URI
    /// </summary>
    public Uri OrgUri => new($"https://dev.azure.com/{this.Org}/");
}

/// <summary>
/// Application configuration settings
/// </summary>
public class AppSettings
{
    /// <summary>
    /// If <c>true</c>, no changes will be made to ADO.
    /// </summary>
    public bool DryRun {get;set;}

    /// <summary>
    /// ADO configuration settings
    /// </summary>
    public AdoSettings Ado { get; set; } = default!;

    /// <summary>
    /// Excel configuration settings
    /// </summary>
    public ExcelSettings Excel { get; set; } = new();
}

/// <summary>
/// Excel configuration settings
/// </summary>
public class ExcelMappings
{
    /// <summary>
    /// Settings for columns to read from ADO and write to Excel
    /// </summary>
    public ColumnMapping[] ReadFromAdo { get; set; } = Array.Empty<ColumnMapping>();

    /// <summary>
    /// Settings for columns to read from Excel and write to ADO
    /// </summary>
    public ColumnMapping[] WriteToAdo { get; set; } = Array.Empty<ColumnMapping>();
}

/// <summary>
/// Excel configuration settings
/// </summary>
public class ExcelSettings
{
    /// <summary>
    /// The ADO ID column name
    /// </summary>
    public string AdoIdColumn { get; set; } = default!;

    /// <summary>
    /// Column mappings for reading and writing to ADO
    /// </summary>
    public ExcelMappings Mappings { get; set; } = new();

    /// <summary>
    /// The path to the Excel file
    /// </summary>
    public string Path { get; set; } = default!;

    /// <summary>
    /// The Excel sheet name
    /// </summary>
    public string SheetName { get; set; } = default!;
}

/// <summary>
/// Excel column to ADO field mapping settings
/// </summary>
public class ColumnMapping
{
    /// <summary>
    /// The ADO field name
    /// </summary>
    public string AdoField { get; set; } = default!;

    /// <summary>
    /// The Excel column name
    /// </summary>
    public string ExcelColumn { get; set; } = default!;

    /// <summary>
    /// If <c>true</c>, child work items will be updated recursively.
    /// </summary>
    public bool Recursive { get; set; }
}