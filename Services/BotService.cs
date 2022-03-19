using Discord;
using Discord.Commands;
using System.Globalization;
using Mira.Managers;
using Mira.Handlers;

namespace Mira.Services
{
    public sealed class BotService
    {
        public static async Task<Embed> DisplayInfoAsync(SocketCommandContext context)
        {
            string prefix = BotManager.GetPrefix(context.Guild.Id.ToString());

            List<EmbedFieldBuilder> fields = new()
            {
                new EmbedFieldBuilder
                {
                    Name = "Owner",
                    Value = context.Guild.Owner.Mention,
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "📆 Creation Date",
                    Value = $"`{context.Guild.CreatedAt.DateTime.ToString("dd MMM yyyy HH:mm", CultureInfo.CreateSpecificCulture("en-US"))}`",
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "❗ Prefix",
                    Value = $"`{prefix}`",
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "🆔 Server ID",
                    Value = $"`{context.Guild.Id}`",
                    IsInline = false
                },
                new EmbedFieldBuilder
                {
                    Name = $"👥 Users [{context.Guild.Users.Count(x => !x.IsBot)}]",
                    Value = $"Online People: {context.Guild.Users.Count(x => x.Status is not UserStatus.Offline && x.Status is not UserStatus.Invisible && !x.IsBot)}\n" +
                    $"Current Bots: {context.Guild.Users.Count(x => x.IsBot)}\n" +
                    $"✨ Boosts: {context.Guild.PremiumSubscriptionCount}\n" +
                    $"🎚️ Boost level: {context.Guild.PremiumTier}",
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "🔊 / 💬 Channels",
                    Value = $"Text: { context.Guild.TextChannels.Count}\n" +
                    $"Voice: {context.Guild.VoiceChannels.Count}",
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = $":closed_lock_with_key: Roles ({context.Guild.Roles.Count}) ",
                    Value = $"For the list of roles: {prefix}roles",
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "🌏 Others",
                    Value = $"Region: {context.Guild.VoiceRegionId[..1].ToUpper()}" + $"{context.Guild.VoiceRegionId[1..]}\n" +
                    $"Verification level: {context.Guild.VerificationLevel}",
                    IsInline = true
                },
            };

            var author = await Task.Run(() => new EmbedAuthorBuilder
            {
                Name = context.Guild.Name,
                IconUrl = context.Guild.IconUrl
            });

            var embed = await Task.Run(() => new EmbedBuilder
            {
                ThumbnailUrl = context.Guild.IconUrl,
                Author = author,
                Color = EmbedHandler.SetColor(),
                Footer = new EmbedFooterBuilder { Text = "Developed by <@321223711271288834> | Discord.Net & Victoria", IconUrl = context.Client.CurrentUser.GetAvatarUrl() },
                Fields = fields
            });

            return embed.Build();
        }
    }
}
