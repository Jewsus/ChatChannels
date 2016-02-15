using System;
using System.Collections.Generic;
using System.Linq;
using TShockAPI;

namespace ChatChannels
{
	public class Channel
	{
		public string Name { get; private set; }
		public string ShortName { get; private set; }
		public Color Colour { get; private set; }
		public ChannelModes Modes { get; private set; }
		public List<ChannelUser> Users { get; private set; }

		public Channel(string name, string shortName, Color colour, ChannelModes modes)
		{
			Name = name;
			ShortName = shortName;
			Colour = colour;
			Modes = modes;
			Users = new List<ChannelUser>();
		}

		public Channel(string name, string shortName, string colour, string modes)
		{
			Name = name;
			ShortName = shortName;
			Colour = ParseColour(colour);
			Modes = ChannelModes.ModesFromString(modes);
			Users = new List<ChannelUser>();
		}

		public bool HasMode(char c)
		{
			return Modes.HasMode(c);
		}

		public ChannelUser GetUser(TSPlayer player)
		{
			if (!player.IsLoggedIn)
			{
				return null;
			}

			return Users.FirstOrDefault(u => u.Name == player.User.Name);
		}

		public void Broadcast(string msg, params object[] args)
		{
			string text = string.Format($"[{Name}] {msg}", args);
			List<string> userNames = Users.Select(u => u.Name).ToList();
			foreach (TSPlayer player in TShock.Players.Where(p => p != null && p.IsLoggedIn && userNames.Contains(p.User.Name)))
			{
				player.SendMessage(string.Format(text, args), Colour);
			}
		}

		public void BroadcastFromPlayer(TSPlayer player, string msg, params object[] args)
		{
			if (HasMode(ChannelModes.MuteMode))
			{
				ChannelUser user = GetUser(player);
				if (user == null || !user.HasMode(UserModes.BypassChannelMute))
				{
					return;
				}
			}
			string text = $"{player.Group.Prefix}{player.Name}{player.Group.Suffix}: {msg}";
			Broadcast(text, args);
		}

		public static Color ParseColour(string colour)
		{
			string[] split = colour.Split(',');
			if (split.Length < 3)
			{
				throw new FormatException($"string '{colour}' was not in the correct format. Expected format 'rrr,ggg,bbb'.");
			}

			byte r, g, b;

			if (!byte.TryParse(split[0], out r) || !byte.TryParse(split[1], out g) || !byte.TryParse(split[2], out b))
			{
				throw new InvalidCastException($"Failed to cast string to colour. Expected a string in the format 'rrr,ggg,bbb'.");
			}

			return new Color(r, g, b);
		}

		public static string ColourToString(Color colour)
		{
			return $"{colour.R},{colour.G},{colour.B}";
		}
	}
}
