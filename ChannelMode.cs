using System.Collections.Generic;

namespace ChatChannels
{
	public class ChannelMode
	{
		public ChannelMode(char c)
		{

		}


		public static List<ChannelMode> ModesFromString(string modes)
		{
			List<ChannelMode> ret = new List<ChannelMode>();
			//mode format: +abcdefetc
			modes = modes.Remove(0, 1);
			for (int i = 0; i < modes.Length; i++)
			{
				ret.Add(new ChannelMode(modes[i]));
			}
			return ret;
		}
	}
}
