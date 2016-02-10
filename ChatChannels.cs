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
		public UpdateChecker UpdateChecker;
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
            Commands.ChatCommands.Add(new Command(Permission.Chat, Chat, "ch") { AllowServer = false });
        }

		private void TShock_Initialized()
		{
			UpdateChecker = new UpdateChecker();
			UpdateChecker.CheckForUpdate();
			if (UpdateChecker.UpdateAvailable)
			{
				TShock.Log.ConsoleInfo("An update is available for the Chat++ plugin!");
				TShock.Log.ConsoleInfo("Type /chan changelog to see the changelog!");
			}

			ChannelManager = new ChatChannelsManager();
			ChannelManager.Initialize();

			ChannelHooks.OnChannelCreate += ChannelHooks_ChannelCreated;
			ChannelHooks.OnChannelLogin += ChannelHooks_ChannelLogin;
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
            if (ChatChannelsManager.ChatMember.ContainsKey(e.Player.Index))
                ChatChannelsManager.UnLoadMember(e.Player);
            ChatChannelsManager.LoadMember(e.Player);
        }

        void OnLeave(LeaveEventArgs e)
        {
            ChatChannelsManager.UnLoadMember(TShock.Players[e.Who]);
        }

        void OnChat(ServerChatEventArgs args)
        {
            TSPlayer ts = TShock.Players[args.Who];
            if (ts.mute || !ts.IsLoggedIn)
                return;

            Channel MyChannel = ChatChannelsManager.FindChannelByPlayer(ts);
            if (MyChannel == null)
                return;

            if (args.Text.StartsWith(TShock.Config.CommandSpecifier))
                return;

            if (!ts.Group.HasPermission(Permission.Chat) || !ts.Group.HasPermission(Permission.Use))
            {
                MyChannel.OnlineChannelMembers[ts.Index].DefaultToChannelChat = false;
                return;
            }

            if (MyChannel.OnlineChannelMembers[ts.Index].DefaultToChannelChat)
            {
                args.Handled = true;
                MyChannel.Broadcast(string.Format("[channel] {0} - {1}: {2}", MyChannel.Name, ts.Name, string.Join(" ", args.Text)));
            }
        }

        static string[] HelpMsg = new string[]
        {
            "/chan <message> - talk in your channels's chat.",
            "/chan checkupdate - checks for available updates.",
            "/chan changelog - shows the changelog if a new update is available.",
            "/chan create <name> - create a new channel with you as leader.",
            "/chan join <name> - join an existing channel.",
            "/chan leave - leave your current channel.",
            "/chan reloadchannels - reload all channels and their members.",
            "/chan reloadconfig - reload the channels configuration file.",
            "/chan invitemode <true/false> - toggle invite-only mode.",
            "/chan list - list all existing channels.",
            "/chan tp - teleport to the guild's spawnpoint.",
            "/chan setspawn - set the guild's spawnpoint to your current location.",
            "/chan setcolor <r, g, b> - change the channel|guild's color.",
            "/chan who - list all online members in your current channel|guild.",
            "/chan find <player> - find out which channel|guilds a player is in.",
            "/chan togglechat - toggle auto-talking in channel|guildchat instead of global chat.",
            "/chan rename <new name> - change your channel|guild's name."

            /*"/chan invite <name> - will invite a player to your channel|guild.",
            "/chan acceptinvite - join a channel|guild you were invited to.",
            "/chan denyinvite - deny a pending channel|guild invitation.",
            "/chan tpall - teleport all channel|guild members to you.",    
            "/chan ban <player> - will ban a player from your channel|guild by Ip-Address.",
            "/chan unban <player> - will unban a player from your channel|guild (if he was banned).",
            "/chan kick <player> - will kick a player out of your channel|guild.",*/
        };

        void Chat(CommandArgs args)
        {
            Channel MyChannel = ChatChannelsManager.FindChannelByPlayer(args.Player);
            if (MyChannel == null)
            {
                args.Player.SendErrorMessage("You are not currently in a channel|guild!");
                return;
            }
            if (args.Player.mute)
            {
                args.Player.SendErrorMessage("You are muted!");
                return;
            }
            MyChannel.Broadcast(string.Format("[Channel] {0} - {1}: {2}", MyChannel.Name, args.Player.Name, string.Join(" ", args.Parameters)));
        }

        void ChannelCmd(CommandArgs args)
        {
            string cmd = args.Parameters.Count > 0 ? args.Parameters[0].ToLower() : "help";

            Channel MyChannel = null;

            if (args.Player != TSPlayer.Server)
                MyChannel = ChatChannelsManager.FindChannelByPlayer(args.Player);

            switch (cmd)
            {
                #region checkupdate
                case "checkupdate":
                    {
                        if (!args.Player.Group.HasPermission(Permission.Create))
                        {
                            args.Player.SendErrorMessage("You do not have permission to check for updates!");
                            return;
                        }

                        if (!UpdateChecker.UpdateAvailable)
                            UpdateChecker.CheckForUpdate();

                        if (UpdateChecker.UpdateAvailable)
                        {
                            args.Player.SendInfoMessage("An update is available for the Chat++ plugin!");
                            args.Player.SendInfoMessage("Type /chan changelog to see the changelog!");
                        }
                        args.Player.SendErrorMessage("No update available!");
                    }
                    break;
                #endregion checkupdate

                #region changelog
                case "changelog":
                    {
                        if (!UpdateChecker.UpdateAvailable)
                        {
                            args.Player.SendErrorMessage("There is no update available! Type \"/chan checkupdate\" to check for updates!");
                            return;
                        }
                        args.Player.SendSuccessMessage("Changelog for the latest version (" + UpdateChecker.NewVersion + "):");
                        for (int i = 0; i < UpdateChecker.ChangeLog.Length; i++)
                            args.Player.SendInfoMessage(UpdateChecker.ChangeLog[i]);
                    }
                    break;
                #endregion changelog

                #region create
                case "create":
                    {
                        if (!args.Player.Group.HasPermission(Permission.Create))
                        {
                            args.Player.SendErrorMessage("You do not have permission to create a channel|guild!");
                            return;
                        }
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("Invalid syntax! proper syntax: /chan create <name>");
                            return;
                        }
                        if (ChatChannelsManager.Config.MaxNumberOfChannels > 0 && ChatChannelsManager.Channels.Keys.Count >= ChatChannelsManager.Config.MaxNumberOfChannels)
                        {
                            args.Player.SendErrorMessage("The maximum amount of channel|guilds has been reached.");
                            return;
                        }
                        string name = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                        if (MyChannel != null)
                        {
                            args.Player.SendErrorMessage("You are already in a channel|guild!");
                            return;
                        }
                        if (ChatChannelsManager.FindChannelByName(name) != null)
                        {
                            args.Player.SendErrorMessage("This channel|guild already exists!");
                            return;
                        }
                        if (!ChatChannelsManager.CreateChannel(args.Player, new Channel() { Name = name, Owner = args.Player.User.Name }))
                            args.Player.SendErrorMessage("Something went wrong! Please contact an administrator.");
                    }
                    break;
                #endregion create

                #region join
                case "join":
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("Invalid syntax! proper syntax: /chan join <channel name>");
                            return;
                        }
                        string name = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                        if (MyChannel != null)
                        {
                            args.Player.SendErrorMessage("You are already in a channel|guild!");
                            return;
                        }
                        Channel c = ChatChannelsManager.FindChannelByName(name);
                        if (c == null)
                        {
                            args.Player.SendErrorMessage("This channel|guild does not exists!");
                            return;
                        }
                        if (c.IsBanned(args.Player.User.Name))
                        {
                            args.Player.SendErrorMessage("You have been banned from this channel|guild!");
                            return;
                        }
                        if (c.InviteMode == InviteMode.True)
                        {
                            args.Player.SendErrorMessage("This channel|guild is in invite-only mode, please ask for an invitation.");
                            return;
                        }
                        ChatChannelsManager.JoinChan(args.Player, c);
                    }
                    break;
                #endregion join

                #region leave
                case "leave":
                    {
                        if (MyChannel == null)
                        {
                            args.Player.SendErrorMessage("You are not in a channel|guild!");
                            return;
                        }
                        if (args.Parameters.Count == 2)
                        {
                            if (args.Parameters[1].ToLower() == "confirm")
                                ChatChannelsManager.LeaveChannel(args.Player, MyChannel);
                            else
                                args.Player.SendErrorMessage("Invalid syntax! proper syntax: /chan leave confirm");
                        }
                        else
                        {
                            if (args.Player.User.Name == MyChannel.Owner)
                                args.Player.SendErrorMessage("You are the owner of this channel|guild, this means that if you leave, the channel|guild will disband!");
                            args.Player.SendInfoMessage("Are you sure you want to leave this channel|guild? type \"/chan leave confirm\"");
                        }
                    }
                    break;
                #endregion leave

                #region inviteMode
                case "invitemode":
                    {
                        if (MyChannel == null)
                        {
                            args.Player.SendErrorMessage("You are not in a channel|guild!");
                            return;
                        }
                        if (MyChannel.Owner != args.Player.User.Name)
                        {
                            args.Player.SendErrorMessage("You are not allowed to alter the channel|guild's invitemode settings!");
                            return;
                        }
                        string subcmd = args.Parameters.Count == 2 ? args.Parameters[1].ToLower() : string.Empty;
                        switch (subcmd)
                        {
                            case "true":
                                ChatChannelsManager.SetInviteMode(MyChannel, InviteMode.True);
                                break;
                            case "false":
                                ChatChannelsManager.SetInviteMode(MyChannel, InviteMode.False);
                                break;
                            default:
                                args.Player.SendErrorMessage("Invalid syntax! proper syntax: /chan invitemode <true/false>");
                                return;
                        }
                        args.Player.SendInfoMessage("Channel|guild invite mode has been set to " + (MyChannel.InviteMode == InviteMode.True ? "true" : "false"));
                    }
                    break;
                #endregion inviteMode

                #region reloadchannels
                case "reloadchannels":
                    {
                        if (!args.Player.Group.HasPermission(Permission.Reload))
                        {
                            args.Player.SendErrorMessage("You do not have permission to create a channel|guild!");
                            return;
                        }
                        ChatChannelsManager.ReloadAll();
                        args.Player.SendInfoMessage("All channels|guilds and their members have been reloaded!");
                        break;
                    }
                #endregion reloadchannels

                #region reloadconfig
                case "reloadconfig":
                    {
                        if (!args.Player.Group.HasPermission(Permission.Reload))
                        {
                            args.Player.SendErrorMessage("You do not have permission to create a channel|guild!");
                            return;
                        }
                        ChatChannelsManager.ReloadConfig(args.Player);
                    }
                    break;
                #endregion reloadconfig

                #region list
                case "list":
                    {
                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                            return;
                        IEnumerable<string> ChannelNames = ChatChannelsManager.Channels.Keys;
                        PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(ChannelNames),
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "Channels ({0}/{1}):",
                                FooterFormat = "Type /chan list {0} for more.",
                                NothingToDisplayString = "There aren't any channels|guilds!",
                            });
                    }
                    break;
                #endregion list

                #region tp
                case "tp":
                    {
                        if (MyChannel == null)
                        {
                            args.Player.SendErrorMessage("You are not in a guild!");
                            return;
                        }
                        if (MyChannel.TileX == 0 || MyChannel.TileY == 0)
                        {
                            args.Player.SendErrorMessage("Your guild has no spawn point defined!");
                            return;
                        }
                        args.Player.Teleport(MyChannel.TileX * 16, MyChannel.TileY * 16);
                    }
                    break;
                #endregion tp

                #region setspawn
                case "setspawn":
                    {
                        if (MyChannel == null)
                        {
                            args.Player.SendErrorMessage("You are not in a guild!");
                            return;
                        }
                        if (MyChannel.Owner != args.Player.User.Name)
                        {
                            args.Player.SendErrorMessage("You are not allowed to alter the guild's spawnpoint!");
                            return;
                        }
                        ChatChannelsManager.SetSpawn(MyChannel, args.Player);
                        args.Player.SendInfoMessage(string.Format("Your guild's spawnpoint has been changed to X:{0}, Y:{1}", MyChannel.TileX, MyChannel.TileY));
                    }
                    break;
                #endregion setspawn

                #region setcolor
                case "setcolor":
                    {
                        if (MyChannel == null)
                        {
                            args.Player.SendErrorMessage("You are not in a channel|guild!");
                            return;
                        }
                        if (MyChannel.Owner != args.Player.User.Name)
                        {
                            args.Player.SendErrorMessage("You are not allowed to alter the channel|guild's chatcolor!");
                            return;
                        }
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("Invalid syntax! proper syntax: /chan setcolor  <0-255,0-255,0-255>");
                            return;
                        }
                        if (!ChatChannelsManager.SetColor(MyChannel, args.Parameters[1]))
                            args.Player.SendErrorMessage("Invalid color format! proper example: /chan setcolor 125,255,137");
                    }
                    break;
                #endregion setcolor

                #region who
                case "who":
                    {
                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                            return;
                        IEnumerable<string> Channelmembers = MyChannel.OnlineChannelMembers.Values.Select(m => m.TSPlayer.Name);
                        PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(Channelmembers),
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "Online Channelmembers ({0}/{1}):",
                                FooterFormat = "Type /chan who {0} for more.",
                            });
                    }
                    break;
                #endregion who

                #region find
                case "find":
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("Invalid syntax! proper syntax: /chan find <player>");
                            return;
                        }
                        var foundplr = TShock.Utils.FindPlayer(args.Parameters[1]);
                        if (foundplr.Count == 0)
                        {
                            args.Player.SendMessage("Invalid player!", Color.Red);
                            return;
                        }
                        else if (foundplr.Count > 1)
                        {
                            args.Player.SendMessage(string.Format("More than one ({0}) player matched!", foundplr.Count), Color.Red);
                            return;
                        }
                        TSPlayer plr = foundplr[0];
                        Channel c = ChatChannelsManager.FindChannelByPlayer(plr);
                        if (c == null)
                        {
                            args.Player.SendErrorMessage(string.Format("{0} is not in a channel|guild!", plr.Name));
                            return;
                        }
                        args.Player.SendInfoMessage(string.Format("{0} is in channel|guild: {1}", plr.Name, c.Name));
                    }
                    break;
                #endregion find

                #region togglechat
                case "togglechat":
                    {
                        MyChannel.OnlineChannelMembers[args.Player.Index].DefaultToChannelChat = !MyChannel.OnlineChannelMembers[args.Player.Index].DefaultToChannelChat;
                        args.Player.SendInfoMessage(MyChannel.OnlineChannelMembers[args.Player.Index].DefaultToChannelChat ? "You will now automaticly talk in channel|guild chat!" : "You are now using global chat, use /chan to talk in channels");
                    }
                    break;
                #endregion togglechat

                #region rename
                case "rename":
                    {
                        if (MyChannel == null)
                        {
                            args.Player.SendErrorMessage("You are not in a channel|guild!");
                            return;
                        }
                        if (MyChannel.Owner != args.Player.User.Name)
                        {
                            args.Player.SendErrorMessage("You are not allowed to alter the channel|guild's name!");
                            return;
                        }
                        string name = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                        if (ChatChannelsManager.FindChannelByName(name) != null)
                        {
                            args.Player.SendErrorMessage("A channel|guild with this name already exists!");
                            return;
                        }
                        ChatChannelsManager.Rename(MyChannel, args.Player, name);
                        MyChannel.Broadcast("The channel|guild has been renamed to " + MyChannel.Name);
                    }
                    break;
                #endregion rename

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

        void ChannelHooks_ChannelLogin(ChannelEventArgs e)
        {
            e.Channel.Broadcast(e.TSPlayer.Name + " has entered the channel!", e.TSPlayer.Index);
        }

        void ChannelHooks_ChannelJoin(ChannelEventArgs e)
        {
            e.Channel.Broadcast(string.Format("A new member ({0}) has joined the channel!", e.TSPlayer.Name), e.TSPlayer.Index);
            e.TSPlayer.SendInfoMessage("Welcome to the channel!");
        }

        void ChannelHooks_ChannelRemoved(ChannelEventArgs e)
        {
            e.Channel.Broadcast("The channel has been closed!");
            TSPlayer.All.SendInfoMessage(string.Format("Channel {0} has been closed!", e.Channel.Name));
        }

        void ChannelHooks_ChannelLeave(ChannelEventArgs e)
        {
            e.Channel.Broadcast(e.TSPlayer.Name + " has left the channel!", e.TSPlayer.Index);
            e.TSPlayer.SendInfoMessage("You have left the channel!");
        }
    }
}
