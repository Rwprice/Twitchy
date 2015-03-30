using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Wintellect.Sterling.Core.Database;

namespace TwitchTV
{
    public class PlaylistDatabaseInstance : BaseDatabaseInstance
    {
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

        public string Status { get; set; }
    }
}
