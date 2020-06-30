using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using RoleplaySwissArmyKnife.Models;
using RoleplaySwissArmyKnife.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RoleplaySwissArmyKnife.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        private class InitContext
        {
            public ulong             CommandChannelId;
            public ITextChannel      CommandChannel;
            public ulong             ResultChannelId;
            public ITextChannel      ResultChannel;
            public bool              IsControlled;
            public InitiativeState   State;
        }

        private async Task<InitContext> GetInitContext( ulong channelId )
        {
            var rid = await StorageService.GetResultChannel(channelId);
            return new InitContext
            {
                CommandChannelId = channelId,
                CommandChannel   = Context.Client.GetChannel(channelId) as ITextChannel,
                ResultChannelId  = rid,
                ResultChannel    = Context.Client.GetChannel(rid) as ITextChannel,
                IsControlled     = channelId != rid,
                State            = await StorageService.GetInitiative(rid) ?? new InitiativeState() { ChannelID = rid }
            };
        }

        //// Dependency Injection will fill this value in for us
        //public PictureService PictureService { get; set; }

        public StorageService StorageService { get; set; }

        [Command("set_initiative_control")]
        [Alias("setctrl", "setcontrol")]
        public async Task SetControlAsync(IChannel channel = null)
        {
            if (channel == null)
            {
                await Context.Channel.SendMessageAsync(
                    "Error: You must specify another channel to control from here.");
                return;
            }

            var textChannel = channel as ITextChannel;
            if (textChannel == null)
            {
                await Context.Channel.SendMessageAsync(
                    "Error: Target channel must be a text-based channel.");
                return;
            }

            await StorageService.SetResultChannel(Context.Channel.Id, channel.Id);
            if (Context.Channel.Id == channel.Id)
                await Context.Channel.SendMessageAsync("Channel control selection cleared.");
            else
                await Context.Channel.SendMessageAsync($"Channel control set for {channel.Name}.");
        }

        [Command("clear_initiative_control")]
        [Alias("clearctrl", "clearcontrol")]
        public async Task ClearControlAsync()
        {
            await StorageService.SetResultChannel(Context.Channel.Id, Context.Channel.Id);
            await Context.Channel.SendMessageAsync("Channel control selection cleared.");
        }

        [Command("setup_initiative")]
        [Alias("setup", "setupinit")]
        public async Task SetupInitiativeAsync(IChannel channel = null)
        {
            channel ??= Context.Channel;

            var textChannel = channel as ITextChannel;
            if (textChannel == null)
            {
                await Context.Channel.SendMessageAsync(
                    "Error: Target channel must be a text-based channel.");
                return;
            }

            var ctx = await GetInitContext(channel.Id);

            // Sanity check: Do we already have a pinned message set up?
            if (ctx.State.PinnedListMessageID != 0)
            {
                bool alreadySetup = false;
                try
                {
                    var prevListMsg = await ctx.ResultChannel.GetMessageAsync(
                        ctx.State.PinnedListMessageID);
                    if (prevListMsg != null)
                        alreadySetup = true;
                }
                catch { }

                if (alreadySetup)
                {
                    await ctx.CommandChannel.SendMessageAsync(
                        $"Initiative already setup for channel {ctx.ResultChannel.Name}");
                    return;
                }

                ctx.State.PinnedListMessageID = 0;
            }

            // Sanity check: Is there a pinned message up there from us?
            // If so, it needs to be cleaned up!
            // TODO: If, in the future, this bot is ever extended to other pinned messages, we'll
            // need to get pickier about how we do this.
            var pinnedMsgs = await ctx.ResultChannel.GetPinnedMessagesAsync();
            var ourMsgs = pinnedMsgs.Where(x => x.Author.Id == Context.Client.CurrentUser.Id);
            if ( ourMsgs.Any() )
            {
                foreach (var m in ourMsgs ) { await m.DeleteAsync(); }
            }

            var listMsg = await ctx.ResultChannel.SendMessageAsync(await GetListText(ctx));
            ctx.State.PinnedListMessageID = listMsg.Id;
            await listMsg.PinAsync();

            if (ctx.IsControlled)
                await ctx.CommandChannel.SendMessageAsync($"Initiative setup for channel {ctx.ResultChannel.Name}");
            else
                await Context.Message.DeleteAsync();

            await StorageService.StoreInitiative(ctx.ResultChannelId, ctx.State);
        }

        [Command("teardown_initiative")]
        [Alias("teardown", "teardowninit")]
        public async Task TeardownInitiativeAsync(IChannel channel = null)
        {
            channel ??= Context.Channel;

            var textChannel = channel as ITextChannel;
            if (textChannel == null)
            {
                await Context.Channel.SendMessageAsync(
                    "Error: Target channel must be a text-based channel.");
                return;
            }

            var ctx = await GetInitContext(channel.Id);

            // Sanity check: Is there a pinned message up there from us?
            // If so, it needs to be cleaned up!
            // TODO: If, in the future, this bot is ever extended to other pinned messages, we'll
            // need to get pickier about how we do this.
            var pinnedMsgs = await ctx.ResultChannel.GetPinnedMessagesAsync();
            var ourMsgs = pinnedMsgs.Where(x => x.Author.Id == Context.Client.CurrentUser.Id);
            if (ourMsgs.Any())
            {
                foreach (var m in ourMsgs) { await m.DeleteAsync(); }
            }

            ctx.State.PinnedListMessageID = 0;

            if ( ctx.State.LastAnnounceMessageID != 0 )
            {
                var poseMsg = await ctx.ResultChannel.GetMessageAsync(ctx.State.LastAnnounceMessageID);
                poseMsg?.DeleteAsync();
            }

            ctx.State.LastAnnounceMessageID = 0;

            ctx.State.CurrentInitiative     = 0;

            ctx.State.InInitiative          = false;

            ctx.State.Characters.Clear();

            if (ctx.IsControlled)
                await ctx.CommandChannel.SendMessageAsync($"Initiative teardown for channel {ctx.ResultChannel.Name} complete.");
            else
                await Context.Message.DeleteAsync();

            await StorageService.StoreInitiative(ctx.ResultChannelId, ctx.State);
        }

        [Command("set_initiative")]
        [Alias("set","setinit")]
        public async Task SetInitiativeAsync(IUser user, string charName, double init, IChannel channel = null)
        {
            channel ??= Context.Channel;
            var textChannel = channel as ITextChannel;
            if (textChannel == null)
            {
                await Context.Channel.SendMessageAsync(
                    "Error: Target channel must be a text-based channel.");
                return;
            }
            var ctx = await GetInitContext(channel.Id);

            var match = ctx.State.Characters.Find(
                x => x.PlayerID == user.Id &&
                     x.DisplayName == charName);

            var msg = "";
            if (match != null)
            {
                match.Initiative = init;
                msg = $"Set {match.DisplayName} initiative to {match.Initiative}";
            }
            else
            {
                ctx.State.Characters.Add(
                    new InitiativeEntry
                    {
                        DisplayName = charName,
                        PlayerID    = user.Id,
                        Initiative  = init
                    });
                msg = $"Added {charName} with initiative {init}";
            }

            ctx.State.Sort();

            if (ctx.IsControlled)
                await ctx.CommandChannel.SendMessageAsync(msg);
            else
                await Context.Message.DeleteAsync();

            if (ctx.State.PinnedListMessageID != 0)
            {
                var pinnedMsg = await ctx.ResultChannel.GetMessageAsync(ctx.State.PinnedListMessageID) as IUserMessage;
                await pinnedMsg.ModifyAsync(async x => x.Content = await GetListText(ctx));
            }

            var oldInit = ctx.State.CurrentInitiative;
            var newInitChar = ctx.State.GetCurrent();
            if (newInitChar.Initiative != oldInit && ctx.State.InInitiative)
            {
                ctx.State.Advance();
                if (ctx.State.LastAnnounceMessageID != 0)
                {
                    try
                    {
                        var poseMsg = (await ctx.ResultChannel.GetMessageAsync(
                            ctx.State.LastAnnounceMessageID));
                        poseMsg?.DeleteAsync();
                        ctx.State.LastAnnounceMessageID = 0;
                    }
                    catch { }
                }

                var postMsg = await ctx.ResultChannel.SendMessageAsync(await GetCurrentPoseText(ctx));
                ctx.State.LastAnnounceMessageID = postMsg.Id;
            }

            await StorageService.StoreInitiative(ctx.ResultChannelId, ctx.State);
        }

        [Command("delete_initiative")]
        [Alias("del", "delinit")]
        public async Task DeleteInitiativeAsync(IUser user, string charName, IChannel channel = null)
        {
            channel ??= Context.Channel;
            var textChannel = channel as ITextChannel;
            if (textChannel == null)
            {
                await Context.Channel.SendMessageAsync(
                    "Error: Target channel must be a text-based channel.");
                return;
            }
            var ctx = await GetInitContext(channel.Id);

            var match = ctx.State.Characters.Find(
                x => x.PlayerID == user.Id &&
                     x.DisplayName == charName);

            var msg = "";
            if (match != null)
            {
                ctx.State.Characters.Remove(match);
                var playerName = await GetUserLocalName(ctx.ResultChannel.Guild, match.PlayerID);
                msg = $"Deleted character {match.DisplayName} with player {playerName}.";
            }
            else
            {
                var playerName = await GetUserLocalName(ctx.ResultChannel.Guild, user.Id);
                msg = $"Could not find character {charName} by player {playerName}.";
            }

            ctx.State.Sort();

            if (ctx.State.PinnedListMessageID != 0)
            {
                var pinnedMsg = await ctx.ResultChannel.GetMessageAsync(ctx.State.PinnedListMessageID) as IUserMessage;
                await pinnedMsg?.ModifyAsync(async x => x.Content = await GetListText(ctx));
            }

            if (ctx.IsControlled)
                await ctx.CommandChannel.SendMessageAsync(msg);
            else
                await Context.Message.DeleteAsync();

            var oldInit = ctx.State.CurrentInitiative;
            var newInitChar = ctx.State.GetCurrent();
            if (newInitChar.Initiative != oldInit && ctx.State.InInitiative)
            {
                ctx.State.Advance();
                if (ctx.State.LastAnnounceMessageID != 0)
                {
                    try
                    {
                        var poseMsg = (await ctx.ResultChannel.GetMessageAsync(
                            ctx.State.LastAnnounceMessageID));
                        poseMsg?.DeleteAsync();
                        ctx.State.LastAnnounceMessageID = 0;
                    }
                    catch { }
                }

                var postMsg = await ctx.ResultChannel.SendMessageAsync(await GetCurrentPoseText(ctx));
                ctx.State.LastAnnounceMessageID = postMsg.Id;
            }

            await StorageService.StoreInitiative(ctx.ResultChannelId, ctx.State);
        }

        [Command("list_initiative")]
        [Alias("list", "listinit")]
        public async Task ListInitiativeAsync(IChannel channel = null)
        {
            channel ??= Context.Channel;
            var textChannel = channel as ITextChannel;
            if (textChannel == null)
            {
                await Context.Channel.SendMessageAsync(
                    "Error: Target channel must be a text-based channel.");
                return;
            }
            var ctx = await GetInitContext(channel.Id);

            if (!ctx.IsControlled)
                await Context.Message.DeleteAsync();

            // Quirk: Since initiative maintains a list pinned to each channel,
            // we will assume the output of this command will go back to the
            // channel where the command was executed, *even if it is controlling
            // another channel!*
            await Context.Channel.SendMessageAsync(await GetListText(ctx));
        }

        [Command("start_initiative")]
        [Alias("start", "startinit")]
        public async Task StartInitiativeAsync(IChannel channel = null)
        {
            channel ??= Context.Channel;
            var textChannel = channel as ITextChannel;
            if (textChannel == null)
            {
                await Context.Channel.SendMessageAsync(
                    "Error: Target channel must be a text-based channel.");
                return;
            }
            var ctx = await GetInitContext(channel.Id);

            if (!ctx.State.InInitiative)
            {
                if (ctx.IsControlled)
                    await ctx.CommandChannel.SendMessageAsync(
                        $"Started initiative for channel {ctx.ResultChannel.Name}.");
                else
                    await Context.Message.DeleteAsync();

                ctx.State.InInitiative = true;

                var oldInit = ctx.State.CurrentInitiative;
                var newInitChar = ctx.State.GetCurrent();
                if (newInitChar.Initiative != oldInit)
                {
                    ctx.State.Advance();
                }

                if (ctx.State.LastAnnounceMessageID != 0)
                {
                    try
                    {
                        var poseMsg = (await ctx.ResultChannel.GetMessageAsync(
                            ctx.State.LastAnnounceMessageID));
                        poseMsg?.DeleteAsync();
                        ctx.State.LastAnnounceMessageID = 0;
                    }
                    catch { }
                }

                var postMsg = await ctx.ResultChannel.SendMessageAsync(await GetCurrentPoseText(ctx));
                ctx.State.LastAnnounceMessageID = postMsg.Id;

                await StorageService.StoreInitiative(ctx.ResultChannelId, ctx.State);
            }
            else
            {
                if (ctx.IsControlled)
                    await ctx.CommandChannel.SendMessageAsync(
                        $"Already in initiative for channel {ctx.ResultChannel.Name}.");
                else
                    await Context.Message.DeleteAsync();
            }
        }

        [Command("stop_initiative")]
        [Alias("stop", "stopinit")]
        public async Task StopInitiativeAsync(IChannel channel = null)
        {
            channel ??= Context.Channel;
            var textChannel = channel as ITextChannel;
            if (textChannel == null)
            {
                await Context.Channel.SendMessageAsync(
                    "Error: Target channel must be a text-based channel.");
                return;
            }
            var ctx = await GetInitContext(channel.Id);

            if (ctx.IsControlled)
                await ctx.CommandChannel.SendMessageAsync(
                    $"Stopped initiative for channel {ctx.ResultChannel.Name}.");
            else
                await Context.Message.DeleteAsync();

            ctx.State.InInitiative = false;

            if (ctx.State.LastAnnounceMessageID != 0)
            {
                try
                {
                    var poseMsg = (await ctx.ResultChannel.GetMessageAsync(
                        ctx.State.LastAnnounceMessageID));
                    poseMsg?.DeleteAsync();
                    ctx.State.LastAnnounceMessageID = 0;
                }
                catch { }
            }

            await StorageService.StoreInitiative(ctx.ResultChannelId, ctx.State);
        }

        [Command("jump_to_initiative")]
        [Alias("jumpto", "jumptoinit")]
        public async Task JumpToInitiativeAsync(double init, IChannel channel = null)
        {
            channel ??= Context.Channel;
            var textChannel = channel as ITextChannel;
            if (textChannel == null)
            {
                await Context.Channel.SendMessageAsync(
                    "Error: Target channel must be a text-based channel.");
                return;
            }
            var ctx = await GetInitContext(channel.Id);

            if (ctx.IsControlled)
                await ctx.CommandChannel.SendMessageAsync(
                    $"Jumped to initiative {init} for channel {ctx.ResultChannel.Name}.");
            else
                await Context.Message.DeleteAsync();

            ctx.State.CurrentInitiative = init;

            if ( ctx.State.InInitiative )
            {
                var newInitChar = ctx.State.GetCurrent();
                ctx.State.CurrentInitiative = newInitChar.Initiative;

                if (ctx.State.LastAnnounceMessageID != 0)
                {
                    try
                    {
                        var poseMsg = (await ctx.ResultChannel.GetMessageAsync(
                            ctx.State.LastAnnounceMessageID));
                        poseMsg?.DeleteAsync();
                        ctx.State.LastAnnounceMessageID = 0;
                    }
                    catch { }
                }

                var postMsg = await ctx.ResultChannel.SendMessageAsync(await GetCurrentPoseText(ctx));
                ctx.State.LastAnnounceMessageID = postMsg.Id;
            }

            await StorageService.StoreInitiative(ctx.ResultChannelId, ctx.State);
        }

        [Command("done_initiative")]
        [Alias("done","skip")]
        public async Task DoneAsync(IChannel channel = null)
        {
            channel ??= Context.Channel;
            var textChannel = channel as ITextChannel;
            if (textChannel == null)
            {
                await Context.Channel.SendMessageAsync(
                    "Error: Target channel must be a text-based channel.");
                return;
            }
            var ctx = await GetInitContext(channel.Id);

            if (!ctx.IsControlled)
                await Context.Message.DeleteAsync();

            var currChar = ctx.State.GetCurrent();
            if (currChar.PlayerID != Context.User.Id)
            {
                var playerName = await GetUserLocalName(ctx.CommandChannel.Guild,Context.User);
                if (ctx.IsControlled)
                    await ctx.CommandChannel.SendMessageAsync(
                        $"Error: Not {playerName}'s turn!");
                return;
            }

            if (ctx.State.InInitiative)
            {
                currChar = ctx.State.Advance();
 
                if (ctx.State.LastAnnounceMessageID != 0)
                {
                    try
                    {
                        var poseMsg = (await ctx.ResultChannel.GetMessageAsync(
                            ctx.State.LastAnnounceMessageID));
                        poseMsg?.DeleteAsync();
                        ctx.State.LastAnnounceMessageID = 0;
                    }
                    catch { }
                }

                var postMsg = await ctx.ResultChannel.SendMessageAsync(await GetCurrentPoseText(ctx));
                ctx.State.LastAnnounceMessageID = postMsg.Id;
            }

            await StorageService.StoreInitiative(ctx.ResultChannelId, ctx.State);
        }

        [Command("help_initiative")]
        [Alias("help", "helpinit")]
        public async Task HelpInitiativeAsync()
        {
            var sb = new StringBuilder();
            var prefix = await StorageService.GetServerPrefix(
                Context.Guild.Id) ?? "/";

            sb.Append("\u200B\n");
            sb.Append("```");

            foreach (var s in new List<string> { 
                "setcontrol<channel>",
                "clearcontrol",
                "",
                "setup[channel]",
                "teardown[channel]",
                "",
                "set<player> < character > < init > [channel]",
                "del<player> < character > [channel]",
                "list[channel]",
                "",
                "start[channel]",
                "stop[channel]",
                "jumpto<init>[channel]",
                "done[channel]",
                "",
                "help",
                "setprefix",
                "clearprefix",
            })
            {
                if (s != "")
                    sb.Append(prefix);
                sb.Append(s);
                sb.Append("\n");
            }
            sb.Append("```");

            await Context.Channel.SendMessageAsync(sb.ToString());
        }

        [Command("set_initiative_prefix")]
        [Alias("setprefix", "setinitprefix")]
        public async Task SetInitiativePrefixAsync(string prefix)
        {
            await StorageService.SetServerPrefix(
                Context.Guild.Id, prefix);
            await Context.Channel.SendMessageAsync(
                $"Prefix updated to '{prefix}'.");
        }

        [Command("clear_initiative_prefix")]
        [Alias("clearprefix", "clearinitprefix")]
        public async Task ClearInitiativePrefixAsync()
        {
            await StorageService.SetServerPrefix(
                Context.Guild.Id, "");
            await Context.Channel.SendMessageAsync(
                $"Prefix disabled.  Bot will respond only direct mentions.  (Use setprefix to re-enable!)");
        }

        private async Task<string> GetListText(InitContext ctx)
        {
            var sb = new StringBuilder();
            sb.Append("\u200B\n");
            //sb.Append("```");

            foreach (var item in ctx.State.Characters)
            {
                var playerName = await GetUserLocalName(ctx.ResultChannel.Guild,item.PlayerID);

                if (item.Initiative == ctx.State.CurrentInitiative)
                { sb.Append("**"); }

                sb.Append(string.Format(
                    "[{0}] {1} ({2})",
                    item.Initiative,
                    item.DisplayName,
                    playerName));

                if (item.Initiative == ctx.State.CurrentInitiative)
                { sb.Append("**"); }

                sb.Append("\n");
            }
            //sb.Append("```");

            return sb.ToString();
        }

        private async Task<string> GetCurrentPoseText(InitContext ctx)
        {
            var currInit = ctx.State.GetCurrent();
            var playerName = Context.Guild.GetUser(currInit.PlayerID).Mention;
            return $"Next Pose: {currInit.DisplayName} ({playerName})";
        }

        private async Task<string> GetUserLocalName(IGuild guild, ulong userid)
        {
            var guser = await guild.GetUserAsync(userid);
            return guser?.Nickname ?? guser.Username;
        }

        private async Task<string> GetUserLocalName(IGuild guild, IUser user)
            => await GetUserLocalName(guild, user.Id);
    }
}
