using System;
using System.Collections.Generic;

namespace ChatChannels
{
	public class ChannelMode : IEquatable<char>
	{
		private char _mode;

		public ChannelMode(char c)
		{
			_mode = c;
		}
		
		public static List<ChannelMode> ModesFromString(string modes)
		{
			List<ChannelMode> ret = new List<ChannelMode>();
			//mode format: +abcCDefetc
			modes = modes.Remove(0, 1);
			for (int i = 0; i < modes.Length; i++)
			{
				ret.Add(new ChannelMode(modes[i]));
			}
			return ret;
		}

		public bool Equals(char other)
		{
			return _mode == other;
		}
	}
}
