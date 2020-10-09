﻿using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;

namespace VoiceOfAKingdomDiscord.Modules
{
    class GameManager
    {
        private const string UP_ARROW_SMALL = "\\🔼";
        private const string DOWN_ARROW_SMALL = "\\🔻";
        private const string STEADY_ICON = "\\➖";
        private const short SMALL_CHANGE = 4;
        private const short MEDIUM_CHANGE = 9;
        private const short BIG_CHANGE = 12;

        public List<Game> Games { get; } = new List<Game>();
        private const int PROGRESS_BAR_BOXES = 10;
        public static bool HasGame(List<Game> games, ulong userID) =>
            games.Any(game => game.PlayerID == userID);
        public List<Request> Requests { get; } = new List<Request>()
        {
            new Request("Some question?", Person.General,
                new Game.KingdomStatsClass(folks: -MEDIUM_CHANGE, military: BIG_CHANGE, wealth: -SMALL_CHANGE),
                new Game.PersonalStatsClass(happiness: -MEDIUM_CHANGE, charisma: SMALL_CHANGE),
                new Game.KingdomStatsClass(folks: MEDIUM_CHANGE, military: -BIG_CHANGE, wealth: SMALL_CHANGE),
                new Game.PersonalStatsClass(happiness: MEDIUM_CHANGE, charisma: -SMALL_CHANGE),
                "Thank you or something.",
                "You will regret this or something.")
        };

        public static bool TryGetGame(ulong userID, out Game game)
        {
            game = null;
            if (HasGame(App.GameMgr.Games, userID))
            {
                foreach (var gameMgrGame in App.GameMgr.Games)
                {
                    if (gameMgrGame.PlayerID == userID)
                    {
                        game = gameMgrGame;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static void EndGame(Game game)
        {
            try
            {
                GetGameGuildChannel(game).DeleteAsync();
            }
            catch (Exception e)
            {
                CommonScript.LogError(e.Message);
            }

            App.GameMgr.Games.Remove(game);
        }

        public static SocketGuildChannel GetGameGuildChannel(Game game)
        {
            foreach (var channel in App.Client.GetGuild(game.GuildID).GetCategoryChannel(Config.GamesCategoryID).Channels)
            {
                if (channel.Id != game.ChannelID)
                    continue;

                return channel;
            }

            return null;
        }

        public static ISocketMessageChannel GetGameMessageChannel(Game game)
        {
            foreach (var channel in App.Client.GetGuild(game.GuildID).GetCategoryChannel(Config.GamesCategoryID).Channels)
            {
                if (channel.Id != game.ChannelID)
                    continue;

                return (ISocketMessageChannel)channel;
            }

            return null;
        }

        /// <summary>
        /// Creates the new month embed. Format: https://i.imgur.com/ZEUIPeR.png
        /// </summary>
        /// <param name="game"></param>
        /// <param name="request"></param>
        /// <returns>The new month embed.</returns>
        public static Embed GetNewMonthEmbed(Game game)
        {
            // Base
            EmbedBuilder embed = new CustomEmbed()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName($"Month {++game.MonthsInControl} | {game.Date.ToLongDateString()}"))
                .WithTitle($"\\{game.CurrentRequest.Person.Icon} {game.CurrentRequest.Person.Name}")
                .WithThumbnailUrl(game.CurrentRequest.Person.ImgUrl)
                .WithColor(game.CurrentRequest.Person.Color)
                .WithDescription(game.CurrentRequest.Question)
                .WithTimestamp(game.Date)
                .AddField(CommonScript.EmptyEmbedField());

            StringBuilder sb = new StringBuilder();
            #region On Accept
            // init sb
            // then add all the stat changes about the request
            // in the case you accept
            sb.Append(GetStatChangesString(game.CurrentRequest.KingdomStatsOnAccept));
            sb.Append(GetStatChangesString(game.CurrentRequest.PersonalStatsOnAccept));

            embed.AddField(new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName("Changes on Accept")
                .WithValue(sb.ToString()));
            #endregion

            sb.Clear();
            #region On Reject
            // Same things as on accept
            sb.Append(GetStatChangesString(game.CurrentRequest.KingdomStatsOnReject));
            sb.Append(GetStatChangesString(game.CurrentRequest.PersonalStatsOnReject));

            embed.AddField(new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName("Changes on Reject")
                .WithValue(sb.ToString()));
            #endregion

            embed.AddField(CommonScript.EmptyEmbedField());

            #region Folks
            embed.AddField(new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName($"{Person.Folk.Icon} Folks: {game.KingdomStats.Folks}")
                .WithValue(PrepareStatFieldValue(game.KingdomStats.Folks)));
            #endregion

            #region Nobles
            embed.AddField(new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName($"{Person.Noble.Icon} Nobles: {game.KingdomStats.Nobles}")
                .WithValue(PrepareStatFieldValue(game.KingdomStats.Nobles)));
            #endregion

            embed.AddField(CommonScript.EmptyEmbedField());

            #region Military
            embed.AddField(new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName($"{Person.General.Icon} Military: {game.KingdomStats.Military}")
                .WithValue(PrepareStatFieldValue(game.KingdomStats.Military)));
            #endregion

            #region Wealth
            embed.AddField(new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName($":coin: Wealth: {game.KingdomStats.Wealth}")
                .WithValue(PrepareStatFieldValue(game.KingdomStats.Wealth)));
            #endregion

            embed.AddField(CommonScript.EmptyEmbedField());

            embed.AddField(new EmbedFieldBuilder()
                .WithName("🤔 Personal Info")
                .WithValue("=============================="));

            #region Happiness
            embed.AddField(new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName($"😄 Happiness: {game.PersonalStats.Happiness}")
                .WithValue(PrepareStatFieldValue(game.PersonalStats.Happiness)));
            #endregion

            #region Charisma
            embed.AddField(new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName($"🤵‍♂️ Charisma: {game.PersonalStats.Charisma}")
                .WithValue(PrepareStatFieldValue(game.PersonalStats.Charisma)));
            #endregion

            return embed.Build();
        }

        /// <summary>
        /// Creates the string for the stat effects.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static string GetStatChangesString(object obj)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var property in obj.GetType().GetProperties())
            {
                if ((short)property.GetValue(obj) > 0)
                {
                    sb.AppendLine($"{UP_ARROW_SMALL} {property.Name}");
                }
                else if ((short)property.GetValue(obj) < 0)
                {
                    sb.AppendLine($"{DOWN_ARROW_SMALL} {property.Name}");
                }
                else
                {
                    sb.AppendLine($"{STEADY_ICON} {property.Name}");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Prepares the Value of the stat field (bar).
        /// </summary>
        /// <param name="stat"></param>
        /// <returns>The string with the linear progress bar of the stat.</returns>
        private static string PrepareStatFieldValue(int stat)
        {
            StringBuilder sb = new StringBuilder("[");
            int roundedStat = CommonScript.RoundToX(stat, PROGRESS_BAR_BOXES);
            for (short i = 0; i < PROGRESS_BAR_BOXES; i++)
            {
                if (i * PROGRESS_BAR_BOXES >= roundedStat)
                {
                    sb.Append("□");
                }
                else
                {
                    sb.Append("■");
                }
            }

            return sb.Append("]").ToString();
        }

        public static void ResolveRequest(Game game, bool accepted)
        {
            CommonScript.DebugLog("init");
            Game.KingdomStatsClass incKingdomStats = accepted ? game.CurrentRequest.KingdomStatsOnAccept : game.CurrentRequest.KingdomStatsOnReject;
            Game.PersonalStatsClass incPersonalStats = accepted ? game.CurrentRequest.PersonalStatsOnAccept : game.CurrentRequest.PersonalStatsOnReject;

            game.KingdomStats += incKingdomStats;
            game.PersonalStats += incPersonalStats;

            GetGameMessageChannel(game).SendMessageAsync(embed: GetResolveRequestEmbed(game, accepted)).Wait();

            // TODO: Add some chances to die here

            NextMonth(game);
        }

        private static Embed GetResolveRequestEmbed(Game game, bool accepted)
        {
            return new CustomEmbed()
                .WithColor(game.CurrentRequest.Person.Color)
                .WithThumbnailUrl(game.CurrentRequest.Person.ImgUrl)
                .WithTitle(accepted ? $"{CommonScript.CHECKMARK} Accepted" : $"{CommonScript.NO_ENTRY} Rejected")
                .WithDescription(accepted ? game.CurrentRequest.ResponseOnAccepted : game.CurrentRequest.ResponseOnRejected)
                .WithTimestamp(game.Date)
                .Build();
        }

        private static void NextMonth(Game game)
        {
            game.CurrentRequest = GetRandomRequest();

            game.Date = AddMonthToDate(game);

            GetGameMessageChannel(game).SendMessageAsync(embed: GetNewMonthEmbed(game))
                .ContinueWith(antecedent =>
                {
                    antecedent.Result.AddReactionAsync(new Emoji(CommonScript.CHECKMARK)).Wait();
                    antecedent.Result.AddReactionAsync(new Emoji(CommonScript.NO_ENTRY)).Wait();
                });
        }

        public static Request GetRandomRequest() =>
            App.GameMgr.Requests[new Random().Next(0, App.GameMgr.Requests.Count - 1)];

        private static DateTime AddMonthToDate(Game game)
        {
            int monthDays = CommonScript.MonthsWith31Days.Any(monthNum => monthNum == game.Date.Month) ? 31 : 30;
            int minDays = monthDays - game.Date.Day;
            int maxDays = monthDays * 2 - game.Date.Day;

            return game.Date.AddDays(new Random().Next(minDays, maxDays));
        }
    }
}
