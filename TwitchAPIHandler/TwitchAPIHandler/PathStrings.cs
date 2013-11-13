using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchAPIHandler
{
    public class PathStrings
    {
        public const string TOKEN_PATH = "http://api.twitch.tv/api/channels/{0}/access_token";
        public const string M3U8_PATH = "http://usher.twitch.tv/api/channel/hls/{0}.m3u8?token={1}&sig={2}";
    }
}
