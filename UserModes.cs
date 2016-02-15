namespace ChatChannels
{
	public class UserModes : ModeBase
	{
		public const char ChannelOwner = 'C';
		public const char BypassChannelMute = 'M';
		public const char Muted = 'm';
		public const char Invited = 'I';

		public override bool HasMode(char mode)
		{
			if (modes.Contains(ChannelOwner))
			{
				return true;
			}

			return base.HasMode(mode);
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
