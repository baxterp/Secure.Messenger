using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Soap;
using System.Security.Cryptography;
using Secure.Messenger.Shared;

namespace Secure.Messenger.Server
{
    public class CryptoHelper
    {
        public static Boolean SendData(NetworkStream strm, MessageData mes)
        {
            IFormatter formatter = new SoapFormatter();
            MemoryStream memstrm = new MemoryStream();
            TripleDESCryptoServiceProvider tdes = null;
            CryptoStream csw = null;

            try
            {
                byte[] Key = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
                byte[] IV = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };

                tdes = new TripleDESCryptoServiceProvider();
                csw = new CryptoStream(memstrm, tdes.CreateEncryptor(Key, IV), CryptoStreamMode.Write);

                formatter.Serialize(csw, mes);
                csw.FlushFinalBlock();
                byte[] data = memstrm.GetBuffer();
                int memsize = (int)memstrm.Length;
                byte[] size = BitConverter.GetBytes(memsize);
                strm.Write(size, 0, 4);
                strm.Write(data, 0, (int)memsize);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                strm.Flush();
                csw.Close();
                memstrm.Close();
            }
        }

        public static MessageData ReceiveData(NetworkStream strm)
        {
            MemoryStream memstrm = new MemoryStream();

            byte[] Key = {0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16};
            byte[] IV = {0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16};

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            CryptoStream csw = new CryptoStream(memstrm, tdes.CreateDecryptor(Key, IV),
                               CryptoStreamMode.Write);

            byte[] data = new byte[2048];
            int recv = strm.Read(data, 0, 4);
            int size = BitConverter.ToInt32(data, 0);
            int offset = 0;
            while (size > 0)
            {
                recv = strm.Read(data, 0, size);
                csw.Write(data, offset, recv);
                offset += recv;
                size -= recv;
            }
            csw.FlushFinalBlock();
            IFormatter formatter = new SoapFormatter();
            memstrm.Position = 0;

            MessageData mes = new MessageData(string.Empty);

            try
            {
                mes = (MessageData)formatter.Deserialize(memstrm);
            }
            catch (SerializationException ex)
            {
                return null;
            }

            memstrm.Close();
            return mes;
        }
    }
}
