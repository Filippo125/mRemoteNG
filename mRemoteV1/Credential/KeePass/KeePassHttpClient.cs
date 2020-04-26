using mRemoteNG.Config.Connections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;



namespace mRemoteNG.Credential.KeePass
{

    class KeePassHttpClient
    {
        static private readonly string SETTINGSFILE = "a2VlcGFzc3NldHRpbmdz";
        static private readonly int NSETTINGS = 4;
        static readonly HttpClient client = new HttpClient();

        private static readonly Encoding encoding = Encoding.UTF8;
        private string url = "http://localhost:19455";
        private string uid = "";
        private byte[] key = Convert.FromBase64String("");
        private string dbHash = "";


        public KeePassHttpClient()
        {
            LoadConfig();
            Authenticate();
        }

        public KeePassHttpClient(bool newConfig)
        {
            if (! newConfig)
            {
                LoadConfig();
                Authenticate();
            }else
            {
                SaveNewConfig("http://localhost:19455");
            }
        }

        private Dictionary<string, object> DoRequest(Dictionary<string, object> request)
        {
            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Key = key;
            return DoRequest(request, aes, uid, url);
        }
        private Dictionary<string, object> DoRequest(Dictionary<string, object> request, RijndaelManaged aes, string uid, string url)
        {
            try
            {
                
                aes.GenerateIV();
                string base64IV = Convert.ToBase64String(aes.IV);
                ICryptoTransform AESEncrypt = aes.CreateEncryptor(aes.Key, aes.IV);

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

                var jsonRequest = EncryptRequest(keyValues, AESEncrypt);
                byte[] byteRequest = encoding.GetBytes(jsonRequest);


                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(url);
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
                    var responseDict = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(responseString);
                    return DecryptRequest(responseDict, aes);
                }
                throw new Exception("Status code is not ok");
            }
            catch (Exception e)
            {
                throw new Exception("Error: " + e.Message);
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
            List<object> entries = (List<object>)response["Entries"];
            if (entries.Count >= 1)
            {
                var entry =(Dictionary<string,object>)entries[0];
                return new KeePassEntry((string)entry["Name"], (string)entry["Uuid"], (string)entry["Login"], (string)entry["Password"]);
            }
            return new KeePassEntry();
        }

        private object IDecryptObject(object cryptoObject,ICryptoTransform decrypto)
        {
            byte[] buffer;
            if (cryptoObject is Dictionary<string,object>)
            {
                Dictionary<string, object> cryptoDict = (Dictionary<string, object>)cryptoObject;
                Dictionary<string, object> newDict = new Dictionary<string, object>();
                foreach (string key in cryptoDict.Keys)
                {
                    newDict[key] = IDecryptObject(cryptoDict[key], decrypto);
                }
                return newDict;
            }
            else if (cryptoObject is ArrayList || cryptoObject is List<object> )
            {
                var cryptoList = (ArrayList) cryptoObject;
                List<object> newList = new List<object>();
                foreach (var obj in (ArrayList)cryptoObject)
                    newList.Add(IDecryptObject(obj, decrypto));
                return newList;
            }
            else if(cryptoObject is string)
            {
                try
                {
                    buffer = Convert.FromBase64String((string)cryptoObject);
                    return encoding.GetString(decrypto.TransformFinalBlock(buffer, 0, buffer.Length));
                }
                catch (Exception) {}
            }
            return cryptoObject;
        }

        private Dictionary<string,object> DecryptRequest(Dictionary<string, object> cryptoDict, RijndaelManaged aes)
        {
            var nonce = (string)cryptoDict["Nonce"];
            aes.IV = Convert.FromBase64String(nonce);

            var signature = Convert.FromBase64String((string)cryptoDict["Verifier"]);
            var decrypto = aes.CreateDecryptor(aes.Key, aes.IV);
            var verifier = encoding.GetString(decrypto.TransformFinalBlock(signature, 0, signature.Length));
            if (!(cryptoDict["RequestType"].Equals("associate")) && ((verifier != nonce) || (!uid.Equals(cryptoDict["Id"])) || (!dbHash.Equals(cryptoDict["Hash"]))))
            {
                throw new Exception("Error decrypting keepass response");
            }
            var o = IDecryptObject(cryptoDict, decrypto);
            return (Dictionary<string, object>) o;


            //return cryptoDict;
            //return newEntries;
        }

        private Dictionary<string, object> PDecryptRequest(Dictionary<string, object> cryptoDict, RijndaelManaged aes)
        {
            List<KeePassEntry> newEntries = new List<KeePassEntry>();
            var nonce = (string)cryptoDict["Nonce"];
            aes.IV = Convert.FromBase64String(nonce);

            var signature = Convert.FromBase64String((string)cryptoDict["Verifier"]);
            var decrypto = aes.CreateDecryptor(aes.Key, aes.IV);
            var verifier = encoding.GetString(decrypto.TransformFinalBlock(signature, 0, signature.Length));
            if ((verifier != nonce) || (uid.Equals(cryptoDict["Id"])) || (dbHash.Equals(cryptoDict["Hash"])))
            {
                throw new Exception("Error decrypting keepass response");
            }

            foreach (KeyValuePair<string, object> kvp in cryptoDict)
            {

            }
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
            return null;
            //return newEntries;
        }


        private void LoadConfig()
        {
            List<string> lineList = new List<string>();
            // Open file with settings of keepass http comunication
            using (FileStream fs = File.Open("./" + SETTINGSFILE, FileMode.Open))
            {
                StreamReader streamReader = new StreamReader(fs);
                while (!streamReader.EndOfStream)
                {
                    lineList.Add(streamReader.ReadLine());

                }
                streamReader.Close();
            }
            if (lineList.Count < NSETTINGS)
            {
                throw new Exception("The keepass settings file does not contains enough element");
            }
            uid = encoding.GetString(Convert.FromBase64String(lineList[0]));
            key = Convert.FromBase64String(lineList[1]);
            dbHash = encoding.GetString(Convert.FromBase64String(lineList[2]));
            url = encoding.GetString(Convert.FromBase64String(lineList[3]));
        }

        private void SaveNewConfig(string url)
        {
            List<string> lineList = new List<string>();
            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.GenerateKey();
            var dict = new Dictionary<string, object>();
            dict.Add("RequestType", "associate");
            dict.Add("Key", Convert.ToBase64String(aes.Key));
            var response = DoRequest(dict,aes,"",url);

            lineList.Add(Convert.ToBase64String(encoding.GetBytes((string)response["Id"])));
            lineList.Add(Convert.ToBase64String(aes.Key));
            lineList.Add(Convert.ToBase64String(encoding.GetBytes((string)response["Hash"])));
            lineList.Add(Convert.ToBase64String(encoding.GetBytes(url)));
            System.IO.File.WriteAllLines(@"./" + SETTINGSFILE, lineList);
            
        }

        public static bool ExistsConfig()
        {
            return System.IO.File.Exists(@"./" + SETTINGSFILE);
        }
    }
}
