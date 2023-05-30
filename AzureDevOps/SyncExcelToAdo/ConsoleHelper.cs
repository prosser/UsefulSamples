namespace SyncExcelToAdo;

using System;

internal static class ConsoleHelper
{
    public static void Print(string message, ConsoleColor color = ConsoleColor.White, bool useStdErr = false)
    {
        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        if (useStdErr)
        {
            Console.Error.Write(message);
        }
        else
        {
            Console.Write(message);
        }

        Console.ForegroundColor = originalColor;
    }

    public static void Print(object? message, ConsoleColor color = ConsoleColor.White, bool useStdErr = false)
    {
        Print(message?.ToString() ?? "<null>", color, useStdErr);
    }

    public static void PrintLine(string message, ConsoleColor color = ConsoleColor.White, bool useStdErr = false)
    {
        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        if (useStdErr)
        {
            Console.Error.WriteLine(message);
        }
        else
        {
            Console.WriteLine(message);
        }

        Console.ForegroundColor = originalColor;
    }

    public static void PrintLine(object? message, ConsoleColor color = ConsoleColor.White, bool useStdErr = false)
    {
        PrintLine(message?.ToString() ?? "<null>", color, useStdErr);
    }
}