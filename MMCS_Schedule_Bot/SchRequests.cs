using System.Text;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System;
using Newtonsoft.Json;

namespace SchRequests
{
    /// <summary>
    /// Используем вебреквест
    /// </summary>
    public class TimedWebClient : WebClient
    {
        // Timeout in milliseconds, default = 600,000 msec
        public int Timeout { get; set; }

        public TimedWebClient()
        {
            this.Timeout = 600000;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var objWebRequest = base.GetWebRequest(address);
            objWebRequest.Timeout = this.Timeout;
            return objWebRequest;
        }
    }
    public class SchRequests
    {
        /// <summary>
		/// Changes the encoding of <paramref name="response"/> from <paramref name="from"/> to <paramref name="to"/>
		/// </summary>
		/// <param name="response">String where to change encoding</param>
		/// <param name="from">Initial enc, def. - 1251</param>
		/// <param name="to">Resulting enc, def.- UTF-8</param>
		public static void ChangeEncoding(ref string response, int from = 1251, int to = 65001)
        {
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			Encoding fromEncodind = Encoding.GetEncoding(from); //из какой кодировки
            byte[] bytes = fromEncodind.GetBytes(response);
            Encoding toEncoding = Encoding.GetEncoding(to); //в какую кодировку
            response = toEncoding.GetString(bytes);
        }

        /// <summary>
        /// Выполнение запроса и возврат строки-ответа
        /// </summary>
        public static string Request(string url)
        {
            // Выполняем запрос по адресу и получаем ответ в виде строки (Используем вебреквест!)
            string response = new TimedWebClient { Timeout = 3000 }.DownloadString(url);
            // Исправляем кодировку
            //ChangeEncoding(ref response);
            // Возвращаем строку-ответ (формат JSON)
            return response;
        }


        /// <summary>
        /// Десериализация в объект по строке
        /// </summary>
        public static T DeSerializationObjFromStr<T>(string str)
        {
            return JsonConvert.DeserializeObject<T>(str);
        }

        /// <summary>
        /// Десериализация в массив объектов по строке
        /// </summary>
        public static T[] DeSerializationFromStr<T>(string str)
        {
            return JsonConvert.DeserializeObject<T[]>(str);
        }
    }
}
