using Discord;
using Mira.DataStructs;
using Mira.Services;
using Newtonsoft.Json;
using System.Text;

namespace Mira.Managers
{
    public class BotManager
    {
        private static string DatabaseFilePath => Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Database\\Mira.json";
        private static BotData? Data { get; set; }

        public BotManager()
        {
            InitializeAsync();
        }

        private static async void InitializeAsync()
        {
            if (!Directory.Exists(Path.GetDirectoryName(DatabaseFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DatabaseFilePath)!);
            }

            string json;

            if (!File.Exists(DatabaseFilePath))
            {
                json = JsonConvert.SerializeObject(GenerateNewFile(), Formatting.Indented);

                File.WriteAllText(DatabaseFilePath, json, new UTF8Encoding(false));
                await LoggingService.LogInformationAsync("Database", "New database file created");
                Data = GenerateNewFile();
                return;
            }

            json = File.ReadAllText(DatabaseFilePath, new UTF8Encoding(false));
            Data = JsonConvert.DeserializeObject<BotData>(json);
        }

        private static BotData GenerateNewFile() => new() { CustomPrefix = new(), LoopVariable = new(), DjRoles = new() };

        static void UpdateDatabaseFile()
        {
            string json = JsonConvert.SerializeObject(Data, Formatting.Indented);
            File.WriteAllText(DatabaseFilePath, json, new UTF8Encoding(false));
        }

        public static void DeleteVariable(string guildID)
        {
            Data!.LoopVariable!.Remove(guildID);
            Data.CustomPrefix!.Remove(guildID);
            Data.DjRoles!.Remove(guildID);
            UpdateDatabaseFile();
        }

        public static bool LoopVariable(string guildID) => Data!.LoopVariable![guildID];

        public static void LoopUpdate(string guildID, bool loop)
        {
            Data!.LoopVariable![guildID] = loop;
            UpdateDatabaseFile();
        }

        public static void InsertVariable(string guildID)
        {
            if (!Data!.LoopVariable!.ContainsKey(guildID)) Data.LoopVariable.Add(guildID, false);
            if (!Data!.CustomPrefix!.ContainsKey(guildID)) Data.CustomPrefix.Add(guildID, ".");
            if (!Data!.DjRoles!.ContainsKey(guildID)) Data.DjRoles.Add(guildID, "");
            UpdateDatabaseFile();
        }

        public static string GetPrefix(string guildID) => Data!.CustomPrefix![guildID];

        public static void UpdatePrefix(string guildID, string prefix)
        {
            Data!.CustomPrefix![guildID] = prefix;
            UpdateDatabaseFile();
        }

        public static bool DjRole(string guildID) => Data?.DjRoles![guildID] != "";

        public static void SetDjRole(string guildID, IRole role)
        {
            Data!.DjRoles![guildID] = role.Id.ToString();
            UpdateDatabaseFile();
        }

        public static string GetDjRoleID(string guildID) => Data?.DjRoles?[guildID]!;

        public static void DeleteDjRole(string guildID)
        {
            Data!.DjRoles![guildID] = "";
            UpdateDatabaseFile();
        }
    }
}

