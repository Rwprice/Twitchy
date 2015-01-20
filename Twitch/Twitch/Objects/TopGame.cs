using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.ComponentModel;

namespace TwitchAPIHandler.Objects
{
    public class TopGame
    {
        private int _channels;
        public int channels
        {
            get
            {
                return _channels;
            }
            set
            {
                if (value != _channels)
                {
                    _channels = value;
                }
            }
        }

        private Game _game;
        public Game game
        {
            get
            {
                return _game;
            }
            set
            {
                if (value != _game)
                {
                    _game = value;
                }
            }
        }

        public static string TOP_GAMES_PATH = PathStrings.TOP_GAMES_PATH;
    }

    public class Game
    {
        public string name { get; set; }
        public Box box { get; set; }
    }

    public class Box
    {
        public BitmapImage medium { get; set; }
    }
}
