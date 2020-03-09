using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;

using Yandex.Music.Api.Requests;

namespace Yandex.Music.Api.Common
{
    /// <summary>
    /// ���� ����������
    /// </summary>
    public enum YAuthStorageEncryption
    {
        /// <summary>
        /// ��� ����������
        /// </summary>
        None,
        /// <summary>
        /// Rijndael
        /// </summary>
        Rijndael
    }

    /// <summary>
    ///     ��������� ������ ������������
    /// </summary>
    public class YAuthStorage
    {
        #region ����

        private readonly YAuthStorageEncryption encryption;
        private readonly Encryptor encryptor;

        #endregion ����

        #region ��������

        /// <summary>
        /// ���� �����������
        /// </summary>
        public bool IsAuthorized { get; internal set; }

        /// <summary>
        /// Http-��������
        /// </summary>
        public HttpContext Context { get; }

        /// <summary>
        /// ������������
        /// </summary>
        public YUser User { get; set; }

        #endregion ��������

        #region ��������������� �������

        #endregion ��������������� �������

        #region �������� �������

        /// <summary>
        /// �����������
        /// </summary>
        /// <param name="login">�����</param>
        /// <param name="password">������</param>
        /// <param name="usedEncryption">��� ���������� �����</param>
        public YAuthStorage(string login, string password, YAuthStorageEncryption usedEncryption = YAuthStorageEncryption.None)
        {
            User = new YUser {
                Login = login,
                Password = password
            };

            Context = new HttpContext();

            // ����������
            encryptor = new Encryptor($"{User.Login}|{User.Password}");
            encryption = usedEncryption;
        }

        /// <summary>
        /// ��������� ������ ��� �������������
        /// </summary>
        /// <param name="proxy">������</param>
        public void SetProxy(IWebProxy proxy)
        {
            Context.WebProxy = proxy;
        }

        /// <summary>
        /// ���������� �����
        /// </summary>
        /// <param name="fileName">��� �����</param>
        /// <returns></returns>
        public bool Save(string fileName)
        {
            try {
                File.Delete(fileName);

                byte[] bytes;
                using (var ms = new MemoryStream()) {
                    var bf = new BinaryFormatter();
                    bf.Serialize(ms, Context.Cookies);

                    bytes = ms.ToArray();
                }

                switch (encryption) {
                    case YAuthStorageEncryption.Rijndael:
                    {
                        bytes = encryptor.Encrypt(bytes);
                        break;
                    }
                }

                using (var fs = new FileStream(fileName, FileMode.Create)) {
                    fs.Write(bytes, 0, bytes.Length);
                }

                return true;
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
                return false;
            }
        }

        /// <summary>
        /// �������� �����
        /// </summary>
        /// <param name="fileName">��� �����</param>
        /// <returns></returns>
        public bool Load(string fileName)
        {
            try {
                if (!File.Exists(fileName))
                    return false;

                byte[] bytes;

                using (var fs = new FileStream(fileName, FileMode.Open)) {
                    bytes = new byte[fs.Length];
                    fs.Read(bytes, 0, bytes.Length);
                }

                switch (encryption) {
                    case YAuthStorageEncryption.Rijndael:
                    {
                        bytes = encryptor.Decrypt(bytes);
                        break;
                    }
                }

                using (var ms = new MemoryStream(bytes)) {
                    var bf = new BinaryFormatter();
                    Context.Cookies = (CookieContainer) bf.Deserialize(ms);
                }

                return true;
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
                return false;
            }
        }

        #endregion �������� �������
    }
}