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
        private string uid = "Filippo";
        private byte[] key = Convert.FromBase64String("tABX2MnvHGnE9PW5YCTfnrh3rYmqTbdDEqtYtsQg/r0=");
        private string dbHash = "83f864a3b4f2fd3f9e4d4c80424ee84f10d22526";


        public KeePassHttpClient()
        {
            LoadConfig();
            Authenticate();
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
            var entries = DecryptRequest(response);
            if (entries.Count >= 1)
            {
                return entries[0];
            }
            return new KeePassEntry();
        }

        private List<KeePassEntry> DecryptRequest(Dictionary<string, object> cryptoDict)
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
            if ((verifier != nonce) || (uid != cryptoDict["Id"]) || (dbHash != cryptoDict["Hash"] ))
            {
                Console.WriteLine("Error decrypting");
            }
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
    }
}
