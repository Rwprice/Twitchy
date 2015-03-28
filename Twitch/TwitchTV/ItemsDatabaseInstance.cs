using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Wintellect.Sterling.Database;

namespace TwitchTV
{
    public class PlaylistDatabaseInstance : BaseDatabaseInstance
    {
        public override string Name
        {
            get
            {
                return "PlaylistDatabase";
            }
        }

        protected override List<ITableDefinition> RegisterTables()
        {
            return new List<ITableDefinition>
    
                        {
                            // we are only registering the To Do list item with the Sterling engine in this example
                            CreateTableDefinition<Playlist, int>(testModel => testModel.Key)
                        };
        }

        protected string DATAINDEX
        {
            get { return "dataIndex"; }
        }
    }

    public class ModelBase
    {
        private int _key = -1;
        public int Key
        {
            get { return _key; }
            set { _key = value; }
        }
    }

    public class Playlist : ModelBase
    {
        public string Name { get; set; }

        public string Address { get; set; }
    }

    public static class Extensions
    {
        public static void Save(this Playlist playlist)
        {
            int currentIndex = (Application.Current as App).Database.Query<Playlist, int>().Count();
            if (playlist.Key == -1)
            {
                playlist.Key = currentIndex;
            }

            (Application.Current as App).Database.Save(playlist);
            (App.Current as App).Database.Flush();

        }
    }
}
