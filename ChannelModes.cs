using System.Collections.Generic;

namespace ChatChannels
{
	public class ChannelModes
	{
		/// <summary>
		/// Channel Mute.No messages can be sent (except by channel users with a user mode that overrides this)
		/// </summary>
		public const char MuteMode = 'M';
		/// <summary>
		/// Private Channel. People can only join with an invite
		/// </summary>
		public const char PrivateMode = 'p';

		private List<char> _modes = new List<char>();

		public bool HasMode(char mode)
		{
			return _modes.Contains(mode);
		}

		public void AddMode(char mode)
		{
			_modes.Add(mode);
		}
		
		public static ChannelModes ModesFromString(string modes)
		{
			ChannelModes ret = new ChannelModes();
			for (int i = 0; i < modes.Length; i++)
			{
				ret.AddMode(modes[i]);
			}
			return ret;
		}
	}
}
