using SocketEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchTV
{
    public class ChatClient
    {
        public TcpClient IRCConnection = null;
        public IRCConfig config;
        public Stream ns = null;
        public StreamReader sr = null;
        public StreamWriter sw = null;

        public ChatClient(IRCConfig config)
        {
            this.config = config;
            try
            {
                IRCConnection = new TcpClient(config.server, config.port);
            }
            catch
            {
                Console.WriteLine("Connection Error");
            }

            try
            {
                ns = IRCConnection.GetStream();
                sr = new StreamReader(ns);
                sw = new StreamWriter(ns);

                sendData("PASS", config.pass);
                sendData("NICK", config.nick);
            }
            catch
            {
                Console.WriteLine("Communication error");
            }
        }

        public string sendData(string cmd, string param)
        {
            if (param == null)
            {
                sw.WriteLine(cmd);
                sw.Flush();
                return cmd;
            }
            else if (cmd == "PRIVMSG")
            {
                sw.WriteLine(cmd + " #" + config.channel + " :" + param);
                sw.Flush();
                return config.nick + ": " + param;
            }
            else
            {
                sw.WriteLine(cmd + " " + param);
                sw.Flush();
                return cmd + " " + param;
            }
        }

    }

    public struct IRCConfig
    {
        public string server;
        public int port;
        public string nick;
        public string pass;
        public string channel;
    }
}
