using System.Collections.Generic;
using TShockAPI;
using System.IO;


namespace ChatChannels
{
    public class ChatChannelsManager
    {
		Database Database;
        public string SavePath = Path.Combine(TShock.SavePath, "Channels");
        public List<Channel> Channels = new List<Channel>();

		public void Initialize()
        {
            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);

			Database = new Database(TShock.DB);

			Channels = Database.LoadChannels();
        }

		public bool CreateChannel(string name, string colour, string modes)
		{
			return CreateChannel(name, null, colour, modes);
		}

		public bool CreateChannel(string name, string shortName, string colour, string modes)
		{
			return Database.CreateChannel(name, shortName, colour, modes);
		}

		public bool JoinUserToChannel(string channel, string user)
		{
			return Database.JoinUserToChannel(channel, user);
		}

		public bool JoinUserToChannel(Channel channel, ChannelUser user)
		{
			return JoinUserToChannel(channel.Name, user.Name);
		}

		public bool JoinUserToChannel(Channel channel, TSPlayer player)
		{
			if (!player.IsLoggedIn)
			{
				return false;
			}

			if (Database.CheckUserExistance(player.User.Name))
			{
				return JoinUserToChannel(channel.Name, player.User.Name);
			}

			if (!Database.CreateChannelUser(player.User.Name))
			{
				return false;
			}
			
			return JoinUserToChannel(channel.Name, player.User.Name);
		}
    }
}
