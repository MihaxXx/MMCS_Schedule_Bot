using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Threading;
using static System.Console;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

using API;
using Notify;

namespace Console_Schedule_Bot
{
   
    partial class Program
    {
        static void Main(string[] args)
        {
            Json_Data.ReadData();
            KeyboardInit();
            TeachersInit();
            GradeInit();
            GetElectives();




                BOT = new Telegram.Bot.TelegramBotClient(ReadToken());
            WriteLine("Подключен бот.");
            BOT.OnMessage += BotOnMessageReceived;

            BOT.StartReceiving(new UpdateType[] { UpdateType.Message });
            WriteLine("Ожидает сообщений...");
            Scheduler.RunNotifier().GetAwaiter().GetResult();
            Console.CancelKeyPress += new ConsoleCancelEventHandler(OnExit);
            _closing.WaitOne();
        }

        /// <summary>
        /// Used for teacher registration and finding teacher
        /// </summary>
        static private Dictionary<long, Teacher[]> NameMatches = new Dictionary<long, Teacher[]>();

        /// <summary>
        /// List of teachers
        /// </summary>
        static public Dictionary<int, Teacher> TeacherList = new Dictionary<int, Teacher>();

        /// <summary>
        /// List of grades
        /// </summary>
        static public Grade[] GradeList;

        /// <summary>
        /// Bot instance to interact with Telegram
        /// </summary>
        static Telegram.Bot.TelegramBotClient BOT;

        /// <summary>
        /// User DB by Telegram IDs
        /// </summary>
        static public Dictionary<long, User> UserList = new Dictionary<long, User>();

        private static readonly AutoResetEvent _closing = new AutoResetEvent(false);

        protected static void OnExit(object sender, ConsoleCancelEventArgs args)
        {
            BOT.StopReceiving();
            WriteLine("Exit.");
            _closing.Set();
        }

        /// <summary>
        /// Gets list of techers
        /// </summary>
        static void TeachersInit()
        {
            var t = TeacherMethods.GetTeachersList();
            foreach (Teacher x in t)
                TeacherList.Add(x.id, x);
            WriteLine("Список преподавателей получен.");
        }

        /// <summary>
        /// Gets list of grades
        /// </summary>
        static void GradeInit()
        {
            GradeList = GradeMethods.GetGradesList();
            for (int i = 0; i < GradeList.Length; i++)
            {
                GradeList[i].Groups = GradeMethods.GetGroupsList(GradeList[i].id);
            }
            WriteLine("Список курсов получен.");
        }

        /// <summary>
        /// Gets list of electives
        /// </summary>
        static void GetElectives()
        {
            try
            {
                Program.electives = Elective.GetElectives();
                WriteLine("Список факультативов получен.");
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine("Список факультативов не был загружен!");
            }
            electivesStr = electives == null ? "Нет данных о факультативах" : Elective.ElectivesToString(electives);
        }

        /// <summary>
        /// Reads the bot token from file 'token.key'.
        /// </summary>
        /// <returns>The token.</returns>
        public static string ReadToken()
        {
            string token = string.Empty;
            try
            {
                token = File.ReadAllText("token.key", Encoding.UTF8);
            }
            catch (FileNotFoundException e)
            {
                WriteLine("File 'token.key' wasn't found in the working directory!\nPlease save Telegram BOT token to file named 'token.key'.");
                WriteLine(e.Message);
                Environment.Exit(1);
            }
            return token;
        }
    }
}
