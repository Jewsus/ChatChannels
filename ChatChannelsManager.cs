using System.Collections.Generic;
using TShockAPI;
using System.IO;
using System.Linq;

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

		public Channel GetChannel(string name)
		{
			return Channels.FirstOrDefault(c => c.Name.ToLowerInvariant() == name.ToLowerInvariant());
		}

		public ErrorCode CreateChannel(string name, string colour, string modes)
		{
			return CreateChannel(name, null, colour, modes);
		}

		public ErrorCode CreateChannel(string name, string shortName, string colour, string modes)
		{
			bool ret = Database.CreateChannel(name, shortName, colour, modes);

			if (ret)
			{
				Channels.Add(new Channel(name, shortName, colour, modes));
			}
			return ret ? ErrorCode.Success : ErrorCode.ChannelCreateFailed;
		}

		public ErrorCode JoinUserToChannel(string channel, string user)
		{
			return Database.JoinUserToChannel(channel, user) ? ErrorCode.Success : ErrorCode.JoinFailed;
		}

		public ErrorCode JoinUserToChannel(Channel channel, ChannelUser user)
		{
			if (channel.HasMode('p') && !user.HasMode('s'))
			{
				return ErrorCode.InsufficientAccess;
			}

			ErrorCode ret = JoinUserToChannel(channel.Name, user.Name);
			if (ret == ErrorCode.Success)
			{
				channel.Users.Add(user);
			}
			return ret;
		}

		public ErrorCode JoinUserToChannel(Channel channel, TSPlayer player)
		{
			if (!player.IsLoggedIn)
			{
				return ErrorCode.InsufficientAccess;
			}

			if (Database.CheckUserExistance(player.User.Name))
			{
				return JoinUserToChannel(channel.Name, player.User.Name);
			}

			if (!Database.CreateChannelUser(player.User.Name))
			{
				return ErrorCode.UserCreateFailed;
			}
			
			ErrorCode ret = JoinUserToChannel(channel.Name, player.User.Name);
			if (ret == ErrorCode.Success)
			{
				channel.Users.Add(new ChannelUser(player.User.Name));
			}
			return ret;
		}

		public ErrorCode RemoveUserFromChannel(string channel, string user)
		{
			return Database.RemoveUserFromChannel(channel, user) ? ErrorCode.Success : ErrorCode.ChannelRegistrationNotFound;
		}

		public ErrorCode RemoveUserFromChannel(Channel channel, ChannelUser user)
		{
			return RemoveUserFromChannel(channel.Name, user.Name);
		}

		public ErrorCode RemoveUserFromChannel(Channel channel, TSPlayer player)
		{
			if (!player.IsLoggedIn)
			{
				return ErrorCode.InsufficientAccess;
			}

			if (!Database.CheckUserExistance(player.User.Name))
			{
				return ErrorCode.UserNotFound;
			}

			return RemoveUserFromChannel(channel.Name, player.User.Name);
		}

		public ErrorCode RemoveChannel(string channel)
		{
			return Database.RemoveChannel(channel) ? ErrorCode.Success : ErrorCode.ChannelNotFound;
		}

		public ErrorCode RemoveChannel(Channel channel)
		{
			return RemoveChannel(channel.Name);
		}

		public ErrorCode RemoveChannelUser(string user)
		{
			return Database.RemoveUser(user) ? ErrorCode.Success : ErrorCode.UserNotFound;
		}

		public ErrorCode RemoveChannelUser(ChannelUser user)
		{
			return RemoveChannelUser(user.Name);
		}

		public ErrorCode RemoveChannelUser(TSPlayer player)
		{
			if (!player.IsLoggedIn)
			{
				return ErrorCode.InsufficientAccess;
			}

			//database checks user existance for us
			return RemoveChannelUser(player.User.Name);
		}
    }
}
