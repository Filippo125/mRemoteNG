using mRemoteNG.Config.DataProviders;
using mRemoteNG.Config.Serializers.Xml;
using mRemoteNG.Tools;
using mRemoteNG.Tree;
using System;
using System.IO;
using System.Security;

namespace mRemoteNG.Config.Connections
{
    public class KeePassHttpConnectionLoader : IConnectionsLoader
    {
        private readonly string _connectionFilePath;

        public KeePassHttpConnectionLoader()
        {
        }

        public ConnectionTreeModel Load()
        {
            var dataProvider = new KeePassDataProvider();
            var data = dataProvider.Load();
            var deserializer = new KeePassConnectionDeserializer();
            return deserializer.Deserialize(data);
        }

        private Optional<SecureString> PromptForPassword()
        {
            var password = MiscTools.PasswordDialog("", false);
            return password;
        }
    }
}