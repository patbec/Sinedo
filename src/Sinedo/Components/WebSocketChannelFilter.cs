using System;
using System.Collections.Generic;
using System.Linq;
using Sinedo.Flags;

namespace Sinedo.Components
{
    public class WebSocketChannelFilter
    {
        public WebSocketChannel[] Channels { get; }
        public CommandFromServer[] CommandsServer { get; }
        public CommandFromClient[] CommandsClient { get; }


        public WebSocketChannelFilter(WebSocketChannel[] webSocketChannels)
        {
            Channels = webSocketChannels ?? throw new ArgumentNullException(nameof(webSocketChannels));

            HashSet<CommandFromServer> server = new();
            HashSet<CommandFromClient> client = new();

            foreach (var channel in webSocketChannels)
            {
                switch (channel)
                {
                    case WebSocketChannel.Notification:
                        {
                            server.Add(CommandFromServer.Error);
                            server.Add(CommandFromServer.Notification);

                            break;
                        }
                    case WebSocketChannel.Downloads:
                        {
                            server.Add(CommandFromServer.DownloadAdded);
                            server.Add(CommandFromServer.DownloadRemoved);
                            server.Add(CommandFromServer.DownloadChanged);
                            server.Add(CommandFromServer.Setup);

                            client.Add(CommandFromClient.Start);
                            client.Add(CommandFromClient.Stop);
                            client.Add(CommandFromClient.Delete);
                            client.Add(CommandFromClient.StartAll);
                            client.Add(CommandFromClient.StopAll);
                            client.Add(CommandFromClient.FileUpload);

                            break;
                        }
                    case WebSocketChannel.Bandwidth:
                        {
                            server.Add(CommandFromServer.Bandwidth);
                            server.Add(CommandFromServer.Setup);

                            break;
                        }
                    case WebSocketChannel.Disk:
                        {
                            server.Add(CommandFromServer.Disk);
                            server.Add(CommandFromServer.Setup);

                            break;
                        }
                    case WebSocketChannel.Links:
                        {
                            server.Add(CommandFromServer.Links);
                            server.Add(CommandFromServer.Setup);

                            client.Add(CommandFromClient.Links);

                            break;
                        }
                    case WebSocketChannel.Settings:
                        {
                            server.Add(CommandFromServer.Setup);

                            client.Add(CommandFromClient.Restart);

                            throw new NotImplementedException();
                        }
                    case WebSocketChannel.Logs:
                        {
                            server.Add(CommandFromServer.Setup);

                            throw new NotImplementedException();
                        }
                    default:
                        {
                            throw new InvalidOperationException("WebSocketChannelFilter does not support a specified command.");
                        }
                }
            }

            CommandsServer = server.ToArray();
            CommandsClient = client.ToArray();
        }

        public bool IsChannelSupported(WebSocketChannel channel)
        {
            return Channels.Contains(channel);
        }

        public bool IsCommandSupported(CommandFromServer command)
        {
            return CommandsServer.Contains(command);
        }

        public bool IsCommandSupported(CommandFromClient command)
        {
            return CommandsClient.Contains(command);
        }
    }
}
