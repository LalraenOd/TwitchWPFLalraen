using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using TwitchLib.Api;
using TwitchLib.Api.V5.Models.Users;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace TwitchWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotUserName, TwitchInfo.BotTokenAccess);
        public TwitchClient client =  new TwitchClient();
        private static TwitchAPI twAPI = new TwitchAPI();


        private void InitializeWindows()
        {
            RichTextBox_Chat.IsReadOnly = true;
            RichTextBox_Log.IsReadOnly = true;
            TextBlock_StreamStatus.Foreground = Brushes.Red;
            TextBlock_Followers.Foreground = Brushes.Red;
            TextBlock_Viewers.Foreground = Brushes.Red;
            TextBlock_FPS.Foreground = Brushes.Red;
            RichTextBox_Chat.TextChanged += RichTextBox_Chat_TextChanged;
            RichTextBox_Log.TextChanged += RichTextBox_Log_TextChanged;
        }
        public MainWindow()
        {
            InitializeComponent();
            InitializeWindows();

            client.Initialize(credentials, TwitchInfo.ChannelName);

            twAPI.Settings.ClientId = TwitchInfo.ClientId;
            twAPI.Settings.AccessToken = TwitchInfo.BotTokenAccess;

            //Events
            client.OnLog += Client_OnLog;
            client.OnConnected += Client_OnConnected;
            client.OnConnectionError += Client_OnConnectionError;
            client.OnMessageReceived += Client_OnMessageReceived;
        }

        private void RichTextBox_Log_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            RichTextBox_Log.ScrollToEnd();
        }

        private void RichTextBox_Chat_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            RichTextBox_Chat.ScrollToEnd();
        }

        private void Client_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            RichTextBox_Log_Append(e.Data.ToString());
        }

        private void Client_OnMessageReceived(object sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.IsBroadcaster)
            {
                RichTextBox_Chat_Append($"{e.ChatMessage.Username}: {e.ChatMessage.Message.ToString()}");
            }
            else if (e.ChatMessage.IsModerator)
            {
                RichTextBox_Chat_Append($"{e.ChatMessage.Username}: {e.ChatMessage.Message.ToString()}");
            }
            else
            {
                RichTextBox_Chat_Append($"{e.ChatMessage.Username}: {e.ChatMessage.Message.ToString()}");
            }

            switch (e.ChatMessage.Message.ToString().ToLower())
            {
                case "!uptime":
                case "!гзешьу":
                    client.SendMessage(TwitchInfo.ChannelName, "Стрим идет:" + GetUptime()?.ToString(format: @"hh\:mm")
                        ?? $"{TwitchInfo.ChannelName} is offline");
                    RichTextBox_Chat_Append("Стрим идет:" + GetUptime()?.ToString(format: @"hh\:mm")
                        ?? $"{TwitchInfo.BotUserName}: {TwitchInfo.ChannelName} is offline");
                    break;
                case "!game":
                case "!игра":
                    GetGame();
                    break;
                case "!botinfo":
                    client.SendMessage(TwitchInfo.ChannelName, "Twitch Bot and desktop app developed by Lalraen using C#, WPF, TwitchLib.");
                    RichTextBox_Chat_Append($"{TwitchInfo.BotUserName}: Twitch Bot and desktop app developed by Lalraen using C#, WPF, TwitchLib.");
                    break;
                case "!disable":
                    
                    break;
            }
        }

        private void Client_OnConnected(object sender, TwitchLib.Client.Events.OnConnectedArgs e)
        {
            client.SendMessage(TwitchInfo.ChannelName, $"Всем доброго времени суток! Идет подготовка к стриму.");
            RichTextBox_Chat_Append($"{TwitchInfo.BotUserName}: Всем доброго времени суток! Идет подготовка к стриму.");
            RichTextBox_Log_Append("BOT CONNECTED!!!!");

            Thread thread_GetUpdateFollowers = new Thread(GetUpdateFollowers);
            thread_GetUpdateFollowers.Start();
            Thread thread_GetUpdateStreamStatus = new Thread(GetUpdateStreamStatus);
            thread_GetUpdateStreamStatus.Start();
        }

        private void Client_OnConnectionError(object sender, TwitchLib.Client.Events.OnConnectionErrorArgs e)
        {

        }

        private async void GetUpdateStreamStatus()
        {
            while (true)
            {
                Thread.Sleep(5000);
                string userId = GetUserId(TwitchInfo.ChannelName);
                var streamOnline = await twAPI.V5.Streams.BroadcasterOnlineAsync(userId);
                if (streamOnline)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TextBlock_StreamStatus.Text = "Online";
                    });
                    GetUpdateViewers();
                    GetUpdateFPS();
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        TextBlock_StreamStatus.Text = "Offline";
                    });
                }
            }
        }

        private async void GetUpdateViewers()
        {
            string userId = GetUserId(TwitchInfo.ChannelName);
            var stream = await twAPI.V5.Streams.GetStreamByUserAsync(userId);
            var viewers = stream.Stream.Viewers.ToString();
            Dispatcher.Invoke(() =>
            {
                TextBlock_Viewers.Text = viewers;
            });
        }

        private async void GetUpdateFollowers()
        {
            while (true)
            {
                Thread.Sleep(5000);
                string userId = GetUserId(TwitchInfo.ChannelName);
                var followers = await twAPI.V5.Channels.GetChannelFollowersAsync(userId);
                Dispatcher.Invoke(() =>
                {
                    TextBlock_Followers.Text = $"Followers: {followers.Total.ToString()}";
                });
            }
        }

        private async void GetUpdateFPS()
        {
            try
            {
                string userId = GetUserId(TwitchInfo.ChannelName);
                var stream = await twAPI.V5.Streams.GetStreamByUserAsync(userId);
                var frameRate = stream.Stream.AverageFps.ToString();
                Dispatcher.Invoke(() =>
                {
                    TextBlock_FPS.Text = frameRate;
                });
            }
            catch (NullReferenceException e)
            {
                Dispatcher.Invoke(() =>
                {
                    TextBlock_FPS.Text = "Offline";
                });
            }
        }

        private void GetUpdateStreamName()
        {
            try
            {
                string userId = GetUserId(TwitchInfo.ChannelName);
                string streamName = "";
                Dispatcher.Invoke(() =>
                {
                    TextBlock_StreamName.Text = streamName;
                });
            }
            catch (NullReferenceException e)
            {
                Dispatcher.Invoke(() =>
                {
                    TextBlock_StreamName.Text = "Offline";
                });
            }
        }

        private async void GetGame()
        {
            string userId = GetUserId(TwitchInfo.ChannelName);
            var stream = await twAPI.V5.Streams.GetStreamByUserAsync(userId);
            client.SendMessage(TwitchInfo.ChannelName, $"Сейчас играем в {stream.Stream.Game.ToString()}");
            Dispatcher.Invoke(() =>
            {
                RichTextBox_Chat.AppendText($"{TwitchInfo.BotUserName}: Сейчас играем в {stream.Stream.Game.ToString()}");
            });
        }

        private string GetUserId(string username)
        {
            User[] userlist = twAPI.V5.Users.GetUserByNameAsync(username).Result.Matches;
            if (userlist == null || userlist.Length == 0)
            {
                return null;
            }
            return userlist[0].Id;
        }

        TimeSpan? GetUptime()
        {
            string userId = GetUserId(TwitchInfo.ChannelName);
            if (userId == null)
            {
                return null;
            }
            return twAPI.V5.Streams.GetUptimeAsync(userId).Result;
        }
 
        private void Button_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Btn_Connect_Click(object sender, RoutedEventArgs e)
        {
            client.Connect();
        }

        private void Btn_Disconnect_Click(object sender, RoutedEventArgs e)
        {
            client.SendMessage(TwitchInfo.ChannelName, "Всем спасибо, что пришли.. но пора и отдохнуть.. УДАЧИ!");
            RichTextBox_Chat_Append($"{TwitchInfo.BotUserName}: Всем спасибо, что пришли.. но пора и отдохнуть.. УДАЧИ!");
            client.Disconnect();            
        }

        private void RichTextBox_Chat_Append(string textToAppend)
        {
            Dispatcher.Invoke(() =>
            {
                if (client.IsConnected)
                    RichTextBox_Chat.AppendText(textToAppend + "\n");
                else
                    RichTextBox_Chat.AppendText("<< DISCONNECTED >>");
            });
        }

        private void RichTextBox_Log_Append(string textToAppend)
        {
            Dispatcher.Invoke(() =>
            {
                RichTextBox_Log.AppendText(textToAppend + "\n");
            });
        }
    }
}
