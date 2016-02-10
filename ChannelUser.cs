namespace ChatChannels
{
	public class ChannelUser
	{
		public string Name { get; private set; }

		public ChannelUser(string name)
		{
			Name = name;
		}
	}
}
