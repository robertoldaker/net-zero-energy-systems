using System.Security.Cryptography;

namespace SmartEnergyLabDataApi.Models;

public class Crypto {
    private static Crypto _instance;
    private static object _instanceLock = new object();    

    public static Crypto Instance {
        get {
            lock( _instanceLock) {
                if ( _instance == null ) {
                    _instance = new Crypto();
                }
            }
            return _instance;
        }
    }

    private Aes _aes;

    private Crypto() {
        _aes = Aes.Create();
        // Use fixed key in development so links last between restarts
        // in production new keys get created each server is restarted meaning links will not work across restarts
        if ( AppEnvironment.Instance.Context == CommonInterfaces.Models.Context.Development ) {
            var iv = System.Convert.FromBase64String("q3krqgoK0lxCg57lnWMPtw==");
            var key = System.Convert.FromBase64String("IjcrToSIFlUg+OX0h/5RKiRqhvrY/g4/kyvcw3yvyek=");
            _aes.IV = iv;
            _aes.Key = key;
        }
    }

    public string EncryptAsBase64(string input)
    {
        var encryptor = _aes.CreateEncryptor();
        //
        // Create the streams used for encryption. 
        byte[] encryptedData;
        using (MemoryStream msEncrypt = new MemoryStream())
        {
            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    //Write all data to the stream.
                    swEncrypt.Write(input);
                }
                encryptedData = msEncrypt.ToArray();
            }
        }
        return System.Convert.ToBase64String(encryptedData);
    }

    public string DecryptFromBase64(string input)
    {
        var decryptor = _aes.CreateDecryptor();
        var cipherText = System.Convert.FromBase64String(input);
        string decryptedString;
        using (MemoryStream msDecrypt = new MemoryStream(cipherText))
        {
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            {
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {

                    // Read the decrypted bytes from the decrypting stream 
                    // and place them in a string.
                    decryptedString = srDecrypt.ReadToEnd();
                }
            }
        }
        return decryptedString;
    }

}
 