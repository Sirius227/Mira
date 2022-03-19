using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Mira.Services;
using System.Reflection;
using Mira.Managers;

namespace Mira.Handlers
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        public CommandHandler(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            HookEvents();
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public void HookEvents()
        {
            _commands.CommandExecuted += CommandExecutedAsync;
            _commands.Log += LogAsync;
            _client.MessageReceived += HandleCommandAsync;
        }

        private Task HandleCommandAsync(SocketMessage socketMessage)
        {
            var argPos = 0;
            if (socketMessage is not SocketUserMessage message || message.Author.IsBot || message.Author.IsWebhook || message.Channel is IPrivateChannel)
                return Task.CompletedTask;

            var guild = (socketMessage.Channel as SocketGuildChannel)?.Guild;

            string prefix = BotManager.GetPrefix(guild?.Id.ToString()!);

            if (!message.HasStringPrefix(prefix, ref argPos))
                return Task.CompletedTask;

            var context = new SocketCommandContext(_client, socketMessage as SocketUserMessage);

            var blacklistedChannelCheck = from a in GlobalData.Config?.BlacklistedChannels
                                          where a == context.Channel.Id
                                          select a;

            var blacklistedChannel = blacklistedChannelCheck.FirstOrDefault();

            if (blacklistedChannel == context.Channel.Id)
            {
                return Task.CompletedTask;
            }
            else
            {
                var result = _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);
                return result;
            }
        }

        public static async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!command.IsSpecified)
                return;

            if (result.IsSuccess)
                return;

            if (result.ErrorReason == "The server responded with error 50013: Missing Permissions")
            {
                await LoggingService.LogAsync("Command", LogSeverity.Error, result.ErrorReason);
                return;
            }

            await ErrorMessage(context, result);
        }

        private static async Task ErrorMessage(ICommandContext context, IResult result)
        {
            var msg = await context.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Error", result.ErrorReason + " :x:"));
            await Task.Delay(5000);
            await msg.DeleteAsync();
            await context.Message.DeleteAsync();
        }

        private async Task LogAsync(LogMessage log)
        {
            await LoggingService.LogAsync(log.Source, log.Severity, log.Message, log.Exception);
        }
    }
}
