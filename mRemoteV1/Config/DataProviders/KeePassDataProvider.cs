using mRemoteNG.Credential.KeePass;
using System.Collections.Generic;

namespace mRemoteNG.Config.DataProviders
{
    public class KeePassDataProvider : IDataProvider<List<KeePassEntry>>
    {
        private KeePassHttpClient _keepassClient;

        public KeePassDataProvider()
        {
            _keepassClient = new KeePassHttpClient();
        }

        public List<KeePassEntry> Load()
        {
            return _keepassClient.GetAllLogin();
        }

        public void Save(List<KeePassEntry> keePassEntries)
        {
            
        }
    }
}