namespace SyncExcelToAdo;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using MiniExcelLibs;

using System.Data;

using static ConsoleHelper;

internal class Program
{
    private readonly AppSettings settings;
    private AdoClients? ado;

    private static async Task<int> Main()
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

    private AdoClients Ado => this.ado ?? throw new InvalidOperationException("Local client not initialized.");

    public async Task Run()
    {
        try
        {
            this.ado = await VssConnectionHelper.Connect(this.settings.Ado);
            using FileStream stream = File.OpenRead(this.settings.Excel.Path);

            string[] columns = stream.GetColumns(true, this.settings.Excel.SheetName).ToArray();

            WorkItemHelper syncHelper = new(this.ado, this.settings);

            int[] workItemIds = stream.Query(true, this.settings.Excel.SheetName)
                .Select(dyn => (IDictionary<string, object>)dyn)
                .Select(row => row[this.settings.Excel.AdoIdColumn]?.ToString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => int.Parse(x!))
                .ToArray();

            IDictionary<int, WorkItem> workItems = await syncHelper.GetWorkItems(workItemIds);

            foreach (IDictionary<string, object> row in stream.Query(true, this.settings.Excel.SheetName))
            {
                string? strId = row[this.settings.Excel.AdoIdColumn]?.ToString();
                if (strId is null)
                    continue;

                int adoId = int.Parse(strId);

                var excelValues = this.settings.Excel.Mappings.WriteToAdo
                    .Select(s => (Key: s.ExcelColumn, Value: row[s.ExcelColumn]))
                    .Where(kv => kv.Value != null)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

                PrintLine($"{adoId}: {string.Join(", ", excelValues.Select(kv => $"{kv.Key} = {kv.Value}"))}", ConsoleColor.DarkGray);

                WorkItem workItem = workItems[adoId];
                _ = await syncHelper.SyncProperties(this.settings.Excel.Mappings.WriteToAdo, excelValues, workItem);


            }
        }
        finally
        {
            this.ado?.Dispose();
        }
    }

    private record Kv(string Key, object Value);
}