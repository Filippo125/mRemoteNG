using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mRemoteNG.Credential.KeePass
{
    public class KeePassEntry
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string Uuid { get; set; }
        public string Name { get; set; }

        public string ParentGroup { get; set; }
        public Dictionary<String, String> StringFields = new Dictionary<string, string>();

        public KeePassEntry(string name, string uuid, string login, string password)
        {
            Login = login;
            Name = name;
            Uuid = uuid;
            Password = password;
        }

        public KeePassEntry(string name, string uuid, string login, string password, string parentGroup)
        {
            Login = login;
            Name = name;
            Uuid = uuid;
            Password = password;
            ParentGroup = parentGroup;
        }
        public KeePassEntry()
        {
        }

        public void SetExtraFields(List<object> ts)
        {
            foreach(Dictionary<string,object> extra in ts)
            {
                this.StringFields.Add((string)extra["Key"], (string) extra["Value"]);
            }
        }
    }
}
