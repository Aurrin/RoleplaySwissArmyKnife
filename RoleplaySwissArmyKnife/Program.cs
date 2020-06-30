using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RoleplaySwissArmyKnife.Services;

namespace RoleplaySwissArmyKnife
{
    class Program
    {
        static void Main(string[] args) 
            => new Program().MainAsync(args).GetAwaiter().GetResult();

        //private DiscordSocketClient _client  ;
        //private CommandService      _commands;
        //private IServiceProvider    _services;

        private async Task MainAsync( string[] args )
        {
            //_client   = new DiscordSocketClient();
            //_commands = new CommandService     ();
            //_services = new ServiceCollection  ()
            //    .AddSingleton(_client  )
            //    .AddSingleton(_commands)
            //    .BuildServiceProvider();

            //string token = "NzIwMDIyNzA5MDYwMzcwNTAy.XuAzTQ.EGWkLr63TVBmdovo4ixLyH9UOII";
            //_client.Log += _client_Log;
            //await RegisterCommandsAsync();
            //await _client.LoginAsync(TokenType.Bot, token);
            //await _client.StartAsync();
            //await Task.Delay(Timeout.Infinite);

            // You should dispose a service provider created using ASP.NET
            // when you are finished using it, at the end of your app's lifetime.
            // If you use another dependency injection framework, you should inspect
            // its documentation for the best way to do this.
            using (var services = ConfigureServices())
            {
            using (var client = services.GetRequiredService<DiscordSocketClient>())
            {
                client.Log += LogAsync;
                services.GetRequiredService<CommandService>().Log += LogAsync;

                // Tokens should be considered secret data and never hard-coded.
                // We can read from the environment variable to avoid hardcoding.
                await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("token"));
                await client.StartAsync();

                // Here we initialize the logic required to register our commands.
                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

                await Task.Delay(Timeout.Infinite);
            }
            }
        }

        private Task LogAsync(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>   ()
                .AddSingleton<CommandService>        ()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>            ()
                .AddSingleton<PictureService>        ()
                .AddSingleton<StorageService>        ()
                .BuildServiceProvider();
        }

        //public async Task RegisterCommandsAsync()
        //{
        //    _client.MessageReceived += HandleCommandAsync;
        //    //await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        //}

        private async Task HandleCommandAsync(SocketMessage args)
        {
            //var message = args as SocketUserMessage;
            //var context = new SocketCommandContext(_client, message);
            //if (message.Author.IsBot) return;

            // --- Mention a user by name ------------------------------------

            //await Task.Delay(3000);
            //await message.Channel.SendMessageAsync(String.Format(
            //    "I'm sorry {0}, did you say something?",
            //    message.Author.Mention));

            // --- Edit message of User (FAILED) -----------------------------

            //var lastChan = message.Channel;
            //await lastChan.SendMessageAsync(
            //    "🤖 Let me fix that for you...");
            //// Can't do this, only author of message can edit:
            //await message.ModifyAsync( x => x.Content = message.Content.Replace("o","0") );

            // --- Delete message of User ------------------------------------

            //var lastChan = message.Channel;
            //await lastChan.SendMessageAsync(String.Format(
            //    "I'm sorry {0}, I'm afraid I can't allow you to say `{1}`.",
            //    context.Guild.GetUser(message.Author.Id).Nickname,
            //    message.Content));
            //await message.DeleteAsync();

            // --- Infodump about User ---------------------------------------

            //var lastChan = message.Channel;
            //var sb = new StringBuilder();
            //foreach (var pair in new List<(string,string)> {
            //    ( "Username", message.Author.Username ),
            //    ( "AvatarId", message.Author.AvatarId ),
            //    ( "Id"      , message.Author.Id.ToString() ),
            //    ( "Nickname", context.Guild.GetUser(message.Author.Id).Nickname )
            //})
            //{
            //    sb.Append(pair.Item1);
            //    sb.Append(": ");
            //    sb.Append(pair.Item2);
            //    sb.Append("\n");
            //}
            //await lastChan.SendMessageAsync(sb.ToString());

            // --- Always React with Bot Emoji -------------------------------

            //await message.AddReactionAsync(new Emoji("🤖"));

            // --- Original Implementation -----------------------------------

            //int argPos = 0;
            //if (message.HasStringPrefix("!", ref argPos))
            //{
            //    var result = await _commands.ExecuteAsync(
            //        context, argPos, _services);
            //    if (!result.IsSuccess) Console.WriteLine(result.ErrorReason);
            //}
        }
    }
}
