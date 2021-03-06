﻿using Discord;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VoiceOfAKingdomDiscord.Modules
{
    static class CommonScript
    {
        public static string UnicodeAccept { get; } = "✅";
        public static string UnicodeReject { get; } = "⛔";
        public static string Version { get; } = "0.1.0";
        public static string Author { get; } = "Tryphon Ksydas";
        public static string[] Collaborators { get; } = { "ZarOS69" };
        public static string Title { get; } = "Voice of a Kingdom";
        public static int[] MonthsWith31Days { get; } = { 1, 3, 5, 7, 8, 10, 12 };
        public static Random Rng { get; } = new Random();
        public static EmbedFieldBuilder EmptyEmbedField =>
            new EmbedFieldBuilder()
                .WithName("\u200B")
                .WithValue("\u200B");

        public static void Log(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            PrintLine($"App\t     {msg}");
            Console.ResetColor();
        }

        public static void LogWarn(string msg)
        {
            StackFrame stackFrame = new StackTrace(1, true).GetFrame(0);

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            PrintLine($"*WARN\t     {msg} @ {GetClassName(stackFrame.GetFileName())}.{stackFrame.GetMethod().Name}");
            Console.ResetColor();
        }

        public static void LogError(string msg)
        {
            StackFrame errorFrame = new StackTrace(1, true).GetFrame(0);

            Console.ForegroundColor = ConsoleColor.Red;
            PrintLine($"**ERR\t     {msg} @ {GetClassName(errorFrame.GetFileName())}.{errorFrame.GetMethod().Name}");
            Console.ResetColor();
        }

        public static void DebugLog(object msg, bool skipOneFrame = false)
        {
            if (!Config.IsDebug)
                return;

            StackFrame stackFrame;
            if (skipOneFrame)
            {
                stackFrame = new StackTrace(2, true).GetFrame(0);
            }
            else
            {
                stackFrame = new StackTrace(1, true).GetFrame(0);
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            PrintLine($"Debug\t     {msg} @ {GetClassName(stackFrame.GetFileName())}.{stackFrame.GetMethod().Name}");
            Console.ResetColor();
        }

        private static void PrintLine(string msg) =>
            Console.WriteLine($"{DateTime.Now.ToLocalTime().ToLongTimeString()} {msg}");

        private static string GetClassName(string fileName) =>
            fileName.Split('\\').Last().TrimEnd('s', 'c', '.');

        public static DateTime GetRandomDate()
        {
            DateTime start = new DateTime(1600, 1, 1);
            int range = (DateTime.Today - start).Days;
            return start.AddDays(Rng.Next(range)).ToLocalTime();
        }

        public static int RoundToX(int num, int roundTo = 10)
        {
            int rem = num % roundTo;
            return rem >= roundTo/2 ? (num - rem + roundTo) : (num - rem);
        }

        public static short Check0To100Range(short stat)
        {
            if (stat > 100)
            {
                stat = 100;
            }
            else if (stat < 0)
            {
                stat = 0;
            }

            return stat;
        }

        public static short GetRandomPercentage() =>
            (short)Rng.Next(0, 99);
    }
}
