using System.ComponentModel;

namespace ChatChannels
{
    public static class Permission
    {
        [Description("For usage of /chan")]
        public static readonly string Use = "chan.use";

        [Description("For usage of /chan checkupdate")]
        public static readonly string Update = "chan.update";

        [Description("For usage of /chan")]
        public static readonly string Chat = "chan.chat";

        [Description("For usage of /chan create")]
        public static readonly string Create = "chan.create";

        [Description("For usage of /chan reloadchannels & /chan reloadconfig")]
        public static readonly string Reload = "chan.reload";
    }
}
