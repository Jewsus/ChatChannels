using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChatChannels
{
	public abstract class ModeBase
	{
		protected List<char> modes = new List<char>();
		protected Regex modRegex = new Regex("(?<sign>[+-])(?<modes>\\w+)");

		public virtual bool HasMode(char mode)
		{
			return modes.Contains(mode);
		}

		public void ModifyModes(string modes)
		{
			if (!modRegex.IsMatch(modes))
			{
				return;
			}

			MatchCollection matches = modRegex.Matches(modes);
			foreach (Match match in matches)
			{
				if (match.Groups["sign"].Value == "+" || string.IsNullOrWhiteSpace(match.Groups["sign"].Value))
				{
					AddModes(match.Groups["modes"].Value);
				}
				else if (match.Groups["sign"].Value == "-")
				{
					RemoveModes(match.Groups["modes"].Value);
				}
				continue;
			}
		}

		public void AddMode(char mode)
		{
			modes.Add(mode);
		}

		public void AddModes(string modes)
		{
			for (int i = 0; i < modes.Length; i++)
			{
				this.modes.Add(modes[i]);
			}
		}

		public void RemoveMode(char mode)
		{
			modes.Remove(mode);
		}

		public void RemoveModes(string modes)
		{
			for (int i = 0; i < modes.Length; i++)
			{
				this.modes.Remove(modes[i]);
			}
		}

		public override string ToString()
		{
			return string.Join("", modes);
		}
	}
}
