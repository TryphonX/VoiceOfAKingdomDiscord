using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfAKingdomDiscord.Modules
{
    static class DiscordEventHandler
    {
        public static void SetEventTasks()
        {
            App.Client.Log += OnLog;
            App.Client.MessageReceived += OnMessageReceived;
            App.Client.ReactionAdded += OnReactionAdded;
            App.Client.LatencyUpdated += OnLatencyUpdated;
            App.Client.Disconnected += OnDisconnected;
        }

        private static Task OnDisconnected(Exception e)
        {
            CommonScript.LogWarn($"Disconnected. Exception: {e.Message}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Notifies the user about high latency or when it's restored.
        /// </summary>
        /// <param name="previousLatency"></param>
        /// <param name="currentLatency"></param>
        /// <returns></returns>
        private static Task OnLatencyUpdated(int previousLatency, int currentLatency)
        {
            if (currentLatency >= 400 && previousLatency < 400)
            {
                CommonScript.LogWarn($"High latency noted.\tLatency: {currentLatency}");
            }
            else if (currentLatency < 400 && previousLatency >= 400)
            {
                CommonScript.LogWarn($"Latency restored.\tLatency: {currentLatency}");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unCachedMessage"></param>
        /// <param name="channel"></param>
        /// <param name="reaction"></param>
        /// <returns></returns>
        private static Task OnReactionAdded(Cacheable<IUserMessage, ulong> unCachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (App.GameMgr.Games.Count == 0)
                return Task.CompletedTask;

            if (!App.GameMgr.Games.Any(game => game.PlayerID == reaction.UserId))
                return Task.CompletedTask;
            try
            {
                foreach (var game in App.GameMgr.Games)
                {
                    // Not the game's player
                    if (reaction.UserId != game.PlayerID)
                        continue;

                    // Not the game's channel
                    // Safe to exit the task
                    if (channel.Id != game.ChannelID)
                        break;

                    if (reaction.Emote.Name.Equals(CommonScript.CHECKMARK))
                    {
                        // Proceed to next month calculations
                        InitNewMonthPreparations(channel, reaction, game, true);
                    }
                    else if (reaction.Emote.Name.Equals(CommonScript.NO_ENTRY))
                    {
                        // Proceed to next month calculations
                        InitNewMonthPreparations(channel, reaction, game, false);
                    }
                    else
                    {
                        // Unexpected behavior
                        // Someone most likely broke the permissions
                        CommonScript.LogWarn("Invalid reaction. Possibly wrong permissions.");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                CommonScript.LogError(e.Message);
            }

            return Task.CompletedTask;
        }

        private static void InitNewMonthPreparations(ISocketMessageChannel channel, SocketReaction reaction, Game game, bool accepted)
        {
            channel.GetMessageAsync(reaction.MessageId)
                .ContinueWith(antecedent =>
                {
                    antecedent.Result.RemoveAllReactionsAsync();
                    if (antecedent.Result.Author.Id != App.Client.CurrentUser.Id)
                    {
                        CommonScript.LogWarn("Someone other than the bot sent a message. Wrong permissions.");
                    }
                });
            GameManager.ResolveRequest(game, accepted);
        }

        private static Task OnMessageReceived(SocketMessage msg)
        {
            // Game over. You ruled for {yearsInCommand} years " +
            // $"and {game.MonthsInControl - 2 % 12} months.
            if (msg.Author.Id == App.Client.CurrentUser.Id &&
                Regex.IsMatch(msg.Content, @"^Game over. You ruled for \d*? years? and \d*? months\.$"))
            {
                foreach (var game in App.GameMgr.Games)
                {
                    if (msg.Channel.Id != game.ChannelID)
                        continue;

                    Thread.Sleep(CommonScript.TIMEOUT_TIME);

                    GameManager.EndGame(game);
                    break;
                }

                return Task.CompletedTask;
            }
            else if (!msg.Content.StartsWith(Config.Prefix) || msg.Author.IsBot)
                return Task.CompletedTask;

            new CommandHandler().Run(msg);

            return Task.CompletedTask;
        }

        private static Task OnLog(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
