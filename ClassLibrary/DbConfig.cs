using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public class DbConfig
    {
        /// <summary>
        /// Строка подключения к филиалу 1
        /// </summary>
        public const string MainConn = "Host=localhost;Port=5432;Database=maindb;Username=postgres";
        /// <summary>
        /// Строка подключения к филиалу 2
        /// </summary>
        public const string ClientConn = "Host=localhost;Port=5432;Database=clientdb;Username=postgres";
    }
}
