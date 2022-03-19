Mira - Discord music bot

Hi guys, I wanted to develop a discord bot with C#, which is currently the main programming language. Needed to play music: [lavalink](https://github.com/freyacodes/lavalink). 
Move the lavalink and application.yml files to the startup path. There are a total of 40 commands in the bot;

Libraries used;

[Discord.NET](https://github.com/discord-net/Discord.Net)\n
[Victoria](https://github.com/Yucked/Victoria)\n
[Fergun.Interactive](https://github.com/d4n3436/Fergun.Interactive)\n
Microsoft.Extensions.DependencyInjection\n
Newtonsoft.Json\n

Command Name | Description
------------ | -------------
default prefix | Makes the prefix default.
prefix | You can change the prefix.
set dj role | You determine the role of the DJ.
dj role remove | You remove the dj role.
help | Shows help menu
report | You can let us know when you encounter any errors.
ping | Checks the bot's response time to Discord.
user | Shows the user info.
roles | Shows the list of roles.
info | Shows the server info.
avatar | Shows the avatar of the user
invite | You can invite the bot to your own server.
purge | Deletes the number of messages you specified.
join | Joins the voice channel you're on.
leave | Leaving the voice channel.
replay | Repeats the current track.
playlist | Adds search results to the queue.
list | View the playlist.
shuffle | Shuffle the queue.
playskip | Plays a music with the given name or url.
play | Plays a music with the given name or url. (If the music is playing, it is added to the queue.)
soundcloud | Searches soundcloud for a music. (If the music is playing, it is added to the queue.)
lyrics | Retrieves the lyrics of the song mentioned or the current song.
now playing | Shows the music that is playing.
search | search results lists of the query
jump | Play music from the queue.
move | Move the selected song to the provided position.
swap | Swap track positions in the queue.
seek | Adjust the position of the music.
forward | Forward a certain amount in the current track.
rewind | Rewinds by a certain amount in the current track.
loop | Loops the whole queue.
remove | Removes a certain entry from the queue.
remove range | Remove musics in range.
stop | Stops the music and sets the bot to default position (does not exit the voice channel).
clear | Clear playlist.
skip | Skip the music playing.
volume | Adjusts the sound level.
pause | Pauses the music.
resume | Resumes paused music.
