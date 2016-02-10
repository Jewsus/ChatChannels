using System.Collections.Generic;
using System.Linq;

namespace ChatChannels
{
	public class ChannelUser
	{
		public string Name { get; private set; }
		public List<UserMode> Modes { get; private set; }

		public ChannelUser(string name)
		{
			Name = name;
			Modes = new List<UserMode>();
		}

		public bool HasMode(char c)
		{
			return Modes.Any(m => m.Equals(c));
		}
	}
}
