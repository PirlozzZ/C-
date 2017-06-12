﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VertificationLib
{
    public class VertificationShsmu:IVertification
    {
        static log4net.ILog log = log4net.LogManager.GetLogger("Log.Logging");//获取一个日志记录器
         
        public bool VertifyMethod(string loginno, string password)
        {
            bool result = false;
            log.Debug("VertificationShsmu VertifyMethod para value:" + loginno + "----" + password);
            //时间戳，为1970-1-1后流逝的Milliseconds，长整形字符串
            string timeStamp = ((long)((DateTime.Now - DateTime.Parse("1970-1-1")).TotalMilliseconds)).ToString();
            //公钥，由管理员指定
            string publicKey = "98985AA5E";

            string account = "182446";

            //desKey仅在加密密码时使用
            string desKey = "&(*D9sdE";
            string _password = DESEnCode("Pass@word", desKey);

            //发起Web请求的ip地址，如果为内网服务器，则为本机地址
            string ip = "202.120.143.246";
            //string ip = "202.120.143.78";
            string privateKey = account + _password + ip;

            //Token 加密方法为md5(privateKey + timeStamp + publicKey)
            //string token = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(privateKey + timeStamp + publicKey, "MD5");

            MD5 sha1Hash = MD5.Create();
            byte[] data = sha1Hash.ComputeHash(Encoding.UTF8.GetBytes(privateKey + timeStamp + publicKey));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            string token = sBuilder.ToString().ToUpper();

            string url = "http://202.120.143.81/WCF/Services/Authenticate/" + account + "/" + password + "/" + timeStamp + "/" + token;
            try
            {
                WebClient webClient = new WebClient();
                webClient.Proxy = null;
                webClient.Encoding = System.Text.Encoding.UTF8;
                log.Debug("VertificationShsmu VertifyMethod result value:" + webClient.DownloadString(url));
                if ("2".Equals(webClient.DownloadString(url)))
                {
                    result = true;
                }
            }
            catch (Exception e)
            {
                log.Error("VertificationShsmu VertifyMethod error!", e);
            }
            return result;
        }

        static string DESEnCode(string souce, string sKey)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            byte[] inputByteArray = Encoding.GetEncoding("UTF-8").GetBytes(souce);

            des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
            des.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);

            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();

            StringBuilder ret = new StringBuilder();
            foreach (byte b in ms.ToArray())
            {
                ret.AppendFormat("{0:X2}", b);
            }
            ret.ToString();
            return ret.ToString();
        }

        static string DESDeCode(string souce, string sKey)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            byte[] inputByteArray = new byte[souce.Length / 2];
            for (int x = 0; x < souce.Length / 2; x++)
            {
                int i = (Convert.ToInt32(souce.Substring(x * 2, 2), 16));
                inputByteArray[x] = (byte)i;
            }

            des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
            des.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
