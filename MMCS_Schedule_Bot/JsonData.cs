using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using NLog;

namespace ScheduleBot
{
    public static class JsonData
    {
        private static readonly string PathToDb = Environment.GetEnvironmentVariable("MMCS_BOT_PATH_TO_DB") ?? "";
        private static readonly string TgDataFilename = Path.Combine(PathToDb, "UserDB.json");
        private static readonly string VkDataFilename = Path.Combine(PathToDb, "UserVK_DB.json");

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        /// <summary>
        /// Returns the date of last modification of UserDB file.
        /// </summary>
        /// <returns>The date.</returns>
        public static DateTime LastModified()
        {
            return File.GetLastWriteTime(TgDataFilename);
        }
        /// <summary>
        /// Returns the date of last modification of UserDB file.
        /// </summary>
        /// <returns>The date.</returns>
        public static DateTime LastModifiedVK()
        {
            return File.GetLastWriteTime(VkDataFilename);
        }

        /// <summary>
        /// Saves the UserList to file.
        /// </summary>
        public static void WriteData()
        {
            File.WriteAllText(TgDataFilename, JsonConvert.SerializeObject(Program.UserList, Formatting.Indented), Encoding.UTF8);
            Logger.Info($"Записаны в файл данные {Program.UserList.Count} пользователей Telegram.");
            File.WriteAllText(VkDataFilename, JsonConvert.SerializeObject(Program.UserListVK, Formatting.Indented), Encoding.UTF8);
            Logger.Info($"Записаны в файл данные {Program.UserListVK.Count} пользователей VK.");
        }

        /// <summary>
        /// Reads the UserList from file.
        /// </summary>
        public static void ReadData()
        {
            if (File.Exists(TgDataFilename))
            {
                Program.UserList = JsonConvert.DeserializeObject<Dictionary<long, User>>(File.ReadAllText(TgDataFilename, Encoding.UTF8));
                Logger.Info($"Прочитаны из файла данные {Program.UserList.Count} пользователей Telegram.");
            }
            if (File.Exists(VkDataFilename))
            {
                Program.UserListVK = JsonConvert.DeserializeObject<Dictionary<long, User>>(File.ReadAllText(VkDataFilename, Encoding.UTF8));
                Logger.Info($"Прочитаны из файла данные {Program.UserListVK.Count} пользователей VK.");
            }
        }
    }
}
