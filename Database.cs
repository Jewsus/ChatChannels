using System.Collections.Generic;
using System.Data;
using TShockAPI.DB;

namespace ChatChannels
{
	public class Database
	{
		#region Query strings

		const string ChannelUser_Creation_Query = "CREATE TABLE IF NOT EXISTS ChannelUser(UserName VARCHAR(32),Modes VARCHAR(100) NOT NULL,PRIMARY KEY(UserName),FOREIGN KEY(UserName) REFERENCES users(Username)ON DELETE CASCADE ON UPDATE CASCADE);";
		const string Channel_Creation_Query = "CREATE TABLE IF NOT EXISTS Channel(Name VARCHAR(10),ShortName VARCHAR(3),Colour VARCHAR(11) NOT NULL,Modes VARCHAR(100) NOT NULL,PRIMARY KEY(Name));";
		const string Channel_has_users_Creation_Query = "CREATE TABLE IF NOT EXISTS Channel_has_ChannelUser(ChannelName VARCHAR(10),UserName VARCHAR(32),Modes VARCHAR(100) NOT NULL,PRIMARY KEY(ChannelName, UserName),FOREIGN KEY(ChannelName)REFERENCES Channel(Name)ON DELETE CASCADE ON UPDATE CASCADE,FOREIGN KEY(UserName)REFERENCES ChannelUser(UserName) ON DELETE CASCADE ON UPDATE CASCADE);";
		const string ChannelBan_Creation_Query = "CREATE TABLE IF NOT EXISTS ChannelBan(ChannelName VARCHAR(10),UserName VARCHAR(32),PRIMARY KEY(ChannelName, UserName),FOREIGN KEY(ChannelName)REFERENCES Channel(Name)ON DELETE CASCADE ON UPDATE CASCADE,FOREIGN KEY(UserName)REFERENCES ChannelUser(UserName)ON DELETE CASCADE ON UPDATE CASCADE);";

		const string Channel_Insert_Query = "INSERT INTO Channel (Name, ShortName, Colour, Modes) VALUES (@0, @1, @2, @3);";
		const string Channel_Insert_Query_No_Short = "INSERT INTO Channel (Name, Colour, Modes) VALUES (@0, @1, @2);";
		const string Channel_Exists_Query = "SELECT Name FROM Channel WHERE Name = @0";


		const string ChannelUser_Insert_Query = "INSERT INTO ChannelUser(UserName) VALUES (@0);";
		const string ChannelUser_Existance_Query = "SELECT UserName FROM ChannelUser WHERE UserName = @0;";

		const string Channel_has_users_Insert_Query = "INSERT INTO Channel_has_ChannelUser(ChannelName, UserName) VALUES (@0, @1);";

		const string Channel_has_users_Delete_Query = "DELETE FROM Channel_has_ChannelUser WHERE ChannelName = @0 AND UserName = @1;";

		const string Channel_Already_Has_User_Check_Query = "SELECT UserName FROM Channel_has_ChannelUser WHERE UserName = @0 AND ChannelName = @1";
		
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

		/// <summary>
		/// Creates a database representation of a channel
		/// </summary>
		/// <param name="name"></param>
		/// <param name="shortName"></param>
		/// <param name="colour"></param>
		/// <param name="modes"></param>
		/// <returns></returns>
		public bool CreateChannel(string name, string shortName, string colour, string modes)
		{
			//If a channel with the given name exists, return early
			if (CheckChannelExistance(name))
			{
				return false;
			}

			//Don't insert a null or empty value for shortName
			if (string.IsNullOrEmpty(shortName))
			{
				return _db.Query(Channel_Insert_Query_No_Short, name, colour, modes) > 0;
			}

			return _db.Query(Channel_Insert_Query, name, shortName, colour, modes) > 0;
		}

		/// <summary>
		/// Creates a database representation of a ChannelUser
		/// </summary>
		/// <param name="username"></param>
		/// <returns></returns>
		public bool CreateChannelUser(string username)
		{
			//If a user with the given username exists, return early
			if (CheckUserExistance(username))
			{
				return false;
			}

			return _db.Query(ChannelUser_Insert_Query, username) > 0;
		}

		/// <summary>
		/// Joins a user to a channel
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		public bool JoinUserToChannel(string channel, string user)
		{
			//If the user is already part of the channel, return early
			if (CheckUserChannelRegistration(user, channel))
			{
				return false;
			}

			return _db.Query(Channel_has_users_Insert_Query, channel, user) > 0;
		}

		/// <summary>
		/// Removes a user from a channel
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		public bool RemoveUserFromChannel(string channel, string user)
		{
			//If the user is not part of the channel, return early
			if (!CheckUserChannelRegistration(user, channel))
			{
				return false;
			}

			return _db.Query(Channel_has_users_Delete_Query, channel, user) > 0;
		}

		/// <summary>
		/// Determines the existance of a channel
		/// </summary>
		/// <param name="channel"></param>
		/// <returns></returns>
		public bool CheckChannelExistance(string channel)
		{
			using (QueryResult res = _db.QueryReader(Channel_Exists_Query, channel))
			{
				return res.Read();
			}
		}

		/// <summary>
		/// Determines if a ChannelUser exists for the given username
		/// </summary>
		/// <param name="username"></param>
		/// <returns></returns>
		public bool CheckUserExistance(string username)
		{
			using (QueryResult res = _db.QueryReader(ChannelUser_Existance_Query, username))
			{
				return res.Read();
			}
		}
		
		/// <summary>
		/// Determines if a ChannelUser has been registered (joined) to a Channel
		/// </summary>
		/// <param name="username"></param>
		/// <param name="channel"></param>
		/// <returns></returns>
		public bool CheckUserChannelRegistration(string username, string channel)
		{
			using (QueryResult res = _db.QueryReader(Channel_Already_Has_User_Check_Query, username, channel))
			{
				return res.Read();
			}
		}

		/// <summary>
		/// Loads the channel with the given name
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
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

		/// <summary>
		/// Loads all channels
		/// </summary>
		/// <returns></returns>
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
