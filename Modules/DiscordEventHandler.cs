﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfAKingdomDiscord.Modules
{
    static class DiscordEventHandler
    {
        public static void SetEventTasks()
        {
            App.Client.Log += Client_Log;
            App.Client.MessageReceived += Client_MessageReceived;
        }

        private static Task Client_MessageReceived(SocketMessage msg)
        {
            if (!msg.Content.StartsWith(Config.Prefix) || msg.Author.IsBot)
                return Task.CompletedTask;

            new CommandHandler().Run(msg);

            return Task.CompletedTask;
        }

        private static Task Client_Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}