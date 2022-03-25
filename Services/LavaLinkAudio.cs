using Discord;
using Discord.WebSocket;
using Mira.Handlers;
using System.Text;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Mira.Managers;
using Victoria.Responses.Search;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;

namespace Mira.Services
{
    public sealed class LavaLinkAudio
    {
        private readonly LavaNode _lavaNode;
        InteractiveService Interactive { get; set; }

        public LavaLinkAudio(LavaNode? lavaNode, InteractiveService? service)
        {
            _lavaNode = lavaNode!;
            Interactive = service!;
        }

        private bool GetDjRole(IGuild guild, SocketGuildUser user)
        {
            if (BotManager.DjRole(guild.Id.ToString()))
            {
                string roleID = BotManager.GetDjRoleID(guild.Id.ToString());
                djrole = guild.GetRole(ulong.Parse(roleID));
                var role = user.Roles.Count(x => x.Id.ToString() == roleID);

                if (role == 0)
                {
                    if (user.GuildPermissions.ManageRoles)
                        return true;

                    return false;
                }
            }

            return true;
        }

        private async Task<Embed> DjRoleErrorMessage()
            => await EmbedHandler.CreateErrorEmbed("Dj Role", "You must have dj role or manage roles permission to use this command\nDj Role: " + djrole!.Mention);

        private async Task<SearchResponse> SearchQueryFromYoutube(string query)
            => Uri.IsWellFormedUriString(query, UriKind.Absolute) ? await _lavaNode.SearchAsync(SearchType.Direct, query) : await _lavaNode.SearchAsync(SearchType.YouTube, query);

        bool play = false, pause = false;
        IRole? djrole;
        public async Task<Embed> JoinAsync(IGuild guild, IVoiceState voiceState, ITextChannel textChannel)
        {

            if (!GetDjRole(guild, (voiceState as SocketGuildUser)!))
            {
                return await DjRoleErrorMessage();
            }

            if (_lavaNode.HasPlayer(guild))
            {
                if (voiceState.VoiceChannel is null)
                    return await EmbedHandler.CreateErrorEmbed("Join", "You must join a voice channel!");

                var player = _lavaNode.GetPlayer(guild);

                int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

                if (voiceState.VoiceChannel.Id == player.VoiceChannel.Id)
                    return await EmbedHandler.CreateErrorEmbed("Join", "I'm connected to the voice channel you're on");

                if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    if (player.Track != null)
                    {
                        var track = player.Track;
                        var position = player.Track.Position;
                        var queue = player.Queue;

                        play = player.PlayerState == PlayerState.Playing;
                        pause = player.PlayerState == PlayerState.Paused;

                        await player.StopAsync();
                        player.Queue.Clear();

                        await _lavaNode.LeaveAsync(voiceState.VoiceChannel);
                        await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);

                        var _player = _lavaNode.GetPlayer(guild);

                        if (pause)
                        {
                            await _player.PlayAsync(track);
                            await _player.SeekAsync(position);
                            await _player.PauseAsync();

                            foreach (var item in queue)
                            {
                                _player.Queue.Enqueue(item);
                            }
                        }
                        else if (play)
                        {
                            await _player.PlayAsync(track);
                            await _player.SeekAsync(position);

                            foreach (var item in queue)
                            {
                                _player.Queue.Enqueue(item);
                            }
                        }
                        return await EmbedHandler.CreateBasicEmbed("Join", $"I joined to the channel \"{voiceState.VoiceChannel.Name}\" ");
                    }


                    await _lavaNode.LeaveAsync(voiceState.VoiceChannel);
                    await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);

                    return await EmbedHandler.CreateBasicEmbed("Join", $"I joined to the channel \"{voiceState.VoiceChannel.Name}\" ");
                }

                return await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");
            }

            try
            {
                await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);
                return await EmbedHandler.CreateBasicEmbed("Join", $"I joined to the channel \"{voiceState.VoiceChannel.Name}\" ");
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync("Join", ex.Message);
                return await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong");
            }
        }


        public async Task ReplayAsync(IGuild guild, ITextChannel channel, IMessage message, IVoiceState voiceState)
        {
            if (!GetDjRole(guild, (voiceState as SocketGuildUser)!))
            {
                await channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (!_lavaNode.HasPlayer(guild))
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "I'm not joined to a voice channel"));
                return;
            }

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                if (player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                {
                    await player.SeekAsync(TimeSpan.FromSeconds(0));
                    var emoji = new Emoji("🔂");
                    await message.AddReactionAsync(emoji);
                    return;
                }

                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Play", "Music is not playing"));
                return;
            }
            else
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!"));
                return;
            }
        }

        public async Task<Embed> PlaylistAsync(IVoiceState voiceState, ITextChannel textChannel, SocketGuildUser user, IGuild guild, string query)
        {
            if (!GetDjRole(guild, (voiceState as SocketGuildUser)!))
            {
                return await EmbedHandler.CreateErrorEmbed("Dj Role", "You don't have a DJ rol\nDj Role : {djrole.Mention}e");
            }

            if (user.VoiceChannel is null)
                return await EmbedHandler.CreateErrorEmbed("Join", "You must join a voice channel");

            if (query == null)
                return await EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Please enter the value you want to search\n\n.playlist [query]");

            await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);

            try
            {
                var player = _lavaNode.GetPlayer(guild);

                int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

                if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {

                    LavaTrack track;

                    var search = await SearchQueryFromYoutube(query);

                    if (search.Status == SearchStatus.NoMatches)
                    {
                        return await EmbedHandler.CreateErrorEmbed("No Matches", $"\"{query}\" I couldn't find anything about it ");
                    }

                    track = search.Tracks.FirstOrDefault()!;
                    var tracks = search.Tracks.ToList();

                    for (int i = 1; i < search.Tracks.Count; i++)
                        player.Queue.Enqueue(tracks[i]);

                    if (player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                        return await EmbedHandler.CreateBasicEmbed("Added to queue", "playlist added to queue", user);

                    await player.PlayAsync(track);
                    return await EmbedHandler.CreateBasicEmbed("Added to queue", $"playlist added to queue ", user);
                }

                return await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");
            }
            catch (ArgumentNullException ex)
            {
                await LoggingService.LogInformationAsync("Playlist", ex.Message);
                return await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong");
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync("Playlist", ex.Message);
                return await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong");
            }
        }

        public async Task ShuffleAsync(IGuild guild, SocketUserMessage userMessage, IVoiceState voiceState)
        {
            if (!GetDjRole(guild, (voiceState as SocketGuildUser)!))
            {
                await userMessage.Channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (!_lavaNode.HasPlayer(guild))
            {
                await userMessage.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "I'm not joined to a voice channel"));
                return;
            }

            var player = _lavaNode.GetPlayer(guild);


            if (player.Queue.Count == 0 || player.Queue.Count == 1)
            {
                await userMessage.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Queue",
                    "Not enough music to shuffle"));

                return;
            }

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                player.Queue.Shuffle();
                var emoji = new Emoji("👌");
                await userMessage.AddReactionAsync(emoji);
                return;
            }

            await userMessage.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!"));
        }

        public async Task ListAsync(IGuild guild, ITextChannel channel, SocketUser user)
        {
            if (!_lavaNode.HasPlayer(guild))
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "I'm not joined to a voice channel"));
                return;
            }

            var player = _lavaNode.GetPlayer(guild);

            List<string> tracks = new();
            StringBuilder builder = new();

            if (player.PlayerState is PlayerState.Playing)
            {
                if (player.Track.Duration.Hours == 0)
                {
                    builder.Append($"Playing 🎶 [{player.Track.Title}]({player.Track.Url}) | `{player.Track.Position:mm\\:ss}/{player.Track.Duration:mm\\:ss}`\n\n");
                }
                else
                {
                    builder.Append($"Playing 🎶 [{player.Track.Title}]({player.Track.Url}) | `{player.Track.Position:hh\\:mm\\:ss}/{player.Track.Duration:hh\\:mm\\:ss}`\n\n");
                }
            }
            else if (player.PlayerState is PlayerState.Paused)
            {
                if (player.Track.Duration.Hours == 0)
                {
                    builder.Append($"Paused ⏸️ [{player.Track.Title}]({player.Track.Url}) | `{player.Track.Position:mm\\:ss}/{player.Track.Duration:mm\\:ss}`\n\n");
                }
                else
                {
                    builder.Append($"Paused ⏸️ [{player.Track.Title}]({player.Track.Url}) | `{player.Track.Position:hh\\:mm\\:ss}/{player.Track.Duration:hh\\:mm\\:ss}`\n\n");
                }
            }

            string emoji;
            var loop = BotManager.LoopVariable(guild.Id.ToString());

            try
            {
                emoji = GetEmoji(loop);

                string time = GetTotalLength(player);

                int trackNum = 1;
                foreach (var track in player.Queue)
                {
                    if (track.Duration.Hours == 0)
                    {
                        builder.Append($"`{trackNum}.` [{track.Title}]({track.Url}) | `{track.Duration:mm\\:ss}`\n\n");
                    }
                    else
                    {
                        builder.Append($"`{trackNum}.` [{track.Title}]({track.Url}) | `{track.Duration:hh\\:mm\\:ss}`\n\n");
                    }

                    if (trackNum % 10 == 0)
                    {
                        tracks.Add(builder.ToString());
                        builder.Clear();
                    }

                    trackNum++;
                }

                if (builder.Length != 0)
                    tracks.Add(builder.ToString());

                if (tracks.Count == 1)
                {
                    await channel.SendMessageAsync(embed: new EmbedBuilder().WithAuthor($"{player.Queue.Count} Tracks | Total Length : {time}")
                        .WithTitle("Playlist")
                        .WithDescription(tracks[0])
                        .WithFooter(user.Username + $" page 1/1 Loop : {emoji}", user.GetAvatarUrl()).Build());

                    return;
                }

                var pages = new PageBuilder[tracks.Count];

                for (int i = 0; i < pages.Length; i++)
                {
                    pages[i] = new PageBuilder().WithAuthor($"{player.Queue.Count} Tracks | Total Length : {time}").WithTitle("Playlist")
                            .WithDescription(tracks[i])
                            .WithFooter(user.Username + $" page {i + 1}/{pages.Length} Loop : {emoji}", user.GetAvatarUrl());
                }

                var paginator = new StaticPaginatorBuilder()
                    .WithUsers(user)
                    .WithPages(pages)
                    .WithFooter(PaginatorFooter.None)
                    .Build();

                await Interactive.SendPaginatorAsync(paginator, channel, TimeSpan.FromMinutes(2));
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync("List", ex.ToString());
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("List", "Queue is empty"));
                return;
            }
        }
        private static string GetEmoji(bool loop)
        {
            if (loop)
                return "✅";
            else
                return "❌";
        }
        private static string GetTotalLength(LavaPlayer player)
        {
            TimeSpan sum = new(0, 0, 0);

            sum += player.Track.Duration - player.Track.Position;

            foreach (var track in player.Queue)
                sum += track.Duration;

            if (sum.Hours == 0)
                return sum.ToString("mm\\:ss");

            else
                return sum.ToString("hh\\:mm\\:ss");

        }

        public async Task PlaySpotifyAsync(IGuild guild, ITextChannel channel, SocketUser user, SocketUser userSpoti)
        {
            if (!GetDjRole(guild, (user as SocketGuildUser)!))
            {
                await channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if ((user as SocketGuildUser)?.VoiceChannel == null)
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "You must join a voice channel"));
                return;
            }

            var spotifyUser = userSpoti ?? user;

            foreach (var item in user.Activities)
            {
                Console.WriteLine(item.Name);
            }

            if (spotifyUser.Activities.FirstOrDefault(x => x.Name == "Spotify") == null)
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Play Spotify", "User is not listening on Spotify"));
                return;
            }

            await _lavaNode.JoinAsync((user as SocketGuildUser)?.VoiceChannel, channel);
            var player = _lavaNode.GetPlayer(guild);
            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            try
            {
                if ((user as SocketGuildUser)?.VoiceChannel == player.VoiceChannel || (((user as SocketGuildUser)?.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    LavaTrack track;

                    SpotifyGame spotify = null!;

                    try
                    {
                        spotify = (spotifyUser.Activities.FirstOrDefault(x => x.Type == ActivityType.Listening) as SpotifyGame)!;
                    }
                    catch (Exception ex)
                    {
                        var msg = await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("User Spotify", ex.Message));
                        await Task.Delay(TimeSpan.FromSeconds(10));
                        await msg.DeleteAsync();
                        return;
                    }

                    string query = "";

                    query += string.Join(", ", spotify.Artists);
                    query += " - " + spotify.TrackTitle;

                    var search = await SearchQueryFromYoutube(query);

                    if (search.Status is SearchStatus.NoMatches)
                    {
                        await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("No Matches", $"\"{query}\" I couldn't find anything about it "));
                        return;
                    }

                    track = search.Tracks.FirstOrDefault()!;

                    if (player.Track != null && player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                    {
                        player.Queue.Enqueue(track);
                        await channel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Added to queue", $"[{track.Title}]({track.Url})"));
                        return;
                    }

                    await player.PlayAsync(track);
                    return;
                }
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!"));
            }
            catch (ArgumentNullException ex)
            {
                await LoggingService.LogInformationAsync("Play", ex.Message);
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong"));
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync("Play", ex.Message);
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong"));
            }
        }

        public async Task PlayAsync(IVoiceState voiceState, ITextChannel textChannel, SocketGuildUser user, IGuild guild, string query)
        {
            if (!GetDjRole(guild, (voiceState as SocketGuildUser)!))
            {
                await textChannel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (user.VoiceChannel == null)
            {
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "You must join a voice channel"));
                return;
            }

            if (query == null)
            {
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Please enter the value you want to search\n\n.play [query]"));
                return;
            }

            await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            try
            {
                if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    LavaTrack track;

                    var search = await SearchQueryFromYoutube(query);

                    if (search.Status == SearchStatus.NoMatches)
                    {
                        await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("No Matches", $"\"{query}\" I couldn't find anything about it "));
                        return;
                    }

                    track = search.Tracks.FirstOrDefault()!;

                    if (player.Track != null && player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                    {
                        player.Queue.Enqueue(track);
                        await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Added to queue", $"[{track.Title}]({track.Url}) "));
                        return;
                    }

                    await player.PlayAsync(track);
                    return;
                }

                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!"));
            }
            catch (ArgumentNullException ex)
            {
                await LoggingService.LogInformationAsync("Play", ex.Message);
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong"));
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync("Play", ex.Message);
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong"));
            }
        }

        public async Task PlaySkipAsync(IVoiceState voiceState, ITextChannel textChannel, IGuild guild, string query)
        {
            if (!GetDjRole(guild, (voiceState as SocketGuildUser)!))
            {
                await textChannel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (voiceState.VoiceChannel is null)
            {
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "You must join a voice channel"));
                return;
            }

            if (query is null)
            {
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Please enter the value you want to search\n\n.play skip [query]"));
                return;
            }

            await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            try
            {
                if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    LavaTrack track;

                    var search = await SearchQueryFromYoutube(query);

                    if (search.Status == SearchStatus.NoMatches)
                    {
                        await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("No Matches", $"\"{query}\" I couldn't find anything about it "));
                        return;
                    }

                    track = search.Tracks.FirstOrDefault()!;
                    await player.PlayAsync(track);
                    return;
                }

                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!"));
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync("Play Skip", ex.Message);
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong"));
            }

        }

        public async Task PlayCloudAsync(IVoiceState voiceState, ITextChannel textChannel, IGuild guild, string query)
        {
            if (!GetDjRole(guild, (voiceState as SocketGuildUser)!))
            {
                await textChannel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (voiceState.VoiceChannel is null)
            {
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "You must join a voice channel"));
                return;
            }

            if (query is null)
            {
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Please enter the value you want to search\n\n.play skip [query]"));
                return;
            }

            await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            try
            {
                if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    LavaTrack track;

                    var search = Uri.IsWellFormedUriString(query, UriKind.Absolute) ?
                        await _lavaNode.SearchAsync(SearchType.SoundCloud, query) : await _lavaNode.SearchSoundCloudAsync(query);

                    if (search.Status == SearchStatus.NoMatches)
                    {
                        await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("No Matches", $"\"{query}\" I couldn't find anything about it "));
                        return;
                    }

                    track = search.Tracks.FirstOrDefault()!;

                    if (player.Track != null && player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                    {
                        player.Queue.Enqueue(track);
                        await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Added to queue", $"[{track.Title}]({track.Url}) "));
                        return;
                    }

                    await player.PlayAsync(track);
                    return;
                }

                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!"));
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync("Play", ex.Message);
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong"));
            }

        }

        public async Task LyricsAsync(IGuild guild, ITextChannel channel, SocketGuildUser user, string query)
        {
            LavaPlayer player = null!;

            if (_lavaNode.HasPlayer(guild))
            {
                player = _lavaNode.GetPlayer(guild);
            }

            if (query == null)
            {
                if (player.Track is null)
                {
                    var prefix = BotManager.GetPrefix(guild.Id.ToString());
                    await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", $"Example : {prefix}lyrics <query>"));
                    return;
                }
                query = player.Track.Title;
            }
            try
            {
                await channel.SendMessageAsync($"<:genius:846266429657317407> **Searching** 🔎 `{query}`");
                string lyrics = await LyricsService.GetLyricsFromGenius(query);

                if (lyrics.Length > 4000)
                {
                    await channel.SendMessageAsync(embed: await EmbedHandler.CreateLyricsEmbed($"{LyricsService.Title} Lyrics", lyrics[..3950] + $"...\n\nFor the full lyrics, [click here]({LyricsService.TrackURL})", user, LyricsService.TrackImage!));
                    return;
                }

                await channel.SendMessageAsync(embed: await EmbedHandler.CreateLyricsEmbed($"{LyricsService.Title} Lyrics", lyrics, user, LyricsService.TrackImage!));
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync(ex.Source!, ex.Message);
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong"));
            }
        }

        public async Task<Embed> SongAsync(IGuild guild)
        {
            if (!_lavaNode.HasPlayer(guild))
                return await EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel");

            var player = _lavaNode.GetPlayer(guild);

            if (player.Track is null)
            {
                return await EmbedHandler.CreateErrorEmbed("Play", "Music is not playing");
            }

            if (player.Track.Duration.Hours == 0)
            {
                if (player.PlayerState is PlayerState.Paused)
                    return await EmbedHandler.CreateBasicEmbed("Paused ⏸️", $"[{player.Track.Title}]({player.Track.Url}) " +
                        $"[`{player.Track.Position:mm\\:ss}\\" +
                        $"{player.Track.Duration:mm\\:ss}`]");

                return await EmbedHandler.CreateBasicEmbed("Now Playing 🎶", $"[{player.Track.Title}]({player.Track.Url}) " +
                    $"[`{player.Track.Position:mm\\:ss}\\" +
                    $"{player.Track.Duration:mm\\:ss}`]");
            }
            else
            {
                if (player.PlayerState is PlayerState.Paused)
                    return await EmbedHandler.CreateBasicEmbed("Paused ⏸️", $"[{player.Track.Title}]({player.Track.Url}) " +
                        $"[`{player.Track.Position:hh\\:mm\\:ss}\\" +
                        $"{player.Track.Duration:hh\\:mm\\:ss}`]");

                return await EmbedHandler.CreateBasicEmbed("Now Playing 🎶", $"[{player.Track.Title}]({player.Track.Url}) " +
                    $"[`{player.Track.Position:hh\\:mm\\:ss}\\" +
                    $"{player.Track.Duration:hh\\:mm\\:ss}`]");
            }
        }

        readonly static List<ulong> names = new();
        readonly static List<IUserMessage> messages = new();
        public async Task SearchAsync(ITextChannel textChannel, SocketGuildUser user, string query, IGuild guild)
        {
            if (!GetDjRole(guild, user))
            {
                await textChannel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (user.VoiceChannel is null)
            {
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Search", "You must join a voice channel"));
                return;
            }

            if (query == null)
            {
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Invalid Usage :x:",
                    "Please enter the value you want to search\n\n.search [query]"));
                return;
            }

            await _lavaNode.JoinAsync(user.VoiceChannel, textChannel);
            var player = _lavaNode.GetPlayer(guild);
            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            try
            {
                if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    if (names.Contains(user.Id))
                    {
                        await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Cancel",
                                "Write \"cancel\" to exit the search that is already active"));
                        return;
                    }

                    StringBuilder TrackList = new();

                    var search = await SearchQueryFromYoutube(query);

                    if (search.Status == SearchStatus.NoMatches)
                    {
                        await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Search",
                            $"\"{query}\" I couldn't find anything about it"));
                        return;
                    }

                    var searchTracks = search.Tracks.ToList();
                    LavaTrack[] tracks = new LavaTrack[24];

                    if (search.Tracks.Count < 24)
                        tracks = new LavaTrack[search.Tracks.Count];

                    for (int i = 0; i < tracks.Length; i++)
                        tracks[i] = searchTracks[i];

                    List<SelectMenuOptionBuilder> options = new();

                    foreach (var track in tracks)
                    {
                        options.Add(new SelectMenuOptionBuilder
                        {
                            Label = track.Title,
                            Value = track.Url,
                            Description = track.Author
                        });
                    }

                    var selectMenuBuilder = new SelectMenuBuilder
                    {
                        Options = options,
                        CustomId = user.Id.ToString(),
                        Placeholder = "Please select a track",
                        MaxValues = 1,
                        MinValues = 1,
                        IsDisabled = false
                    };

                    var builder = new ComponentBuilder().WithSelectMenu(selectMenuBuilder);

                    var msg = await textChannel.SendMessageAsync("Results", components: builder.Build());

                    messages.Add(msg);
                    names.Add(user.Id);
                    //querys.Add(tracks);                    

                    await Task.Delay(TimeSpan.FromSeconds(40));

                    if (names.Contains(user.Id))
                    {
                        names.Remove(user.Id);
                        messages.Remove(msg);

                        await msg.DeleteAsync();

                        await textChannel.SendMessageAsync("**Timeout** ❌");
                    }

                    return;
                }

                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!"));
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync("Search", ex.Message);
                await textChannel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong"));
            }

        }

        public async Task SelectedMenu(SocketMessageComponent component)
        {
            var guild = (component.Channel as SocketGuildChannel)!.Guild;
            var voiceState = component.User as IVoiceState;

            if (component.Data.CustomId == component.User.Id.ToString())
            {
                try
                {
                    if (names.Contains(component.User.Id))
                    {
                        if (voiceState!.VoiceChannel == null)
                        {
                            await component.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "You must join a voice channel :x:"));
                        }

                        if (!_lavaNode.HasPlayer(guild))
                            await _lavaNode.JoinAsync(voiceState.VoiceChannel, component.Channel as ITextChannel);

                        var player = _lavaNode.GetPlayer(guild);
                        var search = await SearchQueryFromYoutube(component.Data.Values.First());

                        if (search.Status == SearchStatus.NoMatches)
                        {
                            await component.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Play", "Track not found"));
                            return;
                        }

                        var track = search.Tracks.First();

                        if (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused)
                        {
                            player.Queue.Enqueue(track);
                            names.Remove(component.User.Id);
                            messages.Remove(component.Message);
                            await component.Channel.SendMessageAsync(embed: await EmbedHandler.CreateBasicEmbed("Added to queue", $"[{track.Title}]({track.Url})"));
                            await component.Message.DeleteAsync();
                            return;
                        }

                        await player.PlayAsync(track);
                        names.Remove(component.User.Id);
                        messages.Remove(component.Message);
                        await component.Message.DeleteAsync();
                    }
                }
                catch (Exception ex)
                {
                    await component.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed(ex.Source!, ex.Message));
                }
            }
        }

        public async Task JumpAsync(IGuild guild, string title, SocketGuildUser user, ITextChannel channel)
        {
            if (!GetDjRole(guild, user))
            {
                await channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (!_lavaNode.HasPlayer(guild))
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel"));
                return;
            }

            if (title is null || title == "0")
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Invalid Usage ❌", "Example : .jump {number or title}"));
                return;
            }

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (player.Queue.Count == 0)
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Queue", "music queue is empty"));
                return;
            }

            LavaTrack track;

            if (int.TryParse(title, out int position))
            {
                if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    position--;
                    track = player.Queue.RemoveAt(position);
                    await player.PlayAsync(track);
                    return;
                }

                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!"));
                return;
            }

            if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                track = player.Queue.FirstOrDefault(x => x.Title == title)!;
                if (track is null)
                {
                    await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed(null!, "there is no such part"));
                    return;
                }
                player.Queue.Remove(track);

                await player.PlayAsync(track);
                return;
            }

            await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!"));
        }

        public async Task MoveAsync(IGuild guild, ITextChannel channel, SocketMessage message, SocketGuildUser user, int move, int position)
        {
            if (!GetDjRole(guild, user))
            {
                await channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (!_lavaNode.HasPlayer(guild))
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel"));
                return;
            }

            if (user.VoiceChannel is null)
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "You must to join a voice channel"));
                return;
            }

            if (position == 0 || move == 0)
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Invalid Usage ❌", "Example : .move 1 3"));
                return;
            }

            var player = _lavaNode.GetPlayer(guild);

            if (player.Queue.Count == 0 || player.Queue.Count == 1)
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Move", "Not enough tracks in the tail"));
                return;
            }

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            try
            {
                if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    move--;
                    position--;

                    var que = player.Queue.ToList();
                    var track = que[move];
                    que.Remove(que[move]);

                    player.Queue.Clear();

                    for (int i = 0; i < que.Count; i++)
                    {
                        if (i == position)
                        {
                            player.Queue.Enqueue(track);
                        }
                        player.Queue.Enqueue(que[i]);
                    }

                    await message.AddReactionAsync(new Emoji("👌"));
                    return;
                }

                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!"));
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync(ex.Source!, ex.Message);
                await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong");
            }
        }

        public async Task SwapAsync(IGuild guild, ITextChannel channel, SocketMessage message, SocketGuildUser user, int number1, int number2)
        {
            if (!GetDjRole(guild, user))
            {
                await channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (!_lavaNode.HasPlayer(guild))
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel"));
                return;
            }

            if (user.VoiceChannel is null)
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "You must to join a voice channel"));
                return;
            }

            if (number1 == 0 || number2 == 0)
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Invalid Usage ❌", "Example : .swap 1 3"));
                return;
            }

            var player = _lavaNode.GetPlayer(guild);

            if (player.Queue.Count == 0 || player.Queue.Count == 1)
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Move", "Not enough tracks in the tail"));
                return;
            }

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            try
            {
                if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    number1--;
                    number2--;
                    var que = player.Queue.ToList();

                    player.Queue.Clear();

                    LavaTrack swap = que[number1];
                    que[number1] = que[number2];
                    que[number2] = swap;

                    foreach (var item in que)
                    {
                        player.Queue.Enqueue(item);
                    }

                    await message.AddReactionAsync(new Emoji("👌"));
                    return;
                }

                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!"));
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync(ex.Source!, ex.Message);
                await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong");
            }
        }

        public async Task<Embed> SeekAsync(string position, IGuild guild, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            if (position is null)
                return await EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Example: `1:20` `1:30` `30` `40`");

            if (user.VoiceChannel is null)
                return await EmbedHandler.CreateErrorEmbed("Join", "You must to join a voice channel");

            if (!_lavaNode.HasPlayer(guild))
                return await EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel");

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                string[] times = Array.Empty<string>();
                int h = 0, m = 0, s;

                try
                {
                    if (position.Contains(':'))
                        times = position.Split(':');

                    if (times.Length == 2)
                    {
                        m = int.Parse(times[0]);
                        s = int.Parse(times[1]);
                    }
                    else if (times.Length == 3)
                    {
                        h = int.Parse(times[0]);
                        m = int.Parse(times[1]);
                        s = int.Parse(times[2]);
                    }
                    else
                    {
                        s = int.Parse(position);
                    }
                    if (s < 0 || m < 0 || h < 0)
                    {
                        return await EmbedHandler.CreateErrorEmbed("Seek", "Please enter in positive value");
                    }
                    TimeSpan seek = new(h, m, s);

                    if (player.Track!.Duration < seek)
                    {
                        return await EmbedHandler.CreateErrorEmbed("Seek", "Value must not be greater than current track duration");
                    }

                    await player.SeekAsync(seek);

                    if (seek.Hours == 0)
                        return await EmbedHandler.CreateBasicEmbed("Seek", $"Set position to `{seek:mm\\:ss}`", user);
                    else
                        return await EmbedHandler.CreateBasicEmbed("Seek", $"Set position to `{seek:hh\\:mm\\:ss}`");
                }


                catch (Exception ex)
                {
                    if (ex.Message == "Input string was not in a correct format.")
                        return await EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Example: `1:20` `1:30` `30` `40`");

                    return await EmbedHandler.CreateErrorEmbed("Seek", ex.Message);
                }
            }

            return await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");
        }

        public async Task<Embed> Forward(IGuild guild, string position, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            if (position is null)
                return await EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Example: `1:20` `1:30` `30` `40`");

            if (user.VoiceChannel is null)
                return await EmbedHandler.CreateErrorEmbed("Join", "You must to join a voice channel");

            if (!_lavaNode.HasPlayer(guild))
                return await EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel");

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                string[] times = Array.Empty<string>();
                int h = 0, m = 0, s;

                try
                {
                    if (position.Contains(':'))
                        times = position.Split(':');

                    if (times.Length == 2)
                    {
                        m = int.Parse(times[0]);
                        s = int.Parse(times[1]);
                    }
                    else if (times.Length == 3)
                    {
                        h = int.Parse(times[0]);
                        m = int.Parse(times[1]);
                        s = int.Parse(times[2]);
                    }
                    else
                    {
                        s = int.Parse(position);
                    }
                    if (s < 0 || m < 0 || h < 0)
                    {
                        return await EmbedHandler.CreateErrorEmbed("Forward", "Please enter in positive value");
                    }
                    TimeSpan seek = new(h, m, s);
                    seek += player.Track!.Position;

                    if (player.Track.Duration < seek)
                    {
                        return await EmbedHandler.CreateErrorEmbed("Forward", "Value must not be greater than current track duration");
                    }

                    await player.SeekAsync(seek);

                    if (seek.Hours == 0)
                        return await EmbedHandler.CreateBasicEmbed("Forward", $"Set position to `{seek:mm\\:ss}`", user);
                    else
                        return await EmbedHandler.CreateBasicEmbed("Forward", $"Set position to `{seek:hh\\:mm\\:ss}`");
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Input string was not in a correct format.")
                        return await EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Example: `1:20` `1:30` `30` `40`");

                    return await EmbedHandler.CreateErrorEmbed("Forward", ex.Message);
                }
            }

            return await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");

        }

        public async Task<Embed> Rewind(IGuild guild, string position, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            if (position is null)
                return await EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Example: `1:20` `1:30` `30` `40`");

            if (user.VoiceChannel is null)
                return await EmbedHandler.CreateErrorEmbed("Join", "You must to join a voice channel");

            if (!_lavaNode.HasPlayer(guild))
                return await EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel");

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                string[] times = Array.Empty<string>();
                int h = 0, m = 0, s;

                try
                {
                    if (position.Contains(':'))
                        times = position.Split(':');

                    if (times.Length == 2)
                    {
                        m = int.Parse(times[0]);
                        s = int.Parse(times[1]);
                    }
                    else if (times.Length == 3)
                    {
                        h = int.Parse(times[0]);
                        m = int.Parse(times[1]);
                        s = int.Parse(times[2]);
                    }
                    else
                    {
                        s = int.Parse(position);
                    }
                    if (s < 0 || m < 0 || h < 0)
                    {
                        return await EmbedHandler.CreateErrorEmbed("Rewind", "Please enter in positive value");
                    }
                    TimeSpan seek = new(h, m, s);

                    if (player.Track!.Duration < seek)
                    {
                        return await EmbedHandler.CreateErrorEmbed("Rewind", "Value must not be greater than current track duration");
                    }

                    seek = player.Track.Position - seek;

                    await player.SeekAsync(seek);

                    if (seek.Hours == 0)
                        return await EmbedHandler.CreateBasicEmbed("Rewind", $"Set position to `{seek:mm\\:ss}`", user);
                    else
                        return await EmbedHandler.CreateBasicEmbed("Rewind", $"Set position to `{seek:hh\\:mm\\:ss}`");
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Input string was not in a correct format.")
                        return await EmbedHandler.CreateErrorEmbed("Invalid Usage :x:", "Example: `1:20` `1:30` `30` `40`");

                    return await EmbedHandler.CreateErrorEmbed("Rewind", ex.Message);
                }
            }

            return await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");

        }

        bool loop = true;
        public async Task LoopAsync(IGuild guild, ITextChannel channel, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                await channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (!_lavaNode.HasPlayer(guild))
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel"));
                return;
            }

            loop = BotManager.LoopVariable(guild.Id.ToString());

            if (loop)
            {
                BotManager.LoopUpdate(guild.Id.ToString(), false);
                await channel.SendMessageAsync("🔁 *Loop Disabled*");
                return;
            }

            BotManager.LoopUpdate(guild.Id.ToString(), true);
            await channel.SendMessageAsync("🔁 *Loop Enabled*");
        }

        public async Task<Embed> RemoveAsync(string title, IGuild guild, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            if (!_lavaNode.HasPlayer(guild))
                return await EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel");

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (player.Queue.Count == 0)
                return await EmbedHandler.CreateErrorEmbed("Queue", "music queue is empty");

            if (title is null || title == "0")
            {
                return await EmbedHandler.CreateErrorEmbed("Invalid Usage ❌", "Example : .remove {number or title}");
            }

            if (int.TryParse(title, out int indis))
            {
                if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    LavaTrack track;
                    indis--;

                    track = player.Queue.RemoveAt(indis);
                    return await EmbedHandler.CreateBasicEmbed("Remove", $"Removed 📑 [{track.Title}]({track.Url})", user);
                }

                return await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");
            }

            if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                LavaTrack track;

                track = player.Queue.FirstOrDefault(x => x.Title == title)!;

                if (track is null)
                {
                    return await EmbedHandler.CreateErrorEmbed(null!, "there is no such track ❌");
                }

                player.Queue.Remove(track);
                return await EmbedHandler.CreateBasicEmbed("Remove", $"Removed 📑 [{track.Title}]({track.Url})", user);
            }

            return await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");

        }

        public async Task<Embed> RemoveRange(IGuild guild, int trackNum1, int trackNum2, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            if (!_lavaNode.HasPlayer(guild))
                return await EmbedHandler.CreateErrorEmbed("Join", $"I must to join a voice channel");

            if (trackNum1 == 0 || trackNum2 == 0)
                return await EmbedHandler.CreateErrorEmbed("Invalid Usage ❌", "Value cannot be zero");

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (player.Queue.Count == 0)
                return await EmbedHandler.CreateErrorEmbed("Queue", "Queue is empty");

            if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                int a = trackNum1, b = trackNum2;

                trackNum1--;
                trackNum2--;

                if (trackNum1 > player.Queue.Count || trackNum2 > player.Queue.Count)
                    return await EmbedHandler.CreateErrorEmbed("Remove Range", "you cannot enter a value greater than the music queue");

                for (int i = trackNum1; i <= trackNum2; i++)
                    player.Queue.RemoveAt(trackNum1);

                return await EmbedHandler.CreateBasicEmbed("Remove Range", $"Range {a} and {b} removed 📑", user);
            }

            return await EmbedHandler.CreateErrorEmbed("Join", "Someone else is already listening to music on different channel!");
        }
        public async Task LeaveAsync(IGuild guild, SocketUserMessage message, IVoiceState voiceState)
        {
            if (!GetDjRole(guild, (voiceState as SocketGuildUser)!))
            {
                await message.Channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (!_lavaNode.HasPlayer(guild))
            {
                await message.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Leave", "I'm not joined to a voice channel"));
                return;
            }

            var player = _lavaNode.GetPlayer(guild);

            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

            if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                player.Queue.Clear();
                BotManager.LoopUpdate(guild.Id.ToString(), false);

                if (player.Track != null) { await player.StopAsync(); await Task.Delay(750); }

                await _lavaNode.LeaveAsync(player.VoiceChannel);
                await message.AddReactionAsync(new Emoji("👋"));

                return;
            }

            await message.Channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Leave", "Someone else is already listening to music on different channel!"));
        }

        public async Task ClearAsync(IGuild guild, ITextChannel channel, IVoiceState voiceState)
        {
            if (!GetDjRole(guild, (voiceState as SocketGuildUser)!))
            {
                await channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            try
            {
                if (!_lavaNode.HasPlayer(guild))
                {
                    await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel"));
                    return;
                }

                var player = _lavaNode.GetPlayer(guild);

                int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

                if (player.Queue.Count == 0)
                {
                    await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Queue", "Queue is empty"));
                    return;
                }

                if (voiceState.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                {
                    player.Queue.Clear();
                    await channel.SendMessageAsync(":bookmark_tabs: *Cleared queue*");
                }
            }
            catch (Exception e)
            {
                await LoggingService.LogInformationAsync("Clear", e.Message);
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong"));
            }

        }

        public async Task SkipTrackAsync(IGuild guild, SocketGuildUser user, ITextChannel channel, SocketMessage message)
        {
            if (!GetDjRole(guild, user))
            {
                await channel.SendMessageAsync(embed: await DjRoleErrorMessage());
                return;
            }

            if (!_lavaNode.HasPlayer(guild))
            {
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Join", "I must to join a voice channel"));
                return;
            }

            try
            {
                var player = _lavaNode.GetPlayer(guild);

                if (player == null)
                {
                    await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Play", $"Music is not playing"));
                    return;
                }


                if (player.Queue.Count < 1)
                {
                    await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Queue", "Queue is empty!"));
                    return;
                }

                int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

                try
                {
                    if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
                    {
                        await player.SkipAsync();
                        await message.AddReactionAsync(new Emoji("⏭️"));
                        return;
                    }

                    await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed("Skip", "Someone else is already listening to music on different channel!"));
                }
                catch (Exception ex)
                {
                    await LoggingService.LogInformationAsync("Skip", ex.Message);
                    await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong"));
                    return;
                }

            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync("Skip", ex.Message);
                await channel.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong"));
            }
        }

        public async Task<Embed> StopAsync(IGuild guild, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            try
            {
                var player = _lavaNode.GetPlayer(guild);

                int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

                if (player.PlayerState is not PlayerState.Playing || (player == null!))
                    return await EmbedHandler.CreateErrorEmbed("Play", "Music is not playing");

                if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)))
                {
                    if (player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                    {
                        player.Queue.Clear();
                        await player.StopAsync();
                        return await EmbedHandler.CreateBasicEmbed("Stop :stop_button:", $"Stopped and queue cleared.", user);
                    }
                }
                else
                    return await EmbedHandler.CreateErrorEmbed("Stop", "Someone else is already listening to music on different channel!");

                return await EmbedHandler.CreateErrorEmbed("Play", "Music is not playing");
            }
            catch (Exception e)
            {
                await LoggingService.LogInformationAsync("Play", e.Message);
                return await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong");
            }
        }

        public async Task<Embed> SetVolumeAsync(IGuild guild, int volume, SocketGuildUser user)
        {
            if (!_lavaNode.HasPlayer(guild))
                return await EmbedHandler.CreateErrorEmbed("Join", $"I must to join a voice channel");

            if (volume >= 150 || volume <= 0)
                return await EmbedHandler.CreateErrorEmbed("Volume", "Please enter value in the range 0 and 150");

            var player = _lavaNode.GetPlayer(guild);

            try
            {
                await player.UpdateVolumeAsync((ushort)volume);
                await LoggingService.LogInformationAsync("Volume", $"Volume: {volume}");
                return await EmbedHandler.CreateBasicEmbed("Volume", $"Volume: {volume}", user);
            }
            catch (InvalidOperationException)
            {
                return await EmbedHandler.CreateBasicEmbed("Play", "Music is not play", user);
            }
        }

        public async Task<Embed> PauseAsync(IGuild guild, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            try
            {
                if (!_lavaNode.HasPlayer(guild))
                    return await EmbedHandler.CreateErrorEmbed("Join", $"I must to join a voice channel");

                var player = _lavaNode.GetPlayer(guild);

                if (player.Track is null)
                {
                    return await EmbedHandler.CreateErrorEmbed("Play", "Music is not playing");
                }

                if (player.PlayerState is PlayerState.Paused)
                {
                    return await EmbedHandler.CreateErrorEmbed("Resume", "Music has already been paused");
                }

                int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

                if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)))
                {
                    await player.PauseAsync();
                    return await EmbedHandler.CreateBasicEmbed("Pause ⏸️", $"[{player.Track.Title}]({player.Track.Url})", user);
                }
                return await EmbedHandler.CreateErrorEmbed("Pause", "Someone else is already listening to music on different channel!");
            }
            catch (InvalidOperationException ex)
            {
                await LoggingService.LogInformationAsync("Pause", ex.Message);
                return await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong");
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync("Pause", ex.Message);
                return await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong");
            }
        }

        public async Task<Embed> ResumeAsync(IGuild guild, SocketGuildUser user)
        {
            if (!GetDjRole(guild, user))
            {
                return await DjRoleErrorMessage();
            }

            try
            {
                if (!_lavaNode.HasPlayer(guild))
                    return await EmbedHandler.CreateErrorEmbed("Join", $"I must to join a voice channel");

                var player = _lavaNode.GetPlayer(guild);

                int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);

                if (user.VoiceChannel == player.VoiceChannel || ((user.VoiceChannel != player.VoiceChannel) && (users == 0)))
                {
                    if (player.PlayerState is PlayerState.Paused)
                    {
                        await player.ResumeAsync();
                        return await EmbedHandler.CreateBasicEmbed("Resume :ok_hand:", $"[{player.Track.Title}]({player.Track.Url})", user);
                    }
                }
                else
                    return await EmbedHandler.CreateErrorEmbed("Resume", "You must join the voice channel where the bot is located.");

                if (player.Track is null)
                    return await EmbedHandler.CreateErrorEmbed("Play", "Music is not playing");

                return await EmbedHandler.CreateErrorEmbed("Pause", "Music has already been playing");
            }
            catch (InvalidOperationException ex)
            {
                await LoggingService.LogInformationAsync("Resume", ex.Message);
                return await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong");
            }
            catch (Exception ex)
            {
                await LoggingService.LogInformationAsync("Resume", ex.Message);
                return await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong");
            }
        }
        public static async Task TrackException(TrackExceptionEventArgs args)
        {
            var msg = await args.Player.TextChannel.SendMessageAsync(embed:
                await EmbedHandler.CreateBasicEmbed("Track Exception", $"**Error Message** {args.Exception.Message}\n\n" +
                $"Track: {args.Track.Title}"));

            await Task.Delay(TimeSpan.FromSeconds(5));
            await msg.DeleteAsync();
        }
        public static async Task TrackStuck(TrackStuckEventArgs args)
        {
            var msg = await args.Player.TextChannel.SendMessageAsync(embed:
                await EmbedHandler.CreateBasicEmbed("Track Stuck", $"**Threshold** {args.Threshold.TotalMilliseconds}\n\n" +
                $"Track: {args.Track.Title}"));

            await args.Player.PlayAsync(args.Track);
            await Task.Delay(TimeSpan.FromSeconds(5));
            await msg.DeleteAsync();
        }

        private readonly Dictionary<ulong, IUserMessage> playingMessages = new();

        public async Task TrackStarted(TrackStartEventArgs args)
        {
            var msg = await args.Player.TextChannel.SendMessageAsync
            (embed: await EmbedHandler.CreateBasicEmbed("Playing 🎶", $"[{args.Track.Title}]({args.Track.Url})"));

            if (!playingMessages.ContainsKey(args.Player.TextChannel.GuildId)) await Task.Run(() => playingMessages.Add(args.Player.TextChannel.GuildId, msg));

            await msg.AddReactionAsync(new Emoji("⏭️"));
            await msg.AddReactionAsync(new Emoji("🔂"));
            await msg.AddReactionAsync(new Emoji("🔀"));
            await msg.AddReactionAsync(new Emoji("⏹️"));
            await msg.AddReactionAsync(new Emoji("⏯️"));
        }

        public async Task TrackEnded(TrackEndedEventArgs args)
        {
            if (playingMessages.TryGetValue(args.Player.TextChannel.GuildId, out var msg))
            {
                playingMessages.Remove(args.Player.TextChannel.GuildId);
                await msg.DeleteAsync();
            }

            loop = BotManager.LoopVariable(args.Player.TextChannel.GuildId.ToString());

            if (loop)
            {
                args.Player.Queue.Enqueue(args.Track);
            }

            if (args.Reason is TrackEndReason.Replaced)
                return;

            if (!args.Player.Queue.TryDequeue(out var queueable))
            {
                return;
            }

            if (queueable is not LavaTrack track)
            {
                return;
            }

            await args.Player.PlayAsync(track);
        }

        public async Task ReactionPlayerControlAsync(Cacheable<IUserMessage, ulong> cacheable, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            if (reaction.User.Value.IsBot)
                return;

            var guild = (channel.Value as SocketGuildChannel)!.Guild;

            if (!playingMessages.TryGetValue(guild.Id, out var message))
                return;

            if (message.Id != cacheable.Id)
                return;

            string emote = reaction.Emote.Name;

            var player = _lavaNode.GetPlayer(guild);
            int users = player.VoiceChannel.GetUsersAsync().FlattenAsync().Result.Count(x => !x.IsBot);
            var voiceState = reaction.User.Value as IVoiceState;

            if (voiceState!.VoiceChannel == player.VoiceChannel || ((voiceState.VoiceChannel != player.VoiceChannel) && (users == 0)) || player.Track is null)
            {
                try
                {
                    if (emote == "⏭️")
                    {
                        if (player.Queue.Count == 0)
                        {
                            await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                            await Interactive.DelayedDeleteMessageAsync(await channel.Value.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed(null!
                                , "Queue is empty")), TimeSpan.FromSeconds(10));
                            return;
                        }
                        await player.SkipAsync();
                    }
                    else if (emote == "🔂")
                    {
                        await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        await player.SeekAsync(TimeSpan.FromSeconds(0));
                        await Interactive.DelayedDeleteMessageAsync(await channel.Value.SendMessageAsync("*Track repeated* 🔂"));
                    }
                    else if (emote == "🔀")
                    {
                        await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);

                        if (player.Queue.Count <= 1)
                        {
                            await Interactive.DelayedDeleteMessageAsync(await channel.Value.SendMessageAsync("Not enough songs in the queue ❌"), TimeSpan.FromSeconds(10));
                            return;
                        }
                        player.Queue.Shuffle();
                        await Interactive.DelayedDeleteMessageAsync(await channel.Value.SendMessageAsync("*Queue shuffled* 🔀"), TimeSpan.FromSeconds(10));
                    }
                    else if (emote == "⏹️")
                    {
                        player.Queue.Clear();
                        BotManager.LoopUpdate(guild.Id.ToString(), false);
                        await player.StopAsync();
                        await channel.Value.SendMessageAsync(
                            embed: await EmbedHandler.CreateBasicEmbed("Stop :stop_button:", $"Stopped and queue cleared.", (voiceState as SocketGuildUser)!));
                    }
                    else if (emote == "⏯️")
                    {
                        if (player.PlayerState is PlayerState.Playing)
                            await player.PauseAsync();
                        else
                            await player.ResumeAsync();

                        var playEmbed = await EmbedHandler.CreateBasicEmbed("Playing 🎶", $"[{player.Track!.Title}]({player.Track.Url})");
                        var pauseEmbed = await EmbedHandler.CreateBasicEmbed("Paused ⏸️", $"[{player.Track.Title}]({player.Track.Url})");

                        await Task.Run(() => _ = player.PlayerState is PlayerState.Playing ?
                        message.ModifyAsync(x => x.Embed = playEmbed) : message.ModifyAsync(x => x.Embed = pauseEmbed));

                        await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    }
                }
                catch (Exception ex)
                {
                    await LoggingService.LogCriticalAsync(ex.Source!, ex.Message, ex);
                    await channel.Value.SendMessageAsync(embed: await EmbedHandler.CreateErrorEmbed(null!, "Something went wrong"));
                }
            }
        }

        public static async Task MessageUpdate(SocketMessage socketMessage)
        {
            if (socketMessage is not SocketUserMessage message || message.Author.IsBot || message.Author.IsWebhook || message.Channel is IPrivateChannel)
                return;

            var guild = (message.Channel as SocketGuildChannel)!.Guild;

            if (message.Content.ToLower() == "mira" || message.Content == guild.CurrentUser.Mention)
            {
                string prefix = BotManager.GetPrefix(guild.Id.ToString());

                await message.Channel.SendMessageAsync($"My prefix in server: `{prefix}`", embed: await EmbedHandler.CreateBasicEmbed(null!,
                       $"\nUse the `{prefix}play` command to play music.\n`{prefix}help` for help"));
                return;
            }

            if (message.Content.ToLower() == "cancel")
            {
                names.Remove(message.Author.Id);
                messages.RemoveAt(names.IndexOf(message.Author.Id));

                await message.Channel.SendMessageAsync("✅");
            }
        }
    }
}
