using System.Collections.Generic;
using System.Data;
using TShockAPI.DB;

namespace ChatChannels
{
	public class Database
	{
		#region Query strings

		const string ChannelUser_Creation_Query = "CREATE TABLE IF NOT EXISTS ChannelUser(UserName VARCHAR(32),PRIMARY KEY(UserName));";
		const string Channel_Creation_Query = "CREATE TABLE IF NOT EXISTS Channel(Name VARCHAR(10),ShortName VARCHAR(3),Colour VARCHAR(11) NOT NULL,Modes VARCHAR(100) NOT NULL,PRIMARY KEY(Name));";
		const string Channel_has_users_Creation_Query = "CREATE TABLE IF NOT EXISTS Channel_has_ChannelUser(ChannelName VARCHAR(10),UserName VARCHAR(32),Modes VARCHAR(100) NOT NULL,PRIMARY KEY(ChannelName, UserName),FOREIGN KEY(ChannelName)REFERENCES Channel(Name)ON DELETE CASCADE ON UPDATE CASCADE,FOREIGN KEY(UserName)REFERENCES ChannelUser(UserName) ON DELETE CASCADE ON UPDATE CASCADE);";
		const string ChannelBan_Creation_Query = "CREATE TABLE IF NOT EXISTS ChannelBan(ChannelName VARCHAR(10),UserName VARCHAR(32),PRIMARY KEY(ChannelName, UserName),FOREIGN KEY(ChannelName)REFERENCES Channel(Name)ON DELETE CASCADE ON UPDATE CASCADE,FOREIGN KEY(UserName)REFERENCES ChannelUser(UserName)ON DELETE CASCADE ON UPDATE CASCADE);";

		const string Channel_Insert_Query = "INSERT INTO Channel (Name, ShortName, Colour, Modes) VALUES (@0, @1, @2, @3);";
		const string Channel_Insert_Query_No_Short = "INSERT INTO Channel (Name, Colour, Modes) VALUES (@0, @1, @2);";

		const string ChannelUser_Insert_Query = "INSERT INTO ChannelUser(UserName) VALUES (@0)";
		const string ChannelUser_Existance_Query = "SELECT UserName FROM ChannelUser WHERE UserName = @0";

		const string Channel_has_users_Insert_Query = "INSERT INTO Channel_has_ChannelUser(ChannelName, UserName) VALUES (@0, @1)";

		#endregion

		internal IDbConnection _db;


		public Database(IDbConnection db)
		{
			_db = db;

			_db.Query(ChannelUser_Creation_Query);
			_db.Query(Channel_Creation_Query);
			_db.Query(Channel_has_users_Creation_Query);
			_db.Query(ChannelBan_Creation_Query);
		}

		public bool CreateChannel(string name, string shortName, string colour, string modes)
		{
			if (string.IsNullOrEmpty(shortName))
			{
				return _db.Query(Channel_Insert_Query_No_Short, name, colour, modes) > 0;
			}

			return _db.Query(Channel_Insert_Query, name, shortName, colour, modes) > 0;
		}

		public bool CreateChannelUser(string username)
		{
			return _db.Query(ChannelUser_Insert_Query, username) > 0;
		}

		public bool JoinUserToChannel(string channel, string user)
		{
			return _db.Query(Channel_has_users_Insert_Query, channel, user) > 0;
		}

		public bool CheckUserExistance(string username)
		{
			using (QueryResult res = _db.QueryReader(ChannelUser_Existance_Query, username))
			{
				return res.Read();
			}
		}

		public Channel LoadChannel(string name)
		{
			using (QueryResult res = _db.QueryReader("SELECT ShortName, Colour, Modes FROM Channel WHERE Name = @0", name))
			{
				if (res.Read())
				{
					string shortName = res.Get<string>("ShortName");
					string rgb = res.Get<string>("Colour");
					string modes = res.Get<string>("Modes");

					return new Channel(name, shortName, rgb, modes);
				}
				else
				{
					return null;
				}
			}
		}

		public List<Channel> LoadChannels()
		{
			List<Channel> ret = new List<Channel>();
			using (QueryResult res = _db.QueryReader("SELECT Name, ShortName, Colour, Modes FROM Channel"))
			{
				while (res.Read())
				{
					string name = res.Get<string>("Name");
					string shortName = res.Get<string>("ShortName");
					string rgb = res.Get<string>("Colour");
					string modes = res.Get<string>("Modes");

					ret.Add(new Channel(name, shortName, rgb, modes));
				}
			}

			return ret;
		}
	}
}
