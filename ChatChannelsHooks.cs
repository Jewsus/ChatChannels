using System;
using TShockAPI;

namespace ChatChannels.Hooks
{
    public static class ChannelHooks
    {
        public delegate void ChannelCreated(ChannelEventArgs e);
        public static event ChannelCreated OnChannelCreate;

        public delegate void ChannelRemoved(ChannelEventArgs e);
        public static event ChannelRemoved OnChannelRemove;

        public delegate void ChannelnLogin(ChannelEventArgs e);
        public static event ChannelnLogin OnChannelLogin;

        public delegate void ChannelLogout(ChannelEventArgs e);
        public static event ChannelLogout OnChannelLogout;

        public delegate void ChannelJoin(ChannelEventArgs e);
        public static event ChannelJoin OnChannelJoin;

        public delegate void ChannelLeave(ChannelEventArgs e);
        public static event ChannelLeave OnChanneLeave;

        public static void InvokeChannelCreated(TSPlayer ts, Channel channel)
        {
            if (OnChannelCreate == null)
                return;

            OnChannelCreate(new ChannelEventArgs() { TSPlayer = ts, Channel = channel });
        }

        public static void InvokeChannelRemoved(TSPlayer player, Channel channel)
        {
            if (OnChannelRemove == null)
                return;

            OnChannelRemove(new ChannelEventArgs() { TSPlayer = player, Channel = channel });
        }

        public static void InvokeChannelLogin(TSPlayer ts, Channel channel)
        {
            if (OnChannelLogin == null)
                return;

            OnChannelLogin(new ChannelEventArgs() { TSPlayer = ts, Channel = channel });
        }

        public static void InvokeChannelLogout(TSPlayer ts, Channel channel)
        {
            if (OnChannelLogout == null)
                return;

            OnChannelLogout(new ChannelEventArgs() { TSPlayer = ts, Channel = channel });
        }

        public static void InvokeChannelJoin(TSPlayer ts, Channel chanel)
        {
            if (OnChannelJoin == null)
                return;

            OnChannelJoin(new ChannelEventArgs() { TSPlayer = ts, Channel = chanel });
        }

        public static void InvokeChannelLeave(TSPlayer ts, Channel channel)
        {
            if (OnChanneLeave == null)
                return;

            OnChanneLeave(new ChannelEventArgs() { TSPlayer = ts, Channel = channel });
        }
    }

	public class ChannelEventArgs : EventArgs
	{
		public TSPlayer TSPlayer;
		public Channel Channel;
	}
}
