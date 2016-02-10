using System.Collections.Generic;

namespace ChatChannels
{
	public class UserMode
	{
		private char _mode;

		public UserMode(char c)
		{
			_mode = c;
		}


		public static List<UserMode> ModesFromString(string modes)
		{
			List<UserMode> ret = new List<UserMode>();
			//mode format: +abcdefetc
			modes = modes.Remove(0, 1);
			for (int i = 0; i < modes.Length; i++)
			{
				ret.Add(new UserMode(modes[i]));
			}
			return ret;
		}

		public bool Equals(char other)
		{
			return _mode.Equals(other);
		}
	}
}
