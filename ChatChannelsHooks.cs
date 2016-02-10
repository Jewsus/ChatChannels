using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;

namespace ChatChannels.Hooks
{
    public static class ChannelHooks
    {
        public delegate void ChannelCreatedD(ChannelCreatedEventArgs e);
        public static event ChannelCreatedD ChannelCreated;

        public delegate void ChannelRemovedD(ChannelRemovedEventArgs e);
        public static event ChannelRemovedD ChannelRemove;

        public delegate void ChannelnLoginD(ChannelLoginEventArgs e);
        public static event ChannelnLoginD ChannelLogin;

        public delegate void ChannelLogoutD(ChannelLogoutEventArgs e);
        public static event ChannelLogoutD ChannelLogout;

        public delegate void ChannelJoinD(ChannelJoinEventArgs e);
        public static event ChannelJoinD ChannelJoin;

        public delegate void ChannelLeaveD(ChannelLeaveEventArgs e);
        public static event ChannelLeaveD ChanneLeave;

        public static void OnChannelCreated(TSPlayer ts, string channelname)
        {
            if (ChannelCreated == null)
                return;

            ChannelCreated(new ChannelCreatedEventArgs() { TSplayer = ts, ChannelName = channelname });
        }

        public static void OnChannelRemoved(Channel channel)
        {
            if (ChannelRemove == null)
                return;

            ChannelRemove(new ChannelRemovedEventArgs() { Channel = channel });
        }

        public static void OnChannelLogin(TSPlayer ts, Channel channel)
        {
            if (ChannelLogin == null)
                return;

            ChannelLogin(new ChannelLoginEventArgs() { TSplayer = ts, Channel = channel });
        }

        public static void OnChannelLogout(TSPlayer ts, Channel channel)
        {
            if (ChannelLogout == null)
                return;

            ChannelLogout(new ChannelLogoutEventArgs() { TSplayer = ts, Channel = channel });
        }

        public static void OnChannelJoin(TSPlayer ts, Channel chanel)
        {
            if (ChannelJoin == null)
                return;

            ChannelJoin(new ChannelJoinEventArgs() { TSplayer = ts, Channel = chanel });
        }

        public static void OnChannelLeave(TSPlayer ts, Channel channel)
        {
            if (ChanneLeave == null)
                return;

            ChanneLeave(new ChannelLeaveEventArgs() { TSplayer = ts, Channel = channel });
        }
    }

    public class ChannelCreatedEventArgs : EventArgs
    {
        public TSPlayer TSplayer;
        public string ChannelName;
    }

    public class ChannelRemovedEventArgs : EventArgs
    {
        public TSPlayer TSplayer;
        public Channel Channel;
    }

    public class ChannelLoginEventArgs : EventArgs
    {
        public TSPlayer TSplayer;
        public Channel Channel;
    }

    public class ChannelLogoutEventArgs : EventArgs
    {
        public TSPlayer TSplayer;
        public Channel Channel;
    }

    public class ChannelJoinEventArgs : EventArgs
    {
        public TSPlayer TSplayer;
        public Channel Channel;
    }

    public class ChannelLeaveEventArgs : EventArgs
    {
        public TSPlayer TSplayer;
        public Channel Channel;
    }
}
