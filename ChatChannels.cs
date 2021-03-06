﻿using System.Collections.Generic;
using TerrariaApi.Server;
using System.Reflection;
using TShockAPI.Hooks;
using System.Linq;
using ChatChannels.Hooks;
using TShockAPI;
using Terraria;
using System;
using System.IO;

//TODO: FIX HOW CHANNELS DEAL WITH USERMODES

namespace ChatChannels
{
    [ApiVersion(1, 22)]
    public class ChatChannels : TerrariaPlugin
	{
		public Config Config = new Config();
		public ChatChannelsManager ChannelManager;
		public string SavePath = Path.Combine(TShock.SavePath, "Channels");
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
			if (!Directory.Exists(SavePath))
			{
				Directory.CreateDirectory(SavePath);
			}
			if (!File.Exists(ConfigPath))
			{
				Config.Write(ConfigPath);
			}
			Config.Read(ConfigPath);

			TShock.Initialized += TShock_Initialized;

            ServerApi.Hooks.ServerChat.Register(this, OnChat, 5);
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
            Order = -1;
        }

        void PlayerHooks_PlayerPostLogin(PlayerPostLoginEventArgs e)
        {
			ChannelUser cu = new ChannelUser(e.Player.User.Name);
			ChannelManager.BuildUserRelations(cu);
        }

        void OnLeave(LeaveEventArgs e)
        {
			TSPlayer player = TShock.Players[e.Who];
			if (player == null || !player.IsLoggedIn)
			{
				return;
			}

			ChannelManager.DestructUser(TShock.Players[e.Who]);
        }

        void OnChat(ServerChatEventArgs args)
        {
			if (args.Handled)
			{
				return;
			}

			if (args.Text.StartsWith(TShock.Config.CommandSpecifier)
				|| args.Text.StartsWith(TShock.Config.CommandSilentSpecifier))
			{
				return;
			}

			TSPlayer player = TShock.Players[args.Who];
			if (player == null)
			{
				return;
			}

			if (string.IsNullOrEmpty(args.Text))
			{
				return;
			}

			ChannelUser user = ChannelManager.GetUser(player.User.Name);
			if (user == null)
			{
				return;
			}

			if (args.Text[0] == Config.ChatChannelSwitcherCharacter)
			{
				string switcher = args.Text.Substring(1, args.Text.IndexOf(' ') - 1);
                user.SetActiveChannel(switcher);
				args.Handled = user.SendMessageToActiveChannel(player, args.Text.Remove(0, switcher.Length + 2));
			}
			else
			{
				args.Handled = user.SendMessageToActiveChannel(player, args.Text);
			}
        }

        static string[] HelpMsg = new string[]
        {
            "/channel create <name> <colour (eg 255,255,255)> [short name] - create a new channel",
            "/channel join <name> - join an existing channel.",
            "/channel leave - leave your current channel.",
			"/channel setmode <channel> <modes>.",
			"/channel setmode <channel> <user> <modes>."
        };

        void ChannelCmd(CommandArgs args)
        {
            string cmd = args.Parameters.Count > 0 ? args.Parameters[0].ToLower() : "help";
			if (!args.Player.IsLoggedIn)
			{
				args.Player.SendErrorMessage("You must be logged in to use this command.");
				return;
			}

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

						Channel c;
						ErrorCode code = ChannelManager.CreateChannel(name, shortName, colour, "p", out c);
						if (code != ErrorCode.Success)
						{
							args.Player.SendErrorMessage($"Unable to create channel: Error code {code}.");
                            return;
						}
						args.Player.SendSuccessMessage($"Created channel '{name}'.");
						code = ChannelManager.JoinUserToChannel(c, args.Player);
						if (code != ErrorCode.Success)
						{
							args.Player.SendErrorMessage($"Failed to auto-join you to channel '{name}': Error code {code}.");
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
						args.Player.SendSuccessMessage($"Joined channel '{name}'.");
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
						args.Player.SendSuccessMessage($"Left channel '{name}'.");
					}
                    break;
				#endregion leave

				#region set mode

				case "setmode":
					{
						if (args.Parameters.Count < 3)
						{
							args.Player.SendErrorMessage("Invalid syntax! proper syntax: /channel setmode <channel> <mode>");
							return;
						}

						Channel channel = ChannelManager.GetChannel(args.Parameters[1]);
						if (channel == null)
						{
							args.Player.SendErrorMessage($"Unable to find channel '{args.Parameters[0]}'.");
							return;
						}
						
						if (args.Parameters.Count > 3)
						{
							ChannelUser user = ChannelManager.GetUser(args.Parameters[2]);
							if (user == null)
							{
								//Setting modes on non-online players will not work! FIX
								args.Player.SendErrorMessage($"Unable to find user '{args.Parameters[1]}'.");
								return;
							}

							user.Modes.ModifyModes(args.Parameters[3]);
							ErrorCode code = ChannelManager.SetUserModes(user, channel);
							if (code != ErrorCode.Success)
							{
								args.Player.SendErrorMessage($"Failed to set new modes for user '{user.Name}' on channel '{channel.Name}': {code}.");
								return;
							}
							args.Player.SendSuccessMessage($"Successfully changed user modes for channel user '{user.Name}'.");
						}
						else
						{
							channel.Modes.ModifyModes(args.Parameters[2]);
							ErrorCode code = ChannelManager.SetChannelModes(channel);
							if (code != ErrorCode.Success)
							{
								args.Player.SendErrorMessage($"Failed to set new modes on channel '{channel.Name}': {code}.");
								return;
							}
							args.Player.SendSuccessMessage($"Successfully changed channel modes for channel '{channel.Name}'.");
						}
						break;
					}

				#endregion

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
                                FooterFormat = "Type /channel help {0} for more.",
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
