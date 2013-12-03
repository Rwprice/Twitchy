using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchAPIHandler.Objects
{
    public class OauthToken
    {
        public string Token { get; set; }
        public string UserName { get; set; }

        public void SaveToken(OauthToken token)
        {

        }
    }
}
