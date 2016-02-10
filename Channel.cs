using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ChatChannels
{
	public class Channel
	{
		public string Name { get; private set; }
		public string ShortName { get; private set; }
		public Color Colour { get; private set; }
		public List<ChannelMode> Modes { get; private set; }
		public List<ChannelUser> Users = new List<ChannelUser>();

		public Channel(string name, string shortName, Color colour, List<ChannelMode> modes)
		{
			Name = name;
			ShortName = shortName;
			Colour = colour;
			Modes = modes;
		}

		public Channel(string name, string shortName, string colour, string modes)
		{
			Name = name;
			ShortName = shortName;
			Colour = ParseColour(colour);
			Modes = ChannelMode.ModesFromString(modes);
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
