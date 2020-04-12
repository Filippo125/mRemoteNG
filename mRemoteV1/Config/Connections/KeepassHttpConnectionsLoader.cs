using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Config.DataProviders;
using mRemoteNG.Config.Serializers;
using mRemoteNG.Config.Serializers.MsSql;
using mRemoteNG.Config.Serializers.Versioning;
using mRemoteNG.Container;
using mRemoteNG.Security;
using mRemoteNG.Security.Authentication;
using mRemoteNG.Security.SymmetricEncryption;
using mRemoteNG.Tools;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;

namespace mRemoteNG.Config.Connections
{
    public class KeepassHttpConnectionsLoader : IConnectionsLoader
    {

        static readonly HttpClient client = new HttpClient();

        private static readonly Encoding encoding = Encoding.UTF8;
        private readonly string _url = "http://localhost:19455";
        private readonly string uid = "";
        private readonly byte[] key = Convert.FromBase64String("");
        private readonly string dbHash = "";



        private readonly IDeserializer<string, IEnumerable<LocalConnectionPropertiesModel>>
            _localConnectionPropertiesDeserializer;

        private readonly IDataProvider<string> _dataProvider;

        public Func<Optional<SecureString>> AuthenticationRequestor { get; set; } =
            () => MiscTools.PasswordDialog("", false);

        public KeepassHttpConnectionsLoader(
            IDeserializer<string, IEnumerable<LocalConnectionPropertiesModel>> localConnectionPropertiesDeserializer,
            IDataProvider<string> dataProvider)
        {
            _localConnectionPropertiesDeserializer =
                localConnectionPropertiesDeserializer.ThrowIfNull(nameof(localConnectionPropertiesDeserializer));
            _dataProvider = dataProvider.ThrowIfNull(nameof(dataProvider));
        }

        public ConnectionTreeModel Load()
        {
            var entriesList = GetAllLogins();
            //var dataTable = dataProvider.Load();
            //var deserializer = new DataTableDeserializer(cryptoProvider, decryptionKey.First());
            //var connectionTree = deserializer.Deserialize(dataTable);
            //ApplyLocalConnectionProperties(connectionTree.RootNodes.First(i => i is RootNodeInfo));
            //return connectionTree;
            return null;
        }

        private Optional<SecureString> GetDecryptionKey(SqlConnectionListMetaData metaData)
        {
            var cryptographyProvider = new LegacyRijndaelCryptographyProvider();
            var cipherText = metaData.Protected;
            var authenticator = new PasswordAuthenticator(cryptographyProvider, cipherText, AuthenticationRequestor);
            var authenticated =
                authenticator.Authenticate(new RootNodeInfo(RootNodeType.Connection).DefaultPassword
                                                                                    .ConvertToSecureString());

            if (authenticated)
                return authenticator.LastAuthenticatedPassword;
            return Optional<SecureString>.Empty;
        }

        private void ApplyLocalConnectionProperties(ContainerInfo rootNode)
        {
            var localPropertiesXml = _dataProvider.Load();
            var localConnectionProperties = _localConnectionPropertiesDeserializer.Deserialize(localPropertiesXml);

            rootNode
                .GetRecursiveChildList()
                .Join(localConnectionProperties,
                      con => con.ConstantID,
                      locals => locals.ConnectionId,
                      (con, locals) => new {Connection = con, LocalProperties = locals})
                .ForEach(x =>
                {
                    x.Connection.PleaseConnect = x.LocalProperties.Connected;
                    x.Connection.Favorite = x.LocalProperties.Favorite;
                    if (x.Connection is ContainerInfo container)
                        container.IsExpanded = x.LocalProperties.Expanded;
                });
        }


        public Dictionary<string, object> DoRequest(Dictionary<string, object> request)
        {
            try
            {
                RijndaelManaged aes = new RijndaelManaged();
                aes.KeySize = 256;
                //aes.BlockSize = 128;
                //aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                aes.Key = key;
                aes.GenerateIV();
                string base64IV = Convert.ToBase64String(aes.IV);
                ICryptoTransform AESEncrypt = aes.CreateEncryptor(aes.Key, aes.IV);

                //KeePassHttpClientRequest keepassrequest = new KeePassHttpClientRequest();
                //keepassrequest.Nonce = base64IV;
                //keepassrequest.Verifier = base64IV;
                //keepassrequest.RequestType = "test-associate";
                //keepassrequest.Id = uid;

                // Build request
                var keyValues = new Dictionary<string, object>
                 {
                     { "Nonce", base64IV },
                     { "Verifier", base64IV },
                     { "Id", uid },
                 };
                foreach (string s in request.Keys)
                {
                    keyValues.Add(s, request[s]);
                }
                //keepassrequest.Encrypt(AESEncrypt);
                //var keepassRequestJson = new JavaScriptSerializer().Serialize(keepassrequest);

                var jsonRequest = EncryptRequest(keyValues, AESEncrypt);
                byte[] byteRequest = encoding.GetBytes(jsonRequest);


                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(_url);
                myReq.Method = "POST";
                myReq.ContentLength = byteRequest.Length;
                using (var stream = myReq.GetRequestStream())
                {
                    stream.Write(byteRequest, 0, byteRequest.Length);
                }
                Console.WriteLine(jsonRequest);
                var response = (HttpWebResponse)myReq.GetResponse();
                Console.WriteLine(response.StatusCode);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    return new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(responseString);
                }
                throw new Exception("Status code is not ok");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error encrypting: " + e.Message);
                throw new Exception("Error encrypting: " + e.Message);
            }
        }

        private string EncryptRequest(Dictionary<string, object> request, ICryptoTransform crypto)
        {
            byte[] buffer = encoding.GetBytes((string)request["Verifier"]);
            byte[] criptoVerifier = crypto.TransformFinalBlock(buffer, 0, buffer.Length);
            request["Verifier"] = Convert.ToBase64String(criptoVerifier);
            // Se presente Url => cryptare
            if (request.ContainsKey("Url"))
            {
                buffer = encoding.GetBytes((string)request["Url"]);
                criptoVerifier = crypto.TransformFinalBlock(buffer, 0, buffer.Length);
                request["Url"] = Convert.ToBase64String(criptoVerifier);
            }

            return new JavaScriptSerializer().Serialize(request);
        }

        public bool Authenticate()
        {
            var dict = new Dictionary<string, object>();
            dict.Add("RequestType", "test-associate");
            var response = DoRequest(dict);
            var success = Convert.ToBoolean(response["Success"]);
            return success == true;
        }

        public KeePassEntry GetLogin(string name)
        {
            var dict = new Dictionary<string, object>();
            dict.Add("RequestType", "get-logins");
            dict.Add("Url", name);
            dict.Add("SortSelection", false);
            var response = DoRequest(dict);
            Console.WriteLine(response);
            var entries = Decrypt(response);
            if (entries.Count >= 1)
            {
                return entries[0];
            }
            return new KeePassEntry();
        }

        public List<KeePassEntry> GetAllLogins()
        {
            var dict = new Dictionary<string, object>();
            dict.Add("RequestType", "get-all-logins");
            dict.Add("SortSelection", false);
            var response = DoRequest(dict);
            Console.WriteLine(response);
            var entries = Decrypt(response);
            return entries;
        }

        private List<KeePassEntry> Decrypt(Dictionary<string, object> cryptoDict)
        {
            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Key = key;
            var nonce = (string)cryptoDict["Nonce"];
            aes.IV = Convert.FromBase64String(nonce);

            var signature = Convert.FromBase64String((string)cryptoDict["Verifier"]);
            var decrypto = aes.CreateDecryptor(aes.Key, aes.IV);
            var verifier = encoding.GetString(decrypto.TransformFinalBlock(signature, 0, signature.Length));
            // if ((verifier != nonce) || (uid != cryptoDict["Id"]) || (dbHash != cryptoDict["Hash"] ))
            // {
            //     Console.WriteLine("Error decrypting");
            // }
            List<KeePassEntry> newEntries = new List<KeePassEntry>();
            if (cryptoDict.ContainsKey("Entries"))
            {

                // Decrypt entries
                foreach (var obj in (ArrayList)cryptoDict["Entries"])
                {
                    var entry = (Dictionary<string, object>)obj;
                    KeePassEntry keePassEntry = new KeePassEntry();
                    var buffer = Convert.FromBase64String((string)entry["Login"]);
                    keePassEntry.Login = encoding.GetString(decrypto.TransformFinalBlock(buffer, 0, buffer.Length));

                    buffer = Convert.FromBase64String((string)entry["Password"]);
                    keePassEntry.Password = encoding.GetString(decrypto.TransformFinalBlock(buffer, 0, buffer.Length));

                    buffer = Convert.FromBase64String((string)entry["Uuid"]);
                    keePassEntry.Uuid = encoding.GetString(decrypto.TransformFinalBlock(buffer, 0, buffer.Length));

                    buffer = Convert.FromBase64String((string)entry["Name"]);
                    keePassEntry.Name = encoding.GetString(decrypto.TransformFinalBlock(buffer, 0, buffer.Length));

                    newEntries.Add(keePassEntry);
                }

            }
            return newEntries;
        }
    }
    public class KeePassEntry
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string Uuid { get; set; }
        public string Name { get; set; }


    }
}