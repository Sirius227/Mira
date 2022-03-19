using Mira.DataStructs;
using Newtonsoft.Json;

namespace Mira.Services
{
    public static class LyricsService
    {
        private static readonly Lazy<HttpClient> LazyHttpClient = new();
        internal static readonly HttpClient HttpClient = LazyHttpClient.Value;

        public static string? Title { get; private set; }
        public static string? TrackURL { get; private set; }
        public static string? TrackImage { get; private set; }
        public static LyricsConfig? LyConfig { get; private set; }

        public static async ValueTask<string> GetLyricsFromGenius(string query)  
            => await SearchGeniusAsync(query);         

        public static async ValueTask<string> SearchGeniusAsync(string query)
        {
            query = query.ToLower();

            if (query.Contains("official") || query.Contains("music") || query.Contains("video") || query.Contains("audio"))
            {
                query = query.Replace("official", "")
                    .Replace("music", "").Replace("]", "")
                    .Replace("(", "").Replace("video", "").Replace("[", "")
                    .Replace(")", "").Replace("lyric", "").Replace("lyrics", "")
                    .Replace("audio", "").Replace("4k", "").Replace("2k", "").Replace("8k", "").Trim();
            }

            Title = "";
            TrackURL = "";
            TrackImage = "";

            var responseMessage = await HttpClient.GetAsync($"custom script");

            if (!responseMessage.IsSuccessStatusCode)
                return "No lyrics found";

            var content = await responseMessage.Content.ReadAsStringAsync();
            LyConfig = JsonConvert.DeserializeObject<LyricsConfig>(content);

            Title = LyConfig?.Title ?? "";
            TrackURL = LyConfig?.Url ?? "";
            TrackImage = LyConfig?.ThumbnailUrl ?? "";
                        
            return LyConfig?.Lyrics ?? "No lyrics found";
        }
    }
}
