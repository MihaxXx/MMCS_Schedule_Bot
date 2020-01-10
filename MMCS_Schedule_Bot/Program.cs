using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using NLog;
using Telegram;
using Telegram.Bot.Types.Enums;

using API;
using Notify;

namespace ScheduleBot
{

    partial class Program
    {
        /// <summary>
        /// "-nopreload" - prevents loading shedules on start
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Json_Data.ReadData();
            KeyboardInit();
            TeachersInit();
            GradeInit();
            if (!(Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "-nopreload"))
            {
                TeachersShedInit();
                GroupShedListInit();
            }
            GetElectives();

            BOT = new Telegram.Bot.TelegramBotClient(ReadToken());
            logger.Info("Подключен бот.");
            BOT.OnMessage += BotOnMessageReceived;

            BOT.StartReceiving(new UpdateType[] { UpdateType.Message });
            Scheduler.RunNotifier().GetAwaiter().GetResult();
            logger.Info("Ожидает сообщений...");

            Console.CancelKeyPress += OnExit;
            _closing.WaitOne();
        }


        static public Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Used for teacher registration and finding teacher
        /// </summary>
        static private Dictionary<long, Teacher[]> NameMatches = new Dictionary<long, Teacher[]>();

        /// <summary>
        /// List of teachers
        /// </summary>
        static public Dictionary<int, Teacher> TeacherList = new Dictionary<int, Teacher>();

        /// <summary>
        /// Cached teachers schedule
        /// </summary>
        static public Dictionary<int, (List<(Lesson, List<Curriculum>, List<TechGroup>)>, DateTime)> TeacherSchedule = new Dictionary<int, (List<(Lesson, List<Curriculum>, List<TechGroup>)>, DateTime)>();

        /// <summary>
        /// List of grades
        /// </summary>
        static public List<Grade> GradeList = new List<Grade>();

        /// <summary>
        /// Cached student groups schedule
        /// </summary>
        static public Dictionary<int, (List<(Lesson, List<Curriculum>)>,DateTime)> GroupShedule = new Dictionary<int, (List<(Lesson, List<Curriculum>)>, DateTime)>();

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
            Json_Data.WriteData();
            logger.Info("Exit.");
            _closing.Set();
        }

        /// <summary>
        /// Gets list of techers
        /// </summary>
        static void TeachersInit(bool onstart = true)
        {
            try
            {
                TeacherList = TeacherMethods.GetTeachersList().ToDictionary(t0 => t0.id);
                logger.Info("Список преподавателей получен.");
            }
            catch (System.Net.WebException e)
            {
                if (onstart)
                    throw;
                logger.Warn("Не удалось обновить список преподавателей.", e);
            }
        }
        static void TeachersShedInit(bool onstart = true)
        {
            //Might take a few minutes to load them all
            try
            {
                logger.Info($"Начата загрузка расписаний {TeacherList.Count} преподавателей.");
                foreach (var teach in TeacherList)
                    TeacherSchedule[teach.Key] = (TeacherMethods.RequestWeekSchedule(teach.Key), DateTime.Now);
                logger.Info($"Завершена загрузка расписаний {TeacherSchedule.Count} преподавателей.");
            }
            catch (System.Net.WebException e)
            {
                if (onstart)
                    throw;
                logger.Warn("Не удалось обновить расписания преподавателей.", e);
            }
        }

        public static void WeekInitPlanned()
        {
            WeekInit(true);
        }

        public static void WeekInit(bool planned)
        {
            logger.Info("Updating curWeek...");
            logger.Info($"Cur week setting: {TimeOfLesson.curWeek}");
            Week curWeek = new Week();
            try
            {
                curWeek = CurrentSubject.RequestCurrentWeek();
            }
            catch(System.Net.WebException e)
            {
                logger.Warn("Не удалось обновить тип недели", e);
                if (planned)
                {
                    logger.Warn("Обновление типа недели плановое, поэтому будет установлен тип обратный нынешнему.");
                    curWeek.type = TimeOfLesson.curWeek.reversedtype();
                }
            }
            TimeOfLesson.curWeek = curWeek;
            logger.Info($"Cur week after setting: {TimeOfLesson.curWeek}");
        }

        /// <summary>
        /// Gets list of grades
        /// </summary>
        static void GradeInit(bool onstart = true)
        {
            try
            {
                GradeList = GradeMethods.GetGradesList().ToList();
                for (int i = 0; i < GradeList.Count; i++)
                {
                    GradeList[i].Groups = GradeMethods.GetGroupsList(GradeList[i].id).ToList();
                }
                logger.Info("Список курсов получен.");
            }
            catch (System.Net.WebException e)
            {
                if (onstart)
                    throw;
                logger.Warn("Не удалось обновить список курсов.", e);
            }
        }

        static void GroupShedListInit(bool onstart = true)
        {
            try
            {
                logger.Info("Начата загрузка расписаний групп.");
                foreach (var grade in GradeList)
                    foreach (var group in grade.Groups)
                        GroupShedule[group.id] = (StudentMethods.RequestWeekSchedule(group.id), DateTime.Now);
                logger.Info("Завершена загрузка расписаний групп.");
            }
            catch (System.Net.WebException e)
            {
                if (onstart)
                    throw;
                logger.Warn("Не удалось обновить расписание групп.", e);
            }
        }

        /// <summary>
        /// Gets list of electives
        /// </summary>
        static void GetElectives()
        {
            try
            {
                electives = Elective.GetElectives();
                logger.Info("Список факультативов получен.");
            }
            catch (FileNotFoundException e)
            {
                logger.Info("Список факультативов не был загружен!", e);
            }
            electivesStr = "[Раздел сайта с информацией о факультативах и кружках](http://www.mmcs.sfedu.ru/science/seminars)\n\n"+
                (electives == null ? "Нет данных о факультативах" : Elective.ElectivesToString(electives));
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
                logger.Info("File 'token.key' wasn't found in the working directory!\nPlease save Telegram BOT token to file named 'token.key'.", e);
                Environment.Exit(1);
            }
            return token;
        }
    }
}
