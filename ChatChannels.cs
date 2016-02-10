using System.Collections.Generic;
using TerrariaApi.Server;
using System.Reflection;
using TShockAPI.Hooks;
using System.Linq;
using ChatChannels.Hooks;
using TShockAPI;
using Terraria;
using System;
using System.IO;

namespace ChatChannels
{
    [ApiVersion(1, 22)]
    public class ChatChannels : TerrariaPlugin
	{
		public Config Config = new Config();
		public ChatChannelsManager ChannelManager;
		public string ConfigPath = Path.Combine(TShock.SavePath, "Channels", "ChatChannelsConfig.json");

		public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
        public override string Author
        {
            get { return "Jewsus Updates & Co"; }
        }
        public override string Name
        {
            get { return "Chat++"; }
        }

        public override string Description
        {
            get { return "Adds chat channels!"; }
        }

        public override void Initialize()
        {
			if (!File.Exists(ConfigPath))
			{
				Config.Write(ConfigPath);
			}
			Config.Read(ConfigPath);

			TShock.Initialized += TShock_Initialized;

            ServerApi.Hooks.ServerChat.Register(this, OnChat);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            PlayerHooks.PlayerPostLogin += PlayerHooks_PlayerPostLogin;

            Commands.ChatCommands.Add(new Command(Permission.Use, ChannelCmd, "chan", "channel"));
        }

		private void TShock_Initialized()
		{
			ChannelManager = new ChatChannelsManager();
			ChannelManager.Initialize();

			ChannelHooks.OnChannelCreate += ChannelHooks_ChannelCreated;
			ChannelHooks.OnChannelJoin += ChannelHooks_ChannelJoin;
			ChannelHooks.OnChanneLeave += ChannelHooks_ChannelLeave;
			ChannelHooks.OnChannelRemove += ChannelHooks_ChannelRemoved;
		}

        protected override void Dispose(bool disposing)
        {
            if (disposing)
			{
				PlayerHooks.PlayerPostLogin -= PlayerHooks_PlayerPostLogin;
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
				ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
            }
            base.Dispose(disposing);
        }

        public ChatChannels(Main game)
            : base(game)
        {
            Order = 1;
        }

        void PlayerHooks_PlayerPostLogin(PlayerPostLoginEventArgs e)
        {

        }

        void OnLeave(LeaveEventArgs e)
        {
        }

        void OnChat(ServerChatEventArgs args)
        {
        }

        static string[] HelpMsg = new string[]
        {
            "/channel <message> - talk in your channels's chat.",
            "/channel create <name> <colour (eg 255,255,255)> [short name] - create a new channel",
            "/channel join <name> - join an existing channel.",
            "/channel leave - leave your current channel.",
            "/channel reloadchannels - reload all channels and their members.",
            "/channel reloadconfig - reload the channels configuration file.",
            "/channel list - list all existing channels.",
            "/channel setcolor <r, g, b> - change the channel's color.",
            "/channel who - list all online members in your current channel.",
            "/channel find <player> - find out which channels a player is in.",
            "/channel rename <new name> - change your channel's name."

            /*"/chan invite <name> - will invite a player to your channel|guild.",
            "/chan acceptinvite - join a channel|guild you were invited to.",
            "/chan denyinvite - deny a pending channel|guild invitation.",
            "/chan tpall - teleport all channel|guild members to you.",    
            "/chan ban <player> - will ban a player from your channel|guild by Ip-Address.",
            "/chan unban <player> - will unban a player from your channel|guild (if he was banned).",
            "/chan kick <player> - will kick a player out of your channel|guild.",*/
        };

        void ChannelCmd(CommandArgs args)
        {
            string cmd = args.Parameters.Count > 0 ? args.Parameters[0].ToLower() : "help";

            switch (cmd)
            {
                #region create
                case "create":
                    {
                        if (!args.Player.Group.HasPermission(Permission.Create))
                        {
                            args.Player.SendErrorMessage("You do not have permission to create a channel!");
                            return;
                        }
                        if (args.Parameters.Count < 3)
                        {
                            args.Player.SendErrorMessage("Invalid syntax! proper syntax: /channel create <name> <colour (eg 255,255,255)> [short name]");
                            return;
                        }
						string name = args.Parameters[1];
						string colour = args.Parameters[2];
						string shortName = null;

						if (args.Parameters.Count > 3)
						{
							shortName = args.Parameters[3];
						}

						ErrorCode code = ChannelManager.CreateChannel(name, shortName, colour, "+p");
						if (code != ErrorCode.Success)
						{
							args.Player.SendErrorMessage($"Unable to create channel: Error code {code}.");
                            return;
						}
                    }
                    break;
                #endregion create

                #region join
                case "join":
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("Invalid syntax! proper syntax: /channel join <channel name>");
                            return;
                        }
                        string name = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
						Channel c = ChannelManager.GetChannel(name);
						if (c == null)
						{
							args.Player.SendErrorMessage($"Channel with name '{name}' not found.");
							return;
						}
						ErrorCode code = ChannelManager.JoinUserToChannel(c, args.Player);
						if (code != ErrorCode.Success)
						{
							args.Player.SendErrorMessage($"Unable to join channel: Error code {code}.");
							return;
						}
                    }
                    break;
                #endregion join

                #region leave
                case "leave":
                    {
						if (args.Parameters.Count < 2)
						{
							args.Player.SendErrorMessage("Invalid syntax! proper syntax: /channel leave <channel name>");
							return;
						}
						string name = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
						Channel c = ChannelManager.GetChannel(name);
						if (c == null)
						{
							args.Player.SendErrorMessage($"Channel with name '{name}' not found.");
							return;
						}
						ErrorCode code = ChannelManager.RemoveUserFromChannel(c, args.Player);
						if (code != ErrorCode.Success)
						{
							args.Player.SendErrorMessage($"Unable to leave channel: Error code {code}.");
							return;
						}
					}
                    break;
                #endregion leave

                #region help
                default:
                case "help":
                    {
                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                            return;

                        PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(HelpMsg),
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "Channels help page ({0}/{1})",
                                FooterFormat = "Type /chan help {0} for more.",
                            });
                    }
                    break;
                    #endregion help
            }
        }

        void ChannelHooks_ChannelCreated(ChannelEventArgs e)
        {
            e.TSPlayer.SendSuccessMessage(string.Format("Your channel ({0}) has been successfully created!", e.Channel.Name));
            TSPlayer.All.SendInfoMessage(string.Format("{0} has created a new channel: {1}.", e.TSPlayer.Name, e.Channel.Name));
        }

        void ChannelHooks_ChannelJoin(ChannelEventArgs e)
        {
            e.Channel.Broadcast("{0} has joined the channel!", e.TSPlayer.Name);
        }

        void ChannelHooks_ChannelRemoved(ChannelEventArgs e)
        {
            e.Channel.Broadcast("The channel has been closed!");
        }

        void ChannelHooks_ChannelLeave(ChannelEventArgs e)
        {
            e.Channel.Broadcast(e.TSPlayer.Name + " has left the channel");
            e.TSPlayer.SendInfoMessage($"You have left {e.Channel.Name}");
        }
    }
}
