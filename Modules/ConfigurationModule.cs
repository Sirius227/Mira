using Discord;
using Discord.Commands;
using Mira.Handlers;
using Discord.WebSocket;
using Mira.Managers;

namespace Mira.Modules
{
    [Name("Configuration Commands")]
    public class ConfigurationModule : ModuleBase<SocketCommandContext>
    {
        [Command("Default Prefix")]
        [Alias("Dprefix")]
        [Summary("Makes the prefix default.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task DefaultPrefix()
        {
            BotManager.UpdatePrefix(Context.Guild.Id.ToString(), ".");
            await ReplyAsync("**Prefix set to** `.` 👍");
        }

        string? temp, pre;

        [Command("Prefix")]
        [Summary("You can change the prefix.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Prefix([Remainder] string prefix = null!)
        {
            if (prefix == null)
            {
                await ReplyAsync(embed: await EmbedHandler.CreateErrorEmbed($"Invalid Usage :x:", "Example: .prefix !"));
                return;
            }

            if (prefix.Length >= 5)
            {
                await ReplyAsync($"**The length of the new prefix is too long!** :x:\n`Must be a maximum of 5 characters long.`");
                return;
            }

            temp = "";
            pre = prefix;

            for (int i = 0; i < prefix.Length; i++)
            {
                if (prefix[i] == '\'')
                {
                    temp += "'" + prefix[i];
                    continue;
                }
                if (prefix[i] == '\"')
                {
                    temp += "\"" + prefix[i];
                    continue;
                }                

                temp += prefix[i];
            }
            prefix = temp;

            BotManager.UpdatePrefix(Context.Guild.Id.ToString(), prefix);
            
            await ReplyAsync("**Prefix set to** " + $"`{pre}` 👍");
        }

        [Command("Set dj role")]
        [Alias("djrole", "dj role", "setdjrole")]
        [Summary("You determine the role of the DJ.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task SetDjRole(SocketRole role)
        {
            BotManager.SetDjRole(Context.Guild.Id.ToString(), role);
            await ReplyAsync($"**Dj role set to** {role.Mention} 👍");
        }

        [Command("Dj role remove")]
        [Alias("rdjrole", "rdj role", "dj role r", "removedjrole")]
        [Summary("You remove the dj role.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task RemoveDjRole()
        {
            BotManager.DeleteDjRole(Context.Guild.Id.ToString());
            await ReplyAsync("**Removed Dj role** 👍");
        }
    }
}
