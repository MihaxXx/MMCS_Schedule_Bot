using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Console_Schedule_Bot
{
    class Json_Data
    {
        public User[] User { get; set; }

        static private string serialized;

        static public void WriteData()
        {
            Json_Data myCollection = new Json_Data();
            myCollection.User = new User[Program.UserList.Count];

            int i = 0;
            foreach (KeyValuePair<long, User> us in Program.UserList)
            {
                myCollection.User[i] = new User()
                {
                    ident = us.Value.ident,
                    Info = us.Value.Info,
                    teacherId = us.Value.teacherId,
                    id = us.Value.id,
                    groupid = us.Value.groupid

                };
                i++;
            }

            serialized = JsonConvert.SerializeObject(myCollection);
            if (serialized.Count() > 1)
            {
                if (!File.Exists("Json_Data"))
                    File.Create("Json_Data").Close();
                File.WriteAllText("Json_Data", serialized, Encoding.UTF8);
            }


        }

        static public void ReadData()
        {
            if (File.Exists("Json_Data"))
            {
                serialized = File.ReadAllText("Json_Data", Encoding.UTF8);
                dynamic json = JObject.Parse(serialized);

                for (int i = 0; i < json.User.Count; i++)
                {
                    User x = new User
                    {
                        ident = json.User[i].ident,
                        id = json.User[i].id,
                        teacherId = json.User[i].teacherId,
                        Info = json.User[i].Info,
                        groupid = json.User[i].groupid
                    };
                    Program.UserList.Add(x.id, x);
                }
            }

        }
    }
}
