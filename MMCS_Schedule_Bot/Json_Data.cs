using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using NLog;
 
namespace ScheduleBot
{
    public class Json_Data
    {
        static public Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Returns the date of last modification of UserDB file.
        /// </summary>
        /// <returns>The date.</returns>
        static public DateTime LastModified()
        {
            return File.GetLastWriteTime("UserDB.json");
        }
        /// <summary>
        /// Returns the date of last modification of UserDB file.
        /// </summary>
        /// <returns>The date.</returns>
        static public DateTime LastModifiedVK()
        {
            return File.GetLastWriteTime("UserVK_DB.json");
        }

        /// <summary>
        /// Saves the UserList to file.
        /// </summary>
        static public void WriteData()
        {
            File.WriteAllText("UserDB.json", JsonConvert.SerializeObject(Program.UserList, Formatting.Indented), Encoding.UTF8);
            logger.Info($"Записаны в файл данные {Program.UserList.Count} пользователей Telegram.");
            File.WriteAllText("UserVK_DB.json", JsonConvert.SerializeObject(Program.UserListVK, Formatting.Indented), Encoding.UTF8);
            logger.Info($"Записаны в файл данные {Program.UserListVK.Count} пользователей VK.");
        }

        /// <summary>
        /// Reads the UserList from file.
        /// </summary>
        static public void ReadData()
        {
            if (File.Exists("UserDB.json"))
            {
                Program.UserList = JsonConvert.DeserializeObject<Dictionary<long, User>>(File.ReadAllText("UserDB.json", Encoding.UTF8));
                logger.Info($"Прочитаны из файла данные {Program.UserList.Count} пользователей Telegram.");
            }
            if (File.Exists("UserVK_DB.json"))
            {
                Program.UserListVK = JsonConvert.DeserializeObject<Dictionary<long, User>>(File.ReadAllText("UserVK_DB.json", Encoding.UTF8));
                logger.Info($"Прочитаны из файла данные {Program.UserListVK.Count} пользователей VK.");
            }
        }
    }
}
