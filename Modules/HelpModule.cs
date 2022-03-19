using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mira.Handlers;

namespace Mira.Modules
{
    public static class HelpModule
    {
        public static Embed GetDefaultHelpEmbed(this CommandService commandService, string command, string prefix, SocketUser user, string serverIcon)
        {
            EmbedBuilder helpEmbedBuilder;
            var commandModules = commandService.GetModulesWithCommands();
            var moduleMatch = commandModules.FirstOrDefault(m => m.Name == command || m.Aliases.Contains(command));

            if (string.IsNullOrEmpty(command))
            {
                helpEmbedBuilder = commandService.GenerateHelpCommandEmbed();
            }
            else if (moduleMatch != null)
            {
                helpEmbedBuilder = GenerateSpecificModuleHelpEmbed(moduleMatch);
            }
            else
            {
                helpEmbedBuilder = GenerateSpecificCommandHelpEmbed(commandService, command, prefix);
            }

            helpEmbedBuilder.WithFooter(user.Username + "#" + user.Discriminator + " Total Commands : " + commandService.Commands.Count(), user.GetAvatarUrl());
            helpEmbedBuilder.WithColor(EmbedHandler.SetColor());
            helpEmbedBuilder.WithThumbnailUrl(serverIcon);
            return helpEmbedBuilder.Build();
        }

        private static IEnumerable<ModuleInfo> GetModulesWithCommands(this CommandService commandService)
            => commandService.Modules.Where(module => module.Commands.Count > 0);

        private static EmbedBuilder GenerateSpecificCommandHelpEmbed(this CommandService commandService, string command, string prefix)
        {

            var isNumeric = int.TryParse(command[^1].ToString(), out var pageNum);

            if (isNumeric)
                command = command[0..^2];
            else
                pageNum = 1;

            var helpEmbedBuilder = new EmbedBuilder();
            var commandSearchResult = commandService.Search(command);

            var commandsInfoWeNeed = new List<CommandInfo>();

            if (commandSearchResult.IsSuccess)
            {
                foreach (var c in commandSearchResult.Commands) commandsInfoWeNeed.Add(c.Command);
            }
            else
            {
                var commandModulesList = commandService.Modules.ToList();
                foreach (var c in commandModulesList) commandsInfoWeNeed.AddRange(c.Commands.Where(h => string.Equals(h.Name, command, StringComparison.CurrentCultureIgnoreCase)));
            }

            if (pageNum > commandsInfoWeNeed.Count || pageNum <= 0)
                pageNum = 1;


            if (commandsInfoWeNeed.Count <= 0)
            {
                helpEmbedBuilder.WithTitle("Command not found");
                return helpEmbedBuilder;
            }

            var commandInformation = commandsInfoWeNeed[pageNum - 1].GetCommandInfo(prefix);

            helpEmbedBuilder.WithFields(commandInformation);

            if (commandsInfoWeNeed.Count >= 2)
                helpEmbedBuilder.WithTitle($"Variant {pageNum}/{commandsInfoWeNeed.Count}.\n" +
                                "_______\n");

            return helpEmbedBuilder;
        }

        private static EmbedBuilder GenerateSpecificModuleHelpEmbed(ModuleInfo module)
        {
            var helpEmbedBuilder = new EmbedBuilder();
            helpEmbedBuilder.AddField(module.GetModuleName(), module.GetModuleInfo());
            return helpEmbedBuilder;
        }

        private static EmbedBuilder GenerateHelpCommandEmbed(this CommandService commandService)
        {
            var helpEmbedBuilder = new EmbedBuilder();
            var commandModules = commandService.GetModulesWithCommands();

            foreach (var module in commandModules)
            {
                helpEmbedBuilder.AddField(module.GetModuleName(), module.GetModuleInfo());
            }
            return helpEmbedBuilder;
        }

        public static string GetModuleInfo(this ModuleInfo module)
        {
            string sb = "";
            
            foreach (var item in module.Commands)
            {
                sb += $"`{item.Name.ToLower()}`, ";
            }

            sb = sb[0..^2];

            return sb;
        }

        public static string GetModuleName(this ModuleInfo module)
        {
            return module.Remarks != null ? $"{module.Remarks} {module.Name}" : module.Name;
        }

        public static List<EmbedFieldBuilder> GetCommandInfo(this CommandInfo command, string prefix)
        {
            var aliases = string.Join(", ", command.Aliases);
            var module = command.Module.Name;
            var parameters = string.Join(", ", command.GetCommandParameters());
            var name = command.MainName();
            var summary = command.Summary;

            var sb = new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder
                {
                    Name = "Command Name",
                    Value = name
                },
                new EmbedFieldBuilder
                {
                    Name = "Module",
                    Value = module,
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "Summary",
                    Value = summary
                },
                new EmbedFieldBuilder
                {
                    Name = "Usage",
                    Value = $"{prefix}{name} {parameters}"
                },
                new EmbedFieldBuilder
                {
                    Name = "Aliases",
                    Value = aliases
                }
            };                
            return sb;
        }

        public static IEnumerable<string> GetCommandParameters(this CommandInfo command)
        {
            var parameters = command.Parameters;
            var mandatoryTemplate = "[{0}]";
            List<string> parametersFormated = new();

            foreach (var parameter in parameters)
            {
                parametersFormated.Add(string.Format(mandatoryTemplate, parameter.Name));
            }

            return parametersFormated;
        }

        public static string MainName(this CommandInfo commandInfo) => commandInfo.Aliases[0];
    }
}
