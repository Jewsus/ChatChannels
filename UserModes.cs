using System.Collections.Generic;

namespace ChatChannels
{
	public class UserModes
	{
		public const char ChannelOwner = 'C';
		public const char BypassChannelMute = 'M';
		public const char Muted = 'm';
		public const char Invited = 'I';

		private List<char> _modes = new List<char>();

		public bool HasMode(char mode)
		{
			if (_modes.Contains(ChannelOwner))
			{
				return true;
			}

			return _modes.Contains(mode);
		}

		public void AddMode(char mode)
		{
			_modes.Add(mode);
		}

		public static UserModes ModesFromString(string modes)
		{
			UserModes ret = new UserModes();
			//mode format: +abcdefetc
			modes = modes.Remove(0, 1);
			for (int i = 0; i < modes.Length; i++)
			{
				ret.AddMode(modes[i]);
			}
			return ret;
		}
	}
}
