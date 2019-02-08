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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
		static Dictionary<long, User> UserList = new Dictionary<long, User>();

		//Что за колхоз в ПИ программы? TODO: Make local var at suitable place
		static string serialized;

        /// <summary>
		/// Keyboard for registered users
		/// </summary>
        static ReplyKeyboardMarkup defaultKeyboard = new ReplyKeyboardMarkup(new[] {
							new[]{ new KeyboardButton("Ближайшая пара"),new KeyboardButton("Расписание на сегодня") },      //Кастомная клава
                            new[]{ new KeyboardButton("Расписание на неделю"),new KeyboardButton("Помощь") }
							}
						);

		//TODO: Вынести эту классную история в отдельный файл, где будет всё связанной с внутренней БД
		class Json_Data
		{
			public User[] User { get; set; }
		}

		static void Main(string[] args)
		{
			ReadData();

			BOT = new Telegram.Bot.TelegramBotClient("697446498:AAFkXTktghiTFGCILZUZ9XiKHZN4LKohXiI");
			WriteLine("Подключен бот");
			BOT.OnMessage += BotOnMessageReceived;

			BOT.StartReceiving(new UpdateType[] { UpdateType.Message });
			//BOT.StartReceiving();
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
						Answer = LessonLstCurToAswr(CurrentSubject.GetCurrentLesson(CurrentSubject.CourseGroupToID(UserList[msg.Chat.Id].course, UserList[msg.Chat.Id].group)));
						break;
					case "/knowme":
					case "знаешь меня?":
						Answer = "Да, ты " + UserList[msg.Chat.Id].id.ToString();
						break;


					case "/forget":
					case "забудь меня":
						UserList[msg.Chat.Id].ident = 0;
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
				await BOT.SendTextMessageAsync(msg.Chat.Id, Answer, replyMarkup: defaultKeyboard);
			else
				await BOT.SendTextMessageAsync(msg.Chat.Id, Answer);
		}

		/// <summary>
		/// Checks if entered course and group exist
		/// </summary>
		/// <param name="s">C.G</param>
		/// <returns></returns>
		static bool IsCourseGroup(string s)
		{
			int course;
			bool isCourse = int.TryParse(s[0].ToString(), out course);
			int group;
			bool isGroup = int.TryParse(s.Remove(0, 2), out group);


			if (isCourse && isGroup && s[1] == '.' && course <= 4)        //TODO: Проверка существования группы и курса
				return true;
			else
				return false;
		}

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
						// UserList[msg.Chat.Id].userInfo = msg.Text;  
						UserList[msg.Chat.Id].Info = User.UserInfo.graduate; //Запись данных
						Answer = "Напиши номер курса и группы через точку. (x.x)";

						WriteLine("Записал тип пользователя");

						UserList[msg.Chat.Id].ident++;
					}
					break;
				case "бакалавр":
					if (UserList[msg.Chat.Id].ident == 1)
					{
						UserList[msg.Chat.Id].Info = User.UserInfo.bachelor;  //Запись данных
						Answer = "Напиши номер курса и группы через точку. (x.x)";

						WriteLine("Записал тип пользователя");

						UserList[msg.Chat.Id].ident++;
					}
					break;
				case "магистр":
					if (UserList[msg.Chat.Id].ident == 1)
					{

						UserList[msg.Chat.Id].Info = User.UserInfo.master;  //Запись данных
						Answer = "Напиши номер курса и группы через точку. (x.x)";

						WriteLine("Записал тип пользователя");

						UserList[msg.Chat.Id].ident++;
					}
					break;
				case "преподаватель":
					if (UserList[msg.Chat.Id].ident == 1)
					{
						UserList[msg.Chat.Id].Info = User.UserInfo.teacher;  //Запись данных
						Answer = "Напишите ваше ФИО.";

						WriteLine("Записал тип пользователя");

						UserList[msg.Chat.Id].ident++;
					}
					break;
				default:
					if (UserList[msg.Chat.Id].ident == 2 && UserList[msg.Chat.Id].Info == User.UserInfo.teacher)
					{
						UserList[msg.Chat.Id].FIO = msg.Text;

						WriteLine("Преподаватель зареган");

						Answer = "Вы получили доступ к функцианалу.";

						//База расписания потом не найдёт препода из-за lower-case
						UserList[msg.Chat.Id].ident++;

						WriteData();
					}
					else
					{
						if (UserList[msg.Chat.Id].ident == 2 && IsCourseGroup(msg.Text))               //проверка введённого номера курса\группы
						{
							UserList[msg.Chat.Id].course = int.Parse(msg.Text.Substring(0, 1));       //запись курса\группы
							UserList[msg.Chat.Id].group = int.Parse(msg.Text.Substring(2));

							//Info_label.Text = "Студент зареган";

							UserList[msg.Chat.Id].ident++;
							Answer = "Вы получили доступ к функцианалу.";
							WriteData();

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

		static void WriteData()
		{
			Json_Data myCollection = new Json_Data();
			myCollection.User = new User[UserList.Count];

			int i = 0;
			foreach (KeyValuePair<long, User> us in UserList)
			{
				myCollection.User[i] = new User()
				{
					ident = us.Value.ident,
					Info = us.Value.Info,
					FIO = us.Value.FIO,
					id = us.Value.id,
					course = us.Value.course,
					group = us.Value.group

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

		static void ReadData()
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
						FIO = json.User[i].FIO,
						Info = json.User[i].Info,
						course = json.User[i].course,
						group = json.User[i].group
					};
					UserList.Add(x.id, x);
				}
			}

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
