namespace SyncExcelToAdo;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using MiniExcelLibs;

using System.Data;
using System.Diagnostics;

using static ConsoleHelper;

internal class Program
{
    private readonly AppSettings settings;

    private static async Task<int> Main(bool dryRun)
    {
        try
        {
            string configPath = "appsettings.json";
            string configJson = File.ReadAllText(configPath);
            AppSettings settings = new();

            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile(configPath, optional: false, reloadOnChange: true)
                .AddUserSecrets(typeof(AppSettings).Assembly)
                .Build();

            config.Bind(settings);
            if (dryRun)
            {
                settings.DryRun = dryRun;
            }
            else if (Debugger.IsAttached)
            {
                Print("Press D for dry run, any other key to continue: ");
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key == ConsoleKey.D)
                {
                    settings.DryRun = true;
                    PrintLine("Dry run enabled", ConsoleColor.Yellow);
                }
                else
                {
                    PrintLine("Dry run disabled", ConsoleColor.Yellow);
                }
            }

            if (!File.Exists(settings.Excel.Path))
                throw new FileNotFoundException($"Excel file not found at {settings.Excel.Path}", settings.Excel.Path);

            await new Program(settings).Run();
            return 0;
        }
        catch (Exception ex)
        {
            PrintLine(ex.Message, ConsoleColor.Red, true);
            PrintLine(ex.ToString(), ConsoleColor.DarkGray, true);
            return 1;
        }
    }

    public Program(AppSettings settings)
    {
        this.settings = settings;
    }


    public async Task Run()
    {
        AdoClients? ado = null;
        try
        {
            ado = await VssConnectionHelper.Connect(this.settings.Ado);
            using FileStream stream = File.OpenRead(this.settings.Excel.Path);

            string[] columns;
            try
            {
                columns = stream.GetColumns(true, this.settings.Excel.SheetName).ToArray();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("sheetName"))
                {
                    stream.Position = 0;
                    PrintLine($"Sheet name '{this.settings.Excel.SheetName}' not found in {this.settings.Excel.Path}", ConsoleColor.Red);
                    PrintLine("Sheet names:", ConsoleColor.DarkGray);
                    stream.GetSheetNames().ForEach(x => PrintLine(x, ConsoleColor.DarkGray));
                    
                }

                throw;
            }


            WorkItemHelper syncHelper = new(ado, this.settings);

            Dictionary<string, object>[] allRows = stream.Query(true, this.settings.Excel.SheetName)
                .Select(dyn => (IDictionary<string, object>)dyn)
                .Select(x => x.ToDictionary(kv => kv.Key, kv => kv.Value))
                .ToArray();
            Dictionary<string, object>[] rows = allRows
                //.Where(row => row.TryGetValue("Work Item Type", out object? value) && value is string s && s == "Deliverable")
                .Where(row => this.settings.Excel.RowFilters.All(filter => row.TryGetValue(filter.Column, out object? x) && Equals(x, filter.Value)))
                .ToArray();

            int[] workItemIds = rows
                .Select(row => row[this.settings.Excel.AdoIdColumn]?.ToString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => int.Parse(x!))
                .ToArray();

            // find duplicate ids
            int[] duplicateIds = workItemIds
                 .GroupBy(x => x)
                 .Where(g => g.Count() > 1)
                 .Select(g => g.Key)
                 .ToArray();

            if (duplicateIds.Length > 0)
            {
                throw new InvalidOperationException($"Duplicate work item ids found in the Excel file: {string.Join(", ", duplicateIds)}");
            }

            IDictionary<int, WorkItem> workItems = await syncHelper.GetWorkItems(workItemIds);

            Task[] syncTasks = rows.Select(row =>
            {
                string? strId = row[this.settings.Excel.AdoIdColumn]?.ToString();
                if (strId is null)
                {
                    return Task.CompletedTask;
                }

                int adoId = int.Parse(strId);

                var excelValues = this.settings.Excel.Mappings.WriteToAdo
                    .Select(s => (Key: s.ExcelColumn, Value: row[s.ExcelColumn]))
                    .Where(kv => kv.Value != null)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

                PrintLine($"{adoId}: {string.Join(", ", excelValues.Select(kv => $"{kv.Key} = {kv.Value}"))}", ConsoleColor.DarkGray);

                WorkItem workItem = workItems[adoId];
                return syncHelper.SyncProperties(this.settings.Excel.Mappings.WriteToAdo, excelValues, workItem);
            }).ToArray();

            await Task.WhenAll(syncTasks);
        }
        finally
        {
            ado?.Dispose();
        }
    }

    private record Kv(string Key, object Value);
}