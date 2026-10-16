using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace CakeGandom.Services;

public class PhoneProtector
{
    private readonly byte[] _key;

    public PhoneProtector(IConfiguration cfg)
    {
        var raw=cfg["Encryption:Key"]??"default-key-change-me";
        _key=SHA256.HashData(Encoding.UTF8.GetBytes(raw));
    }

    public string Encrypt(string plain)
    {
        if(string.IsNullOrEmpty(plain)) return string.Empty;
        using var aes=Aes.Create();
        aes.Key=_key;
        aes.GenerateIV();
        var iv=aes.IV;
        using var enc=aes.CreateEncryptor();
        var bytes=Encoding.UTF8.GetBytes(plain);
        var encrypted=enc.TransformFinalBlock(bytes,0,bytes.Length);
        var result=new byte[iv.Length+encrypted.Length];
        Buffer.BlockCopy(iv,0,result,0,iv.Length);
        Buffer.BlockCopy(encrypted,0,result,iv.Length,encrypted.Length);
        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        if(string.IsNullOrEmpty(cipherText)) return string.Empty;
        try
        {
            var data=Convert.FromBase64String(cipherText);
            if(data.Length<16) return string.Empty;
            using var aes=Aes.Create();
            aes.Key=_key;
            var iv=new byte[16];
            Buffer.BlockCopy(data,0,iv,0,16);
            aes.IV=iv;
            using var dec=aes.CreateDecryptor();
            var decrypted=dec.TransformFinalBlock(data,16,data.Length-16);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return string.Empty;
        }
    }
}