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

namespace Console_Schedule_Bot
{
    class Program
    {
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
        /// Checks if user is already registered
        /// </summary>
        /// <param name="id">Telegram user ID</param>
        /// <returns></returns>
        static bool IsRegistered(long id) => UserList.ContainsKey(id) && UserList[id].ident > 2;

        /// <summary>
        /// Bot instance to interact with Telegram
        /// </summary>
        static Telegram.Bot.TelegramBotClient BOT;

        /// <summary>
        /// User DB by Telegram IDs
        /// </summary>
        static public Dictionary<long, User> UserList = new Dictionary<long, User>();

        /// <summary>
		/// Keyboard for registered users
		/// </summary>
        static ReplyKeyboardMarkup studentKeyboard = new ReplyKeyboardMarkup(new[] {
                            new[]{ new KeyboardButton("Ближайшая пара"),new KeyboardButton("Расписание на сегодня") },      //Кастомная клава для студентов
                            new[]{ new KeyboardButton("Расписание на неделю"),new KeyboardButton("Помощь") }
                            }
                        );

        static ReplyKeyboardMarkup teacherKeyboard = new ReplyKeyboardMarkup(new[] {
                            new[]{ new KeyboardButton("Ближайшая пара"),new KeyboardButton("Расписание на сегодня") },      //Кастомная клава для препода
                            new[]{ new KeyboardButton("Расписание на неделю"),new KeyboardButton("Помощь") }
                            }
                        );

        static ReplyKeyboardMarkup registrationKeyboard = new ReplyKeyboardMarkup(new[] {
                            new[]{ new KeyboardButton("Бакалавр"),new KeyboardButton("Магистр") },      //Кастомная клава для регистрации
                            new[]{ new KeyboardButton("Аспирант"),new KeyboardButton("Преподаватель") }
                            }
                        );
        /// <summary>
        /// Some options for keyboards
        /// </summary>
        static void KeyboardInit()
        {
            studentKeyboard.ResizeKeyboard = true;
            teacherKeyboard.ResizeKeyboard = true;
            registrationKeyboard.ResizeKeyboard = true;
            registrationKeyboard.OneTimeKeyboard = true;
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


        static void Main(string[] args)
        {
            Json_Data.ReadData();
            KeyboardInit();
            TeachersInit();
            GradeInit();

            BOT = new Telegram.Bot.TelegramBotClient("697446498:AAFkXTktghiTFGCILZUZ9XiKHZN4LKohXiI");
            WriteLine("Подключен бот");
            BOT.OnMessage += BotOnMessageReceived;

            BOT.StartReceiving(new UpdateType[] { UpdateType.Message });
            WriteLine("Ожидает сообщений");
            ReadLine();
            BOT.StopReceiving();
        }

        static async void BotOnMessageReceived(object sender, MessageEventArgs MessageEventArgs)
        {
            Telegram.Bot.Types.Message msg = MessageEventArgs.Message;
            if (msg == null || msg.Type != MessageType.Text)
                return;

            string Answer = "Server Error";

            if (System.DateTime.UtcNow.Subtract(msg.Date).TotalMinutes > 3)
            {
                await BOT.SendTextMessageAsync(msg.Chat.Id, Answer);
                return;
            }

            if (!IsRegistered(msg.Chat.Id))
            {
                if (!UserList.ContainsKey(msg.Chat.Id))
                    UserList.Add(msg.Chat.Id, new User());
                Answer = Registration(msg);      //регистрация студента
            }//prev command was /findteacher
			else if (UserList[msg.Chat.Id].ident == 4)
			{
				if (!NameMatches.ContainsKey(msg.Chat.Id))
				{
					var lst = ReturnTeachersId(msg.Text);
					if (lst.Length == 1)
					{
						Answer = LessonTechToStr(CurrentSubject.GetCurrentLessonforTeacher(lst[0].id));
						UserList[msg.Chat.Id].ident = 3;
					}
					else if (lst.Length > 1)
					{
						//TODO: It would be more efficient to use StringBuilder here
						NameMatches.Add(msg.Chat.Id, lst);
						var s = $"Найдено несколько совпадений:\n";
						for (var i = 0; i < lst.Length; i++)
							s = s + $"{i + 1}) {lst[i].name}\n";
						s = s + "Ввведи номер выбранного преподавателя.";
						Answer = s;
					}
					else
						Answer = "Ошибка, преподаватель не найден! Попробуй ещё раз.";
				}
				else
				{
					if (int.TryParse(msg.Text, out int n) && n - 1 < NameMatches[msg.Chat.Id].Length && n - 1 >= 0)
					{
						Answer = LessonTechToStr(CurrentSubject.GetCurrentLessonforTeacher(NameMatches[msg.Chat.Id][n - 1].id));
						UserList[msg.Chat.Id].ident = 3;
						NameMatches.Remove(msg.Chat.Id);
					}
					else
					{
						Answer = "Ошибка, введён некорректный номер.";
					}
				}
			}
            else
            {
                switch (msg.Text.ToLower())             // Обработка команд боту
                {
					case "/next":
					case "ближайшая пара":
                        if (UserList[msg.Chat.Id].Info != User.UserInfo.teacher)
                            Answer = LessonToStr(CurrentSubject.GetCurrentLesson(UserList[msg.Chat.Id].groupid));
                        else
                            Answer = LessonTechToStr(CurrentSubject.GetCurrentLessonforTeacher(UserList[msg.Chat.Id].teacherId));
                        break;
					case "/findteacher":
					case "найти преподавателя":
						Answer = "Введи фамилию преподавателя";
						UserList[msg.Chat.Id].ident = 4;
						break;
					case "/week":
					case "расписание на неделю":
						if (UserList[msg.Chat.Id].Info != User.UserInfo.teacher)
							Answer = WeekSchToStr(CurrentSubject.GetWeekSchedule(UserList[msg.Chat.Id].groupid));
						else
							Answer = WeekSchTechToStr(CurrentSubject.GetWeekScheduleforTeacher(UserList[msg.Chat.Id].teacherId));
							break;
					case "/today":
					case "расписание на сегодня":
						if (UserList[msg.Chat.Id].Info != User.UserInfo.teacher)
							Answer = DaySchToStr(CurrentSubject.GetDaySchedule(UserList[msg.Chat.Id].groupid,GetCurDayOfWeek()));
						else
							Answer = DaySchTechToStr(CurrentSubject.GetDayScheduleforTeacher(UserList[msg.Chat.Id].teacherId, GetCurDayOfWeek()));
						break;
					case "/tomorrow":
					case "расписание на завтра":
						if (UserList[msg.Chat.Id].Info != User.UserInfo.teacher)
							Answer = DaySchToStr(CurrentSubject.GetDaySchedule(UserList[msg.Chat.Id].groupid, GetNextDayOfWeek()));
						else
							Answer = DaySchTechToStr(CurrentSubject.GetDayScheduleforTeacher(UserList[msg.Chat.Id].teacherId, GetNextDayOfWeek()));
						break;
					case "/knowme":
                    case "знаешь меня?":

                        if (UserList[msg.Chat.Id].Info == User.UserInfo.teacher)
                            Answer = $"Да, вы {TeacherList[UserList[msg.Chat.Id].teacherId].name}, Id = {TeacherList[UserList[msg.Chat.Id].teacherId].id}";     //TODO Убрать вывод ID; База старая, так что выводим только ФИО!!!
                        else
                            Answer = "Да, ты " + UserList[msg.Chat.Id].id.ToString();
                        break;


                    case "/forget":
                    case "забудь меня":
                        UserList[msg.Chat.Id].ident = 0;
                        Json_Data.WriteData();
                        Answer = "Я тебя забыл! Для повторной регистрации пиши /start";
                        break;


                    case "помощь":
                    case "/help":
                        Answer = @"Список команд: 
/next - какая ближайшая пара
/today - расписание на сегодня
/tomorrow - список пар на завтра
/week - расписание на неделю
/findteacher - поиск преподавателя
/info - краткое описание бота    
/knowme - показать ваш id
/forget - сменить пользователя
/help - список команд";
                        break;
					case "/info":
					case "информация":
						//TODO: Write sth about creators XD
						Answer = "Меня создали Миша, Дима, Дима, Глеб, Никита, Ира, Максим.";
						break;

                    default:
                        Answer = "Введены неверные данные, повторите попытку.";
                        break;
                }
            }
            if (IsRegistered(msg.Chat.Id))
                await BOT.SendTextMessageAsync(msg.Chat.Id, Answer, ParseMode.Markdown , replyMarkup: UserList[msg.Chat.Id].Info == User.UserInfo.teacher ? teacherKeyboard : studentKeyboard);
            else if (UserList[msg.Chat.Id].ident == 1)
                await BOT.SendTextMessageAsync(msg.Chat.Id, Answer, replyMarkup: registrationKeyboard);
            else
                await BOT.SendTextMessageAsync(msg.Chat.Id, Answer);

        }

        /// <summary>
        /// Checks if entered course and group exist
        /// </summary>
        /// <param name="s">C.G</param>
        /// <returns></returns>
        static bool IsCourseGroup(long id, string s)
        {
            try
            {
                var lst = s.Split('.').ToArray();
                if (lst[0] == String.Empty || lst[1] == String.Empty || lst.Length > 2 || lst.Length < 1)
                {
                    WriteLine("Ошибка ввода!");
                    return false;
                }
                var (course, group) = (-1, -1);
                bool IsCourse = int.TryParse(lst[0], out course);
                bool IsGroup = int.TryParse(lst[1], out group);
                if (!IsCourse || !IsGroup)
                {
                    WriteLine("Ошибка парсинга!");
                    return false;
                }
                int groupid = 0;
                switch (UserList[id].Info)
                {
                    case User.UserInfo.bachelor:
                        groupid = GradeList.Where(y => y.degree == "bachelor" && y.num == course).First().Groups.Where(y => y.num == group).First().id;
                        break;
                    case User.UserInfo.graduate:
                        groupid = GradeList.Where(y => y.degree == "postgraduate" && y.num == course).First().Groups.Where(y => y.num == group).First().id;
                        break;
                    case User.UserInfo.master:
                        groupid = GradeList.Where(y => y.degree == "master" && y.num == course).First().Groups.Where(y => y.num == group).First().id;
                        break;
                }
                UserList[id].groupid = groupid;
                return true;
            }
            catch (Exception e)
            {
                WriteLine("Ошибка: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Returns matches from TeacherList
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static Teacher[] ReturnTeachersId(string s)
        {
            s = s.ToLower();
            var lst = new List<Teacher>();
            foreach (var x in TeacherList)
                if (s.Length>3 && x.Value.name.ToLower().StartsWith(s))
                    lst.Add(x.Value);
            return lst.ToArray();
        }


		/// <summary>
		/// Registration of new user in bot's DB
		/// </summary>
		/// <param name="msg"></param>
		/// <returns></returns>
		static string Registration(Telegram.Bot.Types.Message msg)
		{
			//TODO: Get rid of all String.Empty and "" because they can lead to exeption
			string Answer = "Введены неверные данные, повторите попытку.";

            msg.Text = msg.Text.ToLower();

            switch (msg.Text)
			{
				case "/start":
					if (UserList[msg.Chat.Id].ident == 0)
					{
						UserList[msg.Chat.Id].id = msg.Chat.Id;      //Запись айди

						WriteLine("Записал ID: " + msg.Chat.Id.ToString());

                        Answer = "Вы бакалавр, магистр, аспирант или преподаватель?";
						UserList[msg.Chat.Id].ident++;
					}
                    else
                        Answer = "Введены неверные данные, повторите попытку.";
                    break;
				case "аспирант":
					if (UserList[msg.Chat.Id].ident == 1)
					{
						UserList[msg.Chat.Id].Info = User.UserInfo.graduate; //Запись данных
						Answer = "Напиши номер курса и группы через точку. (x.x)";

						WriteLine("Записал тип пользователя");

						UserList[msg.Chat.Id].ident++;
					}
                    else
                        Answer = "Введены неверные данные, повторите попытку.";
                    break;
				case "бакалавр":
					if (UserList[msg.Chat.Id].ident == 1)
					{
						UserList[msg.Chat.Id].Info = User.UserInfo.bachelor;  //Запись данных
						Answer = "Напиши номер курса и группы через точку. (x.x)";

						WriteLine("Записал тип пользователя");

						UserList[msg.Chat.Id].ident++;
					}
                    else
                        Answer = "Введены неверные данные, повторите попытку.";
                    break;
				case "магистр":
					if (UserList[msg.Chat.Id].ident == 1)
					{

						UserList[msg.Chat.Id].Info = User.UserInfo.master;  //Запись данных
						Answer = "Напиши номер курса и группы через точку. (x.x)";

						WriteLine("Записал тип пользователя");

						UserList[msg.Chat.Id].ident++;
					}
                    else
                        Answer = "Введены неверные данные, повторите попытку.";
                    break;
				case "преподаватель":
					if (UserList[msg.Chat.Id].ident == 1)
					{
						UserList[msg.Chat.Id].Info = User.UserInfo.teacher;  //Запись данных
						Answer = "Введите вашу фамилию.";

						WriteLine("Записал тип пользователя");

						UserList[msg.Chat.Id].ident++;
					}
                    else
                        Answer = "Введены неверные данные, повторите попытку.";
                    break;
                case "/forget":
                case "забудь меня":
                    UserList[msg.Chat.Id].ident = 0;
                    Answer = "Я тебя забыл! Для повторной регистрации пиши /start";
                    break;
                default:
					if (UserList[msg.Chat.Id].ident == 2 && UserList[msg.Chat.Id].Info == User.UserInfo.teacher)
					{
                        if (!NameMatches.ContainsKey(msg.Chat.Id))
                        {
                            var lst = ReturnTeachersId(msg.Text);
                            if (lst.Length == 1)
                            {
                                UserList[msg.Chat.Id].teacherId = lst[0].id;
                                WriteLine("Преподаватель зареган");
                                Answer = "Вы получили доступ к функционалу.";
                                UserList[msg.Chat.Id].ident++;

                                Json_Data.WriteData();
                            }
                            else if (lst.Length > 1)
                            {
                                NameMatches.Add(msg.Chat.Id, lst);
                                var s = $"Найдено несколько совпадений:\n";
                                for (var i = 0; i < lst.Length; i++)
                                    s = s + $"{i + 1}) {lst[i].name}\n";
                                s = s + "Ввведите номер вашего ФИО.";
                                Answer = s;
                            }
                            else
                                Answer = "Ошибка, преподаватель не найден! Попробуйте ещё раз.";
                        }
                        else
                        {
                            if (int.TryParse(msg.Text, out int n) && n-1< NameMatches[msg.Chat.Id].Length && n-1>=0)
                            {
                                UserList[msg.Chat.Id].teacherId = NameMatches[msg.Chat.Id][n-1].id;
                                WriteLine("Преподаватель зареган");
                                Answer = "Вы получили доступ к функционалу.";
                                UserList[msg.Chat.Id].ident++;
                                NameMatches.Remove(msg.Chat.Id);

                                Json_Data.WriteData();
                            }
                            else
                            {
                                Answer = "Ошибка, введён некорректный номер.";
                            }
                        }
					}
					else
					{
						if (UserList[msg.Chat.Id].ident == 2 && IsCourseGroup(msg.Chat.Id,msg.Text))               //проверка введённого номера курса\группы
						{
							UserList[msg.Chat.Id].ident++;
							Answer = "Вы получили доступ к функционалу.";
							Json_Data.WriteData();

							WriteLine("Регистрация завершена!");
						}
						else
						{
							if (UserList[msg.Chat.Id].ident == 0)
								Answer = "Для начала работы с ботом пиши /start";
							else
								Answer = "Введены неверные данные, повторите попытку.";
						}
					}
					break;

			}

			return Answer;
		}

        

        /// <summary>
        /// Convert a tuple representing Lesson at time-slot and it's descr. to string to string
        /// </summary>
        /// <param name="LC"></param>
        /// <returns></returns>
        static string LessonToStr((Lesson, List<Curriculum>)LC)
		{
            string res = string.Empty;
            if (LC.Item2.Count > 0)
                res = LC.Item1.timeslot + "\n" + string.Join('\n', LC.Item2);
            else
                res = "Нет информации о парах для твоей группы.";
            return res;
			//TODO: In case of equal subjucts print subject name only once, then all teachers and rooms
			//TODO: parse timeslot to TimeOfLesson
		}

		/// <summary>
		///  Convert a tuple representing Lesson at time-slot and it's descr. to string
		/// </summary>
		/// <param name="LCG"></param>
		/// <returns></returns>
		static string LessonTechToStr((Lesson, List<Curriculum>, List<TechGroup>) LCG)
        {
            string res = string.Empty;
            if (LCG.Item3.Count > 0)
                res = LCG.Item1.timeslot + "\n" + string.Join('\n', LCG.Item2.Select(c => c.subjectname+", ауд."+c.roomname)) +"\n"+ string.Join("; ", LCG.Item3.Select(g => g.gradenum+"."+g.groupnum));
            else
                res = "Нет информации о парах для вас.";
            return res;
            //TODO: parse timeslot to TimeOfLesson
        }

		/// <summary>
		/// Enum for days of week
		/// </summary>
		public enum DayOfWeek { Понедельник = 0, Вторник, Среда, Четверг, Пятница, Суббота, Воскресенье };

		/// <summary>
		/// Convert a list of tuples representing week schedule to string
		/// </summary>
		/// <param name="ws">Week schedule</param>
		/// <returns></returns>
		static string WeekSchToStr (List<(Lesson, List<Curriculum>)> ws)
		{
			//TODO: StringBuilder
			string res = String.Empty;
			if (ws.Count > 0)
			{
				for (var i = 0; i < 7; i++)
				{
					var daysch = ws.FindAll(LLC => TimeOfLesson.Parse(LLC.Item1.timeslot).day == i);
					if (daysch.Count > 0)
					{
						res += "*"+((DayOfWeek)i).ToString()+"*" + ":\n";
						foreach (var l in daysch)
							res += LessonToStr(l) + "\n";
					}
					res += "\n";
				}
			}
			else
				res = "Расписание недоступно.";
			return res;
		}
		/// <summary>
		/// Convert a list of tuples representing day schedule to string
		/// </summary>
		/// <param name="ds">Day schedule</param>
		/// <returns></returns>
		static string DaySchToStr(List<(Lesson, List<Curriculum>)> ds)
		{
			string res = String.Empty;
			if (ds.Count > 0)
			{
				foreach (var l in ds)
					res += LessonToStr(l) + "\n";
			}
			else
				res = "В этот день нет пар.";
			return res;
		}
		/// <summary>
		/// Convert a list of tuples representing week schedule for teacher to string
		/// </summary>
		/// <param name="ws">Week schedule</param>
		/// <returns></returns>
		static string WeekSchTechToStr(List<(Lesson, List<Curriculum>, List<TechGroup>)> ws)
		{
			//TODO: StringBuilder
			string res = String.Empty;
			if (ws.Count > 0)
			{
				for (var i = 0; i < 7; i++)
				{
					var daysch = ws.FindAll(LLC => TimeOfLesson.Parse(LLC.Item1.timeslot).day == i);
					if (daysch.Count > 0)
					{
						res += "*" + ((DayOfWeek)i).ToString() + "*" + ":\n";
						foreach (var l in daysch)
							res += LessonTechToStr(l) + "\n";
					}
					res += "\n";
				}
			}
			else 
				res = "Расписание недоступно.";
			return res;
		}
		/// <summary>
		/// Convert a list of tuples representing day schedule for teacher to string
		/// </summary>
		/// <param name="ds">Day schedule</param>
		/// <returns></returns>
		static string DaySchTechToStr(List<(Lesson, List<Curriculum>, List<TechGroup>)> ds)
		{
			string res = String.Empty;
			if (ds.Count > 0)
			{
				foreach (var l in ds)
					res += LessonTechToStr(l) + "\n";
			}
			else
				res = "В этот день нет пар.";
			return res;
		}
		static int GetCurDayOfWeek() => (((int)System.DateTime.Now.DayOfWeek) + 6) % 7;
		static int GetNextDayOfWeek() => (((int)System.DateTime.Now.DayOfWeek) + 7) % 7;
	}
}
