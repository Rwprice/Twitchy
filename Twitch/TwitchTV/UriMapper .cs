using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace TwitchTV
{
    public class UriMapper : UriMapperBase
    {
        public override Uri MapUri(Uri uri)
        {
            string uriToLaunch = System.Net.HttpUtility.UrlDecode(uri.ToString());
 
            // File association launch
            if (uriToLaunch.Contains(@"twitch://"))
            {
                String streamName = null;
                if (uriToLaunch.Contains("/stream/"))
                {
                    int index = uriToLaunch.IndexOf("/stream/") + 8;
                    streamName = uriToLaunch.Substring(index);
                }

                if (streamName != null)
                    return new Uri("/Screens/PlayerPage.xaml?" + streamName, UriKind.Relative);
                else
                    return new Uri("/Screens/MainPage.xaml", UriKind.Relative);
            }

            else
            {
                return uri;
            }
        }
    }
}
