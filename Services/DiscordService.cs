using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Fergun.Interactive;
using Microsoft.Extensions.DependencyInjection;
using Mira.Handlers;
using Mira.Managers;
using Victoria;

namespace Mira.Services
{
    public class DiscordService
    {
        public readonly DiscordSocketClient _client;
        private readonly CommandHandler _commandHandler;
        private readonly ServiceProvider _services;
        private readonly LavaNode _lavaNode;
        private readonly LavaLinkAudio _audioService;
        private readonly BotManager _botManager;

        public DiscordService()
        {
            _services = ConfigureServices();
            _client = _services.GetRequiredService<DiscordSocketClient>();
            _commandHandler = _services.GetRequiredService<CommandHandler>();
            _lavaNode = _services.GetRequiredService<LavaNode>();
            _audioService = _services.GetRequiredService<LavaLinkAudio>();
            _botManager = new();

            SubscribeLavaLinkEvents();
            SubscribeDiscordEvents();
        }

        public async Task InitializeAsync()
        {
            await InitializeGlobalDataAsync();

            await _client.LoginAsync(TokenType.Bot, GlobalData.Config?.DiscordToken);
            await _client.StartAsync();

            await _commandHandler.InitializeAsync();

            await Task.Delay(-1);
        }

        private void SubscribeLavaLinkEvents()
        {
            _lavaNode.OnLog += LogAsync;
            _lavaNode.OnTrackException += LavaLinkAudio.TrackException;
            _lavaNode.OnTrackStuck += LavaLinkAudio.TrackStuck;
            _lavaNode.OnTrackEnded += _audioService.TrackEnded;
            _lavaNode.OnTrackStarted += _audioService.TrackStarted;
        }

        private void SubscribeDiscordEvents()
        {
            _client.Ready += ReadyAsync;
            _client.Log += LogAsync;
            _client.SelectMenuExecuted += _audioService.SelectedMenu;
            _client.MessageReceived += LavaLinkAudio.MessageUpdate;
            _client.JoinedGuild += OnJoinedGuild;
            _client.LeftGuild += OnLeftGuild;
            _client.RoleDeleted += OnRoleDeleted;
            _client.ReactionAdded += _audioService.ReactionPlayerControlAsync;
        }

        private async Task OnJoinedGuild(SocketGuild guild)
        {
            BotManager.InsertVariable(guild.Id.ToString());

            var channel = guild.DefaultChannel;
            await channel.SendMessageAsync("**Hello there! Thanks for inviting me** :comet:\n" +
                $"**This is my prefix:** `.`");

            totalGuild++;
            await _client.SetGameAsync(GlobalData.Config?.GameStatus + $" | {totalGuild} Servers!", type: ActivityType.Listening);

        }

        private async Task OnLeftGuild(SocketGuild guild)
        {
            BotManager.DeleteVariable(guild.Id.ToString());
            totalGuild--;
            await _client.SetGameAsync(GlobalData.Config?.GameStatus + $" | {totalGuild} Servers!", type: ActivityType.Listening);

        }

        private Task OnRoleDeleted(SocketRole role)
        {
            if (BotManager.DjRole(role.Guild.Id.ToString()))
                BotManager.DeleteDjRole(role.Guild.Id.ToString());

            return Task.CompletedTask;
        }

        private static async Task InitializeGlobalDataAsync()
        {
            await GlobalData.InitializeAsync();
            GlobalData.StartLavalink();
        }

        int totalGuild = 0;

        private async Task ReadyAsync()
        {
            try
            {
                await _lavaNode.ConnectAsync();
                await _client.SetGameAsync(GlobalData.Config?.GameStatus + $" | {_client.Guilds.Count} Servers!", type: ActivityType.Listening);

                foreach (var item in _client.Guilds)
                {
                    BotManager.InsertVariable(item.Id.ToString());
                }
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync(ex.Source!, ex.Message);
            }
        }

        private async Task LogAsync(LogMessage log)
        {
            await LoggingService.LogAsync(log.Source, log.Severity, log.Message, log.Exception);
        }

        private static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(new DiscordSocketConfig { GatewayIntents = GatewayIntents.All })
                .AddSingleton<CommandService>()
                .AddSingleton(new CommandServiceConfig { DefaultRunMode = RunMode.Async })
                .AddSingleton<CommandHandler>()
                .AddSingleton<LavaNode>()
                .AddSingleton(new LavaConfig())
                .AddSingleton<LavaLinkAudio>()                
                .AddSingleton<InteractiveService>()
                .AddSingleton<GlobalData>()
                .BuildServiceProvider();
        }
    }
}
