using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mRemoteNG.Credential.KeePass
{
    class KeePassEntry
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string Uuid { get; set; }
        public string Name { get; set; }


        public KeePassEntry(string name, string uuid, string login, string password)
        {
            Login = login;
            Name = name;
            Uuid = uuid;
            Password = password;
        }
        public KeePassEntry()
        {

        }
    }
}
