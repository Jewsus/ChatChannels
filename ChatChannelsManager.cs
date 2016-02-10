using System;
using System.Data;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using System.Linq;
using System.Text;
using TShockAPI;
using System.IO;
using TShockAPI.DB;
using ChatChannels.Hooks;
using Newtonsoft.Json;
using System.Net;
using MySql.Data.MySqlClient;
using System.Diagnostics;


namespace ChatChannels
{
    public static class ChatChannelsManager
    {
        static IDbConnection Database;
        public static Config Config = new Config();
        public static string SavePath = Path.Combine(TShock.SavePath, "Channels");
        public static Dictionary<string, Channel> Channels = new Dictionary<string, Channel>();
        public static Dictionary<int, string> ChatMember = new Dictionary<int, string>();

        public static void Initialize()
        {
            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);

            switch (TShock.Config.StorageType.ToLower())
            {
                case "mysql":
                    string[] host = TShock.Config.MySqlHost.Split(':');
                    Database = new MySqlConnection()
                    {
                        ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
                                host[0],
                                host.Length == 1 ? "3306" : host[1],
                                TShock.Config.MySqlDbName,
                                TShock.Config.MySqlUsername,
                                TShock.Config.MySqlPassword)
                    };
                    break;
                case "sqlite":
                    string sql = Path.Combine(SavePath, "Channels.sqlite");
                    Database = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
                    break;
            }
            SqlTableCreator SQLcreator = new SqlTableCreator(Database, Database.GetSqlType() == SqlType.Sqlite
            ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

            SqlTable[] tables = new SqlTable[]
            {
                 new SqlTable("Channels",
                     new SqlColumn("Name", MySqlDbType.VarChar) { Primary=true, Unique=true },
                     new SqlColumn("Owner", MySqlDbType.VarChar),
                     new SqlColumn("InviteMode", MySqlDbType.Int32),
                     new SqlColumn("TileX", MySqlDbType.Int32),
                     new SqlColumn("TileY", MySqlDbType.Int32),
                     new SqlColumn("ChatColor", MySqlDbType.VarChar),
                     new SqlColumn("Bans", MySqlDbType.VarChar)
                     ),
                 new SqlTable("ChatMember",
                     new SqlColumn("Username",MySqlDbType.VarChar) { Primary=true, Unique=true },
                     new SqlColumn("ChannelName", MySqlDbType.VarChar)
                     ),
                 new SqlTable("GuildWarps",
                     new SqlColumn("WarpName",MySqlDbType.VarChar) { Primary=true, Unique=true }                  
                     )
            };

            for (int i = 0; i < tables.Length; i++)
                SQLcreator.EnsureTableStructure(tables[i]);

            Config = Config.Read();
            LoadChatChannels();
        }

        static bool ParseColor(string colorstring, out Color color)
        {
            color = new Color(135, 214, 9);
            byte r, g, b;
            string[] array = colorstring.Split(',');
            if (array.Length != 3)
                return false;
            if (!byte.TryParse(array[0], out r) || !byte.TryParse(array[1], out g) || !byte.TryParse(array[2], out b))
                return false;
            color = new Color(r, g, b);
            return true;
        }

        public static void ReloadAll()
        {
            Channels.Clear();
            ChatMember.Clear();
            LoadChatChannels();
            for (int i = 0; i < TShock.Players.Length; i++)
            {
                if (TShock.Players[i] == null)
                    continue;

                LoadMember(TShock.Players[i]);
            }
        }

        public static void ReloadConfig(TSPlayer ts)
        {
            Config = Config.Read(ts);
        }

        static void LoadChatChannels()
        {
            try
            {
                using (var reader = Database.QueryReader("SELECT * FROM Channels"))
                {
                    while (reader.Read())
                    {
                        string name = reader.Get<string>("Name");
                        string owner = reader.Get<string>("Owner");
                        int inviteMode = reader.Get<int>("InviteMode");
                        int tileX = reader.Get<int>("TileX");
                        int tileY = reader.Get<int>("TileY");
                        string bans = reader.Get<string>("Bans");
                        Color color;
                        ParseColor(reader.Get<string>("ChatColor"), out color);

                        Channel channel = new Channel()
                        {
                            Name = name,
                            Owner = owner,
                            InviteMode = (InviteMode)inviteMode,
                            TileX = tileX,
                            TileY = tileY,
                            Color = color,
                            Bans = JsonConvert.DeserializeObject<List<string>>(bans)
                        };
                        Channels.Add(name, channel);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static bool SetColor(Channel channels, string color)
        {
            Color temp;
            if (!ParseColor(color, out temp))
                return false;
            channels.Color = temp;
            Database.Query("UPDATE Channels SET ChatColor = @0 WHERE Name = @1", color, channels.Name);
            return true;
        }

        public static void Rename(Channel chan, TSPlayer ts, string newname)
        {
            Database.Query("UPDATE Channels SET Name = @0 WHERE Name = @1", newname ,chan.Name);
            Database.Query("UPDATE ChatMember SET ChannelName = @0 WHERE ChannelName = @1",newname, chan.Name);
            UnLoadChan(chan);
            chan.Name = newname;
            LoadChan(chan);
        }

        public static void UnLoadChan(Channel chan)
        {
            Channels.Remove(chan.Name);
            int[] ids = ChatMember.Where(c => c.Value == chan.Name).Select(c => c.Key).ToArray();
            for (int i = 0; i < ids.Length; i++)
                UnLoadMember(TShock.Players[ids[i]]);
        }

        public static void LoadChan(Channel channel)
        {
            Channels.Add(channel.Name, channel);
            foreach (TSPlayer ts in TShock.Players)
            {
                if (ts != null)
                {
                    if (!ChatMember.ContainsKey(ts.Index))
                        LoadMember(ts);
                }
            }
        }

        public static void SetSpawn(Channel channel, TSPlayer ts)
        {           
            channel.TileX = ts.TileX;
            channel.TileY = ts.TileY;
            Database.Query("UPDATE Channels SET TileX = @0, TileY = @1 WHERE Name = @2", channel.TileX, channel.TileY, channel.Name);
        }

        public static void SetInviteMode(Channel channel, InviteMode mode)
        {
            if (channel.InviteMode == mode)
                return;
            channel.InviteMode = mode;
            Database.Query("UPDATE Channels SET InviteMode = @0 WHERE Name = @1", (int)mode, channel.Name);
        }

        static void UpdateBans(Channel channel)
        {
            Database.Query("UPDATE Channels SET Bans = @0 WHERE Name = @1", JsonConvert.SerializeObject(channel.Bans), channel.Name);
        }

        public static bool InsertBan(Channel channel, TSPlayer ts)
        {
            if (channel.Bans.Contains(ts.User.Name))
                return false;
            channel.Bans.Add(ts.User.Name);
            UpdateBans(channel);
            return true;
        }

        public static bool RemoveBan(Channel channel, string name)
        {
            if (channel.Bans.Contains(name))
                return false;
            channel.Bans.Remove(name);
            UpdateBans(channel);
            return true;
        }

        public static bool CreateChannel(TSPlayer ts, Channel channel)
        {
            try
            {
                Database.Query("INSERT INTO Channels (Name, Owner, InviteMode, ChatColor, Bans) VALUES (@0, @1, @2, @3, @4)", channel.Name, channel.Owner, (int)InviteMode.False, Config.DefaultChatColor, "[]");
                Channels.Add(channel.Name, channel);
                JoinChan(ts, channel);
                ChannelHooks.OnChannelCreated(ts, channel.Name);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public static bool JoinChan(TSPlayer ts, Channel channel)
        {
            try
            {
                ChatMember[ts.Index] = channel.Name;
                Database.Query("INSERT INTO ChatMember (Username, ChannelName) VALUES (@0, @1)", ts.User.Name, channel.Name);
                channel.OnlineChannelMembers.Add(ts.Index, new ChannelMember() { Index = ts.Index, ChannelName = ChatMember[ts.Index] });
                if (ts.User.Name != channel.Owner)
                    ChannelHooks.OnChannelJoin(ts, channel);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public static void LeaveChannel(TSPlayer ts, Channel channel)
        {
            try
            {
                ChannelHooks.OnChannelLeave(ts, channel);
                if (ts.User.Name == channel.Owner)
                {
                    ChannelHooks.OnChannelRemoved(channel);
                    RemoveChannel(channel);
                }
                else
                {
                    channel.OnlineChannelMembers.Remove(ts.Index);
                    Database.Query("DELETE FROM ChatMember WHERE Username = @0 AND ChannelName = @1", ts.User.Name, channel.Name);
                }
                ChatMember[ts.Index] = string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void RemoveChannel(Channel channel)
        {
            Database.Query("DELETE FROM ChatMember WHERE ChannelName = @0", channel.Name);
            Database.Query("DELETE FROM Channels WHERE Name = @0", channel.Name);
            Channels.Remove(channel.Name);
        }

        public static void Kick(Channel channel, string name)
        {
            Database.Query("REMOVE FROM ChatMember WHERE ChannelName = @0 AND Username = @1", channel.Name, name);
        }

        public static void LoadMember(TSPlayer ts)
        {
            try
            {
                using (var reader = Database.QueryReader("SELECT * FROM ChatMember WHERE Username = @0", ts.User.Name))
                {
                    if (reader.Read())
                    {
                        string ChannelName = reader.Get<string>("ChannelName");
                        ChatMember.Add(ts.Index, ChannelName);
                        Channel c = FindChannelByPlayer(ts);
                        if (c != null)
                        {
                            c.OnlineChannelMembers.Add(ts.Index, new ChannelMember() { Index = ts.Index, ChannelName = ChannelName });
                            ChannelHooks.OnChannelLogin(ts, c);
                        }
                    }
                    else
                        ChatMember.Add(ts.Index, string.Empty);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void UnLoadMember(TSPlayer ts)
        {
            Channel c = FindChannelByPlayer(ts);
            if (c != null)
                c.OnlineChannelMembers.Remove(ts.Index);
            ChatMember.Remove(ts.Index);
        }

        public static Channel FindChannelByPlayer(TSPlayer ts)
        {
            if (ts == null)
                return null;

            if (!ChatMember.ContainsKey(ts.Index))
                return null;

            return FindChannelByName(ChatMember[ts.Index]);
        }

        public static Channel FindChannelByName(string name)
        {
            if (Channels.ContainsKey(name))
                return Channels[name];
            return null;
        }
    }

    public class Channel
    {
        public string Name { get; set; }
        public string Owner { get; set; }
        public InviteMode InviteMode { get; set; }
        public int TileX { get; set; }
        public int TileY { get; set; }
        public Color Color { get; set; }
        public Dictionary<int, ChannelMember> OnlineChannelMembers { get; set; }
        public List<string> Bans { get; set; }

        public Channel()
        {
            OnlineChannelMembers = new Dictionary<int, ChannelMember>();
            Bans = new List<string>();
            Color = ChatChannelsManager.Config.ParseColor();
        }

        public bool IsInChannel(int PlayerIndex)
        {
            return OnlineChannelMembers.ContainsKey(PlayerIndex);
        }

        public bool IsBanned(string name)
        {
            return Bans.Contains(name);
        }

        public void Broadcast(string msg, int ExcludePlayer = -1)
        {
            foreach (KeyValuePair<int, ChannelMember> kvp in OnlineChannelMembers)
            {
                if (ExcludePlayer > -1 && kvp.Key == ExcludePlayer)
                    continue;

                kvp.Value.TSPlayer.SendMessage(msg, Color);
            }
        }
    }

    public class ChannelMember
    {
        public int Index { get; set; }
        public string ChannelName { get; set; }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public bool DefaultToChannelChat { get; set; }
    }

    public enum InviteMode
    {
        False = 0,
        True = 1
    }
}
