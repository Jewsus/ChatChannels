using System.Collections.Generic;
using System.Linq;
using TShockAPI;

namespace ChatChannels
{
	public class ChannelUser
	{
		public string Name { get; private set; }
		public List<UserMode> Modes { get; private set; }
		public List<Channel> Channels { get; private set; }
		public Channel ActiveChannel { get; private set; }

		public ChannelUser(string name)
		{
			Name = name;
			Modes = new List<UserMode>();
			Channels = new List<Channel>();
		}

		public bool HasMode(char c)
		{
			return Modes.Any(m => m.Equals(c));
		}

		public void SetActiveChannel(string shortName)
		{
			if (shortName == null || shortName == "~")
			{
				ActiveChannel = null;
				return;
			}

			Channel channel = Channels.FirstOrDefault(c => c.ShortName != null && c.ShortName == shortName);
			if (channel == null)
			{
				channel = Channels.FirstOrDefault(c => c.Name == shortName);
			}
			if (channel == null)
			{
				return;
			}

			ActiveChannel = channel;
		}

		public bool SendMessageToActiveChannel(TSPlayer self, string msg, params object[] args)
		{
			if (ActiveChannel == null)
			{
				return false;
			}

			ActiveChannel.BroadcastFromPlayer(self, msg, args);
			return true;
		}
	}
}
