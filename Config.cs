using System.IO;
using Newtonsoft.Json;
using TShockAPI;

namespace ChatChannels
{
    public class Config
    {
		public char ChatChannelSwitcherCharacter = '>';

		public Config Read(string path, TSPlayer ts = null)
		{
			if (!File.Exists(path))
			{
				Write(path);
			}

			try
			{
				Config res = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
				return res;
			}
			catch (System.Exception e)
			{
				if (ts != null)
				{
					ts.SendErrorMessage("[ChatChannels] Config reading failed. See logs for more details.");
				}
				TShock.Log.ConsoleError($"[ChatChannels] Config reading failed: {e}");
				return null;
			}
		}

        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
