using Discord;
using Discord.Commands;
using Discord.Webhook;
using Discord.WebSocket;
using Mira.Managers;
using System.Text;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using System.Globalization;
using Mira.Handlers;
using Mira.Modules;
using Mira.Services;

namespace DiscordMusicBot.Modules
{
    [Name("Member Commands")]
    public class MemberModule : ModuleBase<SocketCommandContext>
    {
        private static readonly ulong id = GlobalData.Config!.WebhookId;
        private static readonly string token = GlobalData.Config.WebhookToken!;

        private readonly DiscordWebhookClient client = new(id, token);

        public InteractiveService Interactive { get; set; }
        private readonly CommandService service;

        public MemberModule(CommandService _service, InteractiveService interactive)
        {
            service = _service;
            Interactive = interactive;
        }
            

        [Command("help")]
        [Alias("h")]
        [Summary("Shows help menu.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task Help([Remainder] string command = null!)
        {
            var botPrefix = BotManager.GetPrefix(Context.Guild.Id.ToString());
            Embed helpEmbed = service.GetDefaultHelpEmbed(command, botPrefix, Context.User, Context.Guild.IconUrl);
            await ReplyAsync(embed: helpEmbed);
        }       

        [Command("Report")]
        [Alias("rep")]
        [Summary("You can let us know when you encounter any errors.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task ReportAsync([Remainder] string description = null!)
        {
            if (description is null)
            {
                await ReplyAsync("`Please write a description` ❌");
                return;
            }

            try
            {
                List<EmbedFieldBuilder> fields = new()
                {
                    new EmbedFieldBuilder
                    {
                        Name = "❓ Hangi sunucudan rapor edildi?",
                        Value = Context.Guild.Name + " | " + $"`{Context.Guild.Id}`",
                        IsInline = false
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "🆔 Rapor eden kişinin ID'si",
                        Value = $"`{Context.User.Id}`",
                        IsInline = false
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "💬 Rapor Açıklaması",
                        Value = description,
                        IsInline = false
                    }
                };

                var embeds = new List<Embed>
                {
                    new EmbedBuilder
                    {
                        Author = new EmbedAuthorBuilder {Name = Context.User.Username + "#" + Context.User.Discriminator, IconUrl = Context.User.GetAvatarUrl()},
                        Fields = fields,
                        ThumbnailUrl = Context.User.GetAvatarUrl(ImageFormat.Auto, 512),
                        Color = EmbedHandler.SetColor(),
                    }.Build()
                };

                await client.SendMessageAsync(embeds: embeds);
                await Interactive.DelayedDeleteMessageAsync(await ReplyAsync("Your report has reached us. Thank you"), TimeSpan.FromSeconds(15));             
            }
            catch (Exception ex)
            {
                await LoggingService.LogCriticalAsync(ex.Source!, ex.Message, ex);
            }
        }

        [Command("Ping")]
        [Alias("Latency")]
        [Summary("Checks the bot's response time to Discord.")]
        public async Task Ping()
            => await ReplyAsync($"🏓 **Pong** `{Context.Client.Latency}` ms ⏱️ {Context.User.Mention}");

        [Command("User")]
        [Alias("user info", "member", "minfo", "member info")]
        [Summary("Shows the user info.")]
        public async Task User([Remainder] SocketGuildUser user = null!)
        {
            user ??= (Context.User as SocketGuildUser)!;

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
                UserStatus.Offline => "⚫",
                UserStatus.Online => "🟢",
                UserStatus.Idle => "🟠",
                UserStatus.AFK => "🟠",
                UserStatus.DoNotDisturb => "⛔",
                UserStatus.Invisible => "⚫",
                _ => "`None`",
            };

            Color color = EmbedHandler.SetColor();

            string activity = "None";
            string type = "";

            if (user.Activities.Count == 0)
                activity = "None";
            else
            {
                if (user.Activities.FirstOrDefault(x => x.Type == ActivityType.CustomStatus) is not null)
                    status += $"`{user.Activities.First(x => x.Type == ActivityType.CustomStatus)}`";

                if (user.Activities.Any(x => x.Type is not ActivityType.CustomStatus))
                {
                    var actv = user.Activities.FirstOrDefault(x => x.Type is not ActivityType.CustomStatus);
                    activity = actv?.ToString()!;
                    type = actv?.Type.ToString()!;
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
                    Value = $"{status}",
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

            await ReplyAsync(embed: embed.Build());

        }

        [Command("Roles")]
        [Summary("Shows the list of roles.")]
        public async Task Roles()
        {
            List<string> rolePages = new();
            var roles = new StringBuilder();
            var guild = Context.Guild;
            int roleNum = 1;

            foreach (var role in guild.Roles)
            {
                roles.Append($"{role.Mention} {role.Members.Count()} members\n\n");

                if (roleNum % 15 == 0)
                {
                    rolePages.Add(roles.ToString());
                    roles.Clear();
                }

                roleNum++;
            }

            if (roles.Length != 0)
                rolePages.Add(roles.ToString());

            if (rolePages.Count == 1)
            {
                await ReplyAsync(embed: await EmbedHandler.CreateBasicEmbed($"List of the roles [`{guild.Roles.Count}`]", $"{roles}"));                    
                return; 
            }

            var pages = new PageBuilder[rolePages.Count];

            for (int i = 0; i < pages.Length; i++)
            {
                pages[i] = new PageBuilder().WithAuthor($"{Context.User.Username}#{Context.User.Discriminator}", Context.User.GetAvatarUrl()).WithTitle("List of the roles")
                        .WithDescription(rolePages[i])
                        .WithFooter($"page {i + 1}/{pages.Length}");
            }

            var paginator = new StaticPaginatorBuilder()
                .WithUsers(Context.User)
                .WithPages(pages)
                .WithFooter(PaginatorFooter.None)
                .Build();

            await Interactive.SendPaginatorAsync(paginator, Context.Channel, TimeSpan.FromMinutes(2));
        }

        [Command("Info")]
        [Alias("server", "sinfo", "server info")]
        [Summary("Shows the server info.")]
        public async Task Info()
            => await ReplyAsync(embed: await BotService.DisplayInfoAsync(Context));

        [Command("Avatar")]
        [Summary("Shows the avatar of the user")]
        public async Task GetAvatarAsync([Remainder] SocketUser user = null!)
        {
            if (user == null)
                user = Context.User;

            var author = new EmbedAuthorBuilder
            {
                Name = user.Username + "#" + user.Discriminator,
                IconUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl()
            };

            ushort a = 16;
            var avatars = new StringBuilder();

            for (int i = 0; i < 8; i++)
            {
                avatars.Append($" | [{a}]({user.GetAvatarUrl(ImageFormat.Auto, a) ?? user.GetDefaultAvatarUrl()})");
                a *= 2;
            }

            var png = user.GetAvatarUrl(ImageFormat.Png, 512) ?? user.GetDefaultAvatarUrl();
            var jpeg = user.GetAvatarUrl(ImageFormat.Jpeg, 512) ?? user.GetDefaultAvatarUrl();
            var webp = user.GetAvatarUrl(ImageFormat.WebP, 512) ?? user.GetDefaultAvatarUrl();
            var auto = user.GetAvatarUrl(ImageFormat.Auto, 512) ?? user.GetDefaultAvatarUrl();

            var embed = new EmbedBuilder
            {
                Author = author,
                Description = $"[png]({png}) | [jpeg]({jpeg}) | [webp]({webp})\n{avatars.ToString()[3..]}",
                ImageUrl = auto,
                Color = EmbedHandler.SetColor()
            };

            await ReplyAsync(embed: embed.Build());
        }

        [Command("Invite")]
        [Summary("You can invite the bot to your own server.")]
        public async Task Invite()
        {
            string inviteString = "[Click to come](INVITE URL)";

            var embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder { IconUrl = Context.Guild.CurrentUser.GetAvatarUrl(), Name = Context.Guild.CurrentUser.Username },
                Fields = {new EmbedFieldBuilder { Name = "📩 Invite Mira", Value = inviteString, IsInline = false},
                    new EmbedFieldBuilder { Name = "🎁 Support Server", Value = "[Join us](DISCORD SERVER INVITE URL)" } }
            }.Build();

            await ReplyAsync(embed: embed);
        }
    }
}
