namespace Mira.DataStructs
{
    public class BotConfig
    {
        public string? DiscordToken { get; set; }
        public string? GeniusToken { get; set; }
        public string? WebhookToken { get; set; }
        public ulong WebhookId { get; set; }
        public string? GameStatus { get; set; }
        public List<ulong>? BlacklistedChannels { get; set; }
    }
}
