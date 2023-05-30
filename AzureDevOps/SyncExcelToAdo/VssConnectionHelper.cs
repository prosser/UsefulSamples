namespace SyncExcelToAdo;

using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

using System;
using System.Security.Cryptography;
using System.Text;

using static ConsoleHelper;

internal class VssConnectionHelper
{
    public static async Task<AdoClients> Connect(AdoSettings ado)
    {
        for (int i = 0; i < 1; i++)
        {
            try
            {
                VssCredentials creds = GetVssCredentials(ado);
                VssConnection connection = new(ado.OrgUri, creds);
                await connection.ConnectAsync();
                AdoClients clients = new(ado.Project, connection);
                await clients.Initialize(new(new[] { "Scenario", "Deliverable", "Feature", "Task Group", "Task" }));
                return clients;
            }
            catch
            {
                ClearVssCredentials(ado.Org);
                if (i > 0)
                    throw;
            }
        }

        throw new InvalidOperationException();
    }

    public static VssCredentials GetVssCredentials(AdoSettings settings)
    {
        if (settings.PersonalAccessToken.Length > 50)
            return new VssBasicCredential(string.Empty, settings.PersonalAccessToken);

        VssCredentials? creds = ReadCache(settings.Org);
        if (creds is null)
        {
            Print("To connect to Azure DevOps we need your Personal Access Token (PAT) for ");
            PrintLine(settings.Org, ConsoleColor.Cyan);
            PrintLine($"You can create one at https://dev.azure.com/{settings.Org}/_usersSettings/tokens");

            string? pat = null;
            while (pat is null)
            {
                Print("Enter your PAT: ", ConsoleColor.Yellow);
                pat = Console.ReadLine();
            }

            WriteCache(settings.Org, pat);
            creds = new VssBasicCredential(string.Empty, pat);
        }

        return creds;
    }

    public static void ClearVssCredentials(string org)
    {
        try
        {
            File.Delete($"vss.{org}.token");
        }
        catch (IOException) { }
    }

    private static VssCredentials? ReadCache(string org)
    {
        try
        {
            byte[] encrypted = File.ReadAllBytes($"vss.{org}.token");
            string pat = Encoding.UTF8.GetString(ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser));
            return new VssBasicCredential(string.Empty, pat);
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static void WriteCache(string org, string pat)
    {
        try
        {
            byte[] encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(pat), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes($"vss.{org}.token", encrypted);
        }
        catch (IOException)
        {
        }
    }
}
