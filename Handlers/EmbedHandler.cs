using Discord;
using Discord.WebSocket;
using System.Globalization;
using System.Text;

namespace Mira.Handlers
{
    public static class EmbedHandler
    {

        public static async Task<Embed> CreateBasicEmbed(string title, string description, SocketGuildUser user)
        {
            Color color = SetColor();

            var embed = await Task.Run(() => new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(color)
                .WithFooter(text: $"Request by {user.Username}#{user.Discriminator}",
                            iconUrl: user.GetAvatarUrl()).Build());
            return embed;
        }

        public static async Task<Embed> CreateBasicEmbed(string title, string description)
        {
            Color color = SetColor();

            var embed = await Task.Run(() => new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(color).Build());
            return embed;
        }

        public static async Task<Embed> CreateLyricsEmbed(string title, string description, SocketGuildUser user, string thumbnailurl)
        {
            Color color = SetColor();

            var embed = await Task.Run(() => new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(color)
                .WithThumbnailUrl(thumbnailurl)
                .WithFooter("Request by " + user.Username + "#" + user.Discriminator, user.GetAvatarUrl()).Build());
            return embed;
        }

        public static async Task<Embed> CreateErrorEmbed(string source, string error)
        {
            var embed = await Task.Run(() => new EmbedBuilder()
                .WithTitle($"{source}")
                .WithDescription($"{error}")
                .WithColor(Color.Red).Build());
            return embed;
        }

        static readonly Random n = new();

        public static async Task<Embed> CreateUserEmbed(SocketGuildUser user)
        {
            var roles = new StringBuilder();

            foreach (var socketRole in user.Roles)
            {
                if (socketRole.Name != "@everyone")
                    roles.Append($"{socketRole.Mention}\n");
            }

            var role = roles.ToString();
            role = role == "" ? "`None`" : role;

            string status = user.Status switch
            {
                UserStatus.Offline => "Offline",
                UserStatus.Online => "Online",
                UserStatus.Idle => "Idle",
                UserStatus.AFK => "AFK",
                UserStatus.DoNotDisturb => "Do Not Disturb",
                UserStatus.Invisible => "Invisible",
                _ => "`None`",
            };

            Color color = SetColor();

            string activity = null!;
            string type = "";

            if (user.Activities.Count == 0)
                activity = "None";
            else
            {
                if (user.Activities.First().Type is ActivityType.CustomStatus)
                    status += user.Activities.First().ToString();

                var actv = user.Activities.FirstOrDefault(x => x.Type is not ActivityType.CustomStatus);

                if (actv != null)
                {
                    activity = actv.ToString()!;
                    type = actv.Type.ToString();
                }
            }

            List<EmbedFieldBuilder> fields = new()
            {
                new EmbedFieldBuilder
                {
                    Name = "Joined Discord",
                    Value = $"`{user.CreatedAt.DateTime.ToString("dd MMM yyyy HH:mm", CultureInfo.CreateSpecificCulture("en-US"))}`",
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "Joined Server",
                    Value = $"`{user.JoinedAt!.Value.DateTime.ToString("dd MMM yyyy HH:mm", CultureInfo.CreateSpecificCulture("en-US"))}`",
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "Activity " + type,
                    Value = $"`{activity.Trim()}`",
                    IsInline = false
                },
                new EmbedFieldBuilder
                {
                    Name = "Roles",
                    Value = role,
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "Status",
                    Value = $"`{status}`",
                    IsInline = true
                }
            };

            var embed = await Task.Run(() => new EmbedBuilder
            {
                Fields = fields,
                ThumbnailUrl = user.GetAvatarUrl(ImageFormat.Auto, 512),
                Footer = new EmbedFooterBuilder { Text = $"{user.Username}#{user.Discriminator}", IconUrl = user.GetAvatarUrl() },
                Color = color
            });

            return embed.Build();
        }

        public static Color SetColor()
        {
            int r = n.Next(0, 256);
            int g = n.Next(0, 256);
            int b = n.Next(0, 256);

            return new Color(r, g, b);

        }
    }
}
