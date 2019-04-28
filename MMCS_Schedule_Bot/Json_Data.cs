using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace Console_Schedule_Bot
{
    class Json_Data
    {
        /// <summary>
        /// Returns the date of last modification of UserDB file.
        /// </summary>
        /// <returns>The date.</returns>
        static public DateTime LastModified()
        {
            return File.GetLastWriteTime("UserDB.json");
        }

        /// <summary>
        /// Saves the UserList to file.
        /// </summary>
        static public void WriteData()
        {
            File.WriteAllText("UserDB.json", JsonConvert.SerializeObject(Program.UserList, Formatting.Indented), Encoding.UTF8);
        }

        /// <summary>
        /// Reads the UserList from file.
        /// </summary>
        static public void ReadData()
        {
            if (File.Exists("UserDB.json"))
            {
                Program.UserList = JsonConvert.DeserializeObject<Dictionary<long, User>>(File.ReadAllText("UserDB.json", Encoding.UTF8));
            }

        }
    }
}
