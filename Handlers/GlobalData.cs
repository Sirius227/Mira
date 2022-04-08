using Discord;
using Mira.DataStructs;
using Mira.Services;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Mira.Handlers
{
    public class GlobalData
    {
        public static string ConfigPath { get; set; } = "config.json";
        public static BotConfig? Config { get; set; }

        public static async Task InitializeAsync()
        {
            string json;
            if (!File.Exists(ConfigPath))
            {
                json = JsonConvert.SerializeObject(GenerateNewConfig(), Formatting.Indented);
                File.WriteAllText(ConfigPath, json, new UTF8Encoding(false));
                await LoggingService.LogAsync("Bot", LogSeverity.Error, "New file created close then open");
                await Task.Delay(-1);
            }

            json = File.ReadAllText(ConfigPath, new UTF8Encoding(false));
            Config = JsonConvert.DeserializeObject<BotConfig>(json);
        }

        public static void StartLavalink()
        {
            Process myProcess = new();

            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.FileName = "java";
            myProcess.StartInfo.Arguments = $"-jar {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\Lavalink.jar";
            myProcess.Start();

            Thread.Sleep(7500);
        }

        private static BotConfig GenerateNewConfig() => new()
        {
            DiscordToken = "",
            WebhookId = 0,
            WebhookToken = "",
            GameStatus = "",
            BlacklistedChannels = new List<ulong>()
        };
    }
}
