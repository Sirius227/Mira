using Discord;
using Discord.Commands;
using Mira.Handlers;

namespace Mira.Modules
{
    [Name("Moderation Commands")]
    public class ModerationModule : ModuleBase<SocketCommandContext>
    {
        [Command("Purge")]
        [Summary("Deletes the number of messages you specified.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Purge(int amount = 0)
        {
            if (amount <= 0)
            {
                await ReplyAsync(embed: await EmbedHandler.CreateErrorEmbed($"Invalid Usage :x:", "Value cannot be zero"));
                return;
            }

            if (amount > 95)
            {
                await ReplyAsync(embed: await EmbedHandler.CreateErrorEmbed(null!, "You can delete up to 95 messages"));
                return;
            }

            IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync();
            await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messages);
            const int delay = 3000;

            IUserMessage m = await ReplyAsync($"**I deleted {messages.Count() - 1} messages :ok_hand:**");

            await Task.Delay(delay);
            await m.DeleteAsync();
        }
    }
}
