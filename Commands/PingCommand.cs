﻿using System;
using System.Collections.Generic;
using System.Text;
using VoiceOfAKingdomDiscord.Modules;

namespace VoiceOfAKingdomDiscord.Commands
{
    class PingCommand: Command
    {
        public PingCommand()
        {
            Name = "ping";
            Abbreviations.Add(Name);
            Description = "Check if the bot is responding.";
        }

        public override void Run(CommandHandler cmdHandler)
        {
            base.Run(cmdHandler);

            int ping = App.Client.Latency;

            if (ping > 400)
            {
                CommonScript.LogWarn($"High latency noted. Latency: {ping}");
            }

            cmdHandler.Msg.Channel.SendMessageAsync($"Response time: `{ping}ms`");
        }
    }
}
