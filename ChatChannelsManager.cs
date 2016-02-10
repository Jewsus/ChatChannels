using System.Collections.Generic;
using TShockAPI;
using System.Linq;

namespace ChatChannels
{
    public class ChatChannelsManager
    {
		Database Database;
        public List<Channel> Channels = new List<Channel>();
		public List<ChannelUser> Users = new List<ChannelUser>();

		public void Initialize()
        {
			Database = new Database(TShock.DB);
			Channels = Database.LoadChannels();
        }

		public Channel GetChannel(string name)
		{
			return Channels.FirstOrDefault(c => c.Name.ToLowerInvariant() == name.ToLowerInvariant());
		}

		public ChannelUser GetUser(string name)
		{
			return Users.FirstOrDefault(u => u.Name.ToLowerInvariant() == name.ToLowerInvariant());
		}

		public void BuildUserRelations(ChannelUser user)
		{
			Users.Add(user);
			if (!Database.CheckUserExistance(user.Name))
			{
				Database.CreateChannelUser(user.Name);
				return;
			}

			List<string> channels = Database.GetChannelNamesWithUser(user.Name);
			foreach (Channel c in Channels.Where(c => channels.Contains(c.Name)))
			{
				System.Console.WriteLine($"Added {user.Name} to {c.Name}");
				c.Users.Add(user);
				user.Channels.Add(c);
			}
		}

		public void DestructUser(TSPlayer player)
		{
			ChannelUser user = GetUser(player.User.Name);
			if (user == null)
			{
				return;
			}

			DestructUser(user);
		}

		public void DestructUser(ChannelUser user)
		{
			for (int i = user.Channels.Count - 1; i >= 0; i--)
			{
				user.Channels[i].Users.Remove(user);
			}
			Users.Remove(user);
		}

		public ErrorCode CreateChannel(string name, string colour, string modes, out Channel c)
		{
			return CreateChannel(name, null, colour, modes, out c);
		}

		public ErrorCode CreateChannel(string name, string shortName, string colour, string modes, out Channel c)
		{
			bool ret = Database.CreateChannel(name, shortName, colour, modes);

			if (ret)
			{
				c = new Channel(name, shortName, colour, modes);
				Channels.Add(c);
			}
			else
			{
				c = null;
			}
			return ret ? ErrorCode.Success : ErrorCode.ChannelCreateFailed;
		}

		public ErrorCode JoinUserToChannel(string channel, string user)
		{
			return Database.JoinUserToChannel(channel, user) ? ErrorCode.Success : ErrorCode.JoinFailed;
		}

		public ErrorCode JoinUserToChannel(Channel channel, ChannelUser user)
		{
			//Bubbles down to JoinUserToChannel(string, string)
			if (channel.HasMode('p') && !user.HasMode('S'))
			{
				return ErrorCode.InsufficientAccess;
			}

			ErrorCode ret = JoinUserToChannel(channel.Name, user.Name);
			if (ret == ErrorCode.Success)
			{
				channel.Users.Add(user);
				user.Channels.Add(channel);
			}
			return ret;
		}

		public ErrorCode JoinUserToChannel(Channel channel, TSPlayer player)
		{
			//Bubbles down to JoinUserToChannel(Channel, ChannelUser)
			if (!player.IsLoggedIn)
			{
				return ErrorCode.InsufficientAccess;
			}

			ChannelUser user;

			if (Database.CheckUserExistance(player.User.Name))
			{
				user = GetUser(player.User.Name);
				return JoinUserToChannel(channel, user);
			}

			if (Database.CreateChannelUser(player.User.Name))
			{
				user = new ChannelUser(player.User.Name);
			}
			else
			{
				return ErrorCode.UserCreateFailed;
			}
			
			return JoinUserToChannel(channel, user);
		}

		public ErrorCode RemoveUserFromChannel(string channel, string user)
		{
			return Database.RemoveUserFromChannel(channel, user) ? ErrorCode.Success : ErrorCode.ChannelRegistrationNotFound;
		}

		public ErrorCode RemoveUserFromChannel(Channel channel, ChannelUser user)
		{
			//Bubbles down to RemoveUserFromChannel(string, string)
			ErrorCode ret = RemoveUserFromChannel(channel.Name, user.Name);
			if (ret == ErrorCode.Success)
			{
				channel.Users.Remove(user);
				user.Channels.Remove(channel);
			}
			return ret;
		}

		public ErrorCode RemoveUserFromChannel(Channel channel, TSPlayer player)
		{
			//Bubbles down to RemoveUserFromChannel(Channel, ChannelUser)
			if (!player.IsLoggedIn)
			{
				return ErrorCode.InsufficientAccess;
			}

			if (!Database.CheckUserExistance(player.User.Name))
			{
				return ErrorCode.UserNotFound;
			}

			ChannelUser user = GetUser(player.User.Name);

			return RemoveUserFromChannel(channel, user);
		}

		public ErrorCode RemoveChannel(string channel)
		{
			return Database.RemoveChannel(channel) ? ErrorCode.Success : ErrorCode.ChannelNotFound;
		}

		public ErrorCode RemoveChannel(Channel channel)
		{
			ErrorCode ret = RemoveChannel(channel.Name);
			if (ret == ErrorCode.Success)
			{
				Channels.Remove(channel);
				foreach (ChannelUser user in channel.Users)
				{
					user.Channels.Remove(channel);
					if (user.ActiveChannel == channel)
					{
						user.SetActiveChannel(null);
					}
				}
			}
			return ret;
		}

		public ErrorCode RemoveChannelUser(string user)
		{
			return Database.RemoveUser(user) ? ErrorCode.Success : ErrorCode.UserNotFound;
		}

		public ErrorCode RemoveChannelUser(ChannelUser user)
		{
			ErrorCode ret = RemoveChannelUser(user.Name);
			if (ret == ErrorCode.Success)
			{
				DestructUser(user);
			}
			return ret;
		}

		public ErrorCode RemoveChannelUser(TSPlayer player)
		{
			if (!player.IsLoggedIn)
			{
				return ErrorCode.InsufficientAccess;
			}

			if (!Database.CheckUserExistance(player.User.Name))
			{
				return ErrorCode.UserNotFound;
			}

			ChannelUser user = GetUser(player.User.Name);
			
			return RemoveChannelUser(user);
		}
    }
}
