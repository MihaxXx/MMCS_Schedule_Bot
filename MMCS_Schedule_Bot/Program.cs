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
                            new[]{ new KeyboardButton("Ближайшая пара"),new KeyboardButton("Знаешь меня?") },      //Кастомная клава для препода
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

            String Answer = "";

			if (!IsRegistered(msg.Chat.Id))
			{
				if (!UserList.ContainsKey(msg.Chat.Id))
					UserList.Add(msg.Chat.Id, new User());
				Answer = Registration(msg);      //регистрация студента
			}
			else
			{
				switch (msg.Text.ToLower())             // Обработка команд боту
				{
					case "ближайшая пара":
					case "/next":
                        if (UserList[msg.Chat.Id].Info != User.UserInfo.teacher)
                            Answer = LessonLstCurToAswr(CurrentSubject.GetCurrentLesson(UserList[msg.Chat.Id].groupid));
                        else
                            Answer = "Эта команда работает пока только для студентов(";  //TODO Ближайшая пара препода
						break;
					case "/knowme":
					case "знаешь меня?":
                        if (UserList[msg.Chat.Id].Info == User.UserInfo.teacher)
                            Answer = "Да, вы " + TeacherList[UserList[msg.Chat.Id].teacherId].ToString();
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
/help - список команд
/info - краткое описание бота    
/knowme - показать ваш id
/next - какая ближайшая пара
/tomorrow - список пар на завтра
/forget - сменить пользователя";
						break;


					default:
						Answer = "Введены неверные данные, повторите попытку.";
						break;
				}
			}

			if (IsRegistered(msg.Chat.Id))
				await BOT.SendTextMessageAsync(msg.Chat.Id, Answer, replyMarkup: UserList[msg.Chat.Id].Info==User.UserInfo.teacher ? teacherKeyboard : studentKeyboard);
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
		static bool IsCourseGroup(long id,string s)
		{
            try
            {
                var lst = s.Split('.').ToArray();
                if (lst[0] == String.Empty || lst[1] == String.Empty || lst.Length>2 || lst.Length<1)
                {
                    WriteLine("Ошибка ввода!");
                    return false;
                }
                var (course, group) = (-1, -1);
                bool IsCourse = int.TryParse(lst[0],out course);
                bool IsGroup = int.TryParse(lst[1],out group);
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
                WriteLine("Ошибка: "+e.Message);
                return false;
            }
		}

        /// <summary>
        /// Returns Id of teacher or -1
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static int ReturnTeachersId(string s) => TeacherList.FirstOrDefault(t => t.Value.name.ToLower() == s.ToLower()).Key;

		/// <summary>
		/// Registration of new user in bot's DB
		/// </summary>
		/// <param name="msg"></param>
		/// <returns></returns>
		static string Registration(Telegram.Bot.Types.Message msg)
		{
			string Answer = "";
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
						Answer = "Напишите ваше ФИО.";

						WriteLine("Записал тип пользователя");

						UserList[msg.Chat.Id].ident++;
					}
                    else
                        Answer = "Введены неверные данные, повторите попытку.";
                    break;
				default:
					if (UserList[msg.Chat.Id].ident == 2 && UserList[msg.Chat.Id].Info == User.UserInfo.teacher)
					{
                        int index = ReturnTeachersId(msg.Text);
                        if (index != 0)
                        {
                            UserList[msg.Chat.Id].teacherId = index;
                            WriteLine("Преподаватель зареган");
                            Answer = "Вы получили доступ к функционалу.";
                            UserList[msg.Chat.Id].ident++;

                            Json_Data.WriteData();
                        }
                        else Answer = "Ошибка, преподаватель не найден! Попробуйте ещё раз.";
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
        /// Convert a tuple representing Lesson at time-slot and it's descr. to string for answer
        /// </summary>
        /// <param name="LC"></param>
        /// <returns></returns>
        static string LessonLstCurToAswr((Lesson, List<Curriculum>)LC)
		{
			return LC.Item1.ToString() + "\n" + string.Join('\n', LC.Item2);
		}
	}
}
