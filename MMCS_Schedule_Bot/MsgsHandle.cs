using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

using API;
using VkBotFramework.Models;
using VkNet.Model.RequestParams;
using VkNet.Model.Keyboard;

namespace ScheduleBot
{
    partial class Program
    {
        static IEnumerable<Elective> electives;
        static string electivesStr;


        static async void BotOnMessageReceived(object sender, MessageReceivedEventArgs eventArgs)
        {
            var msg = eventArgs.Message;
            if (msg == null || msg.Text == string.Empty)
                return;

            string Answer = "Server Error";

            if (System.DateTime.UtcNow.Subtract(msg.Date.Value).TotalMinutes > 3)
            {
                vkBot.Api.Messages.Send(new MessagesSendParams()
                {
                    RandomId = DateTime.Now.Ticks,
                    PeerId = msg.PeerId,
                    Message = Answer
                });
                return;
            }

            if (UserListVK.ContainsKey(msg.PeerId.Value))
                UserListVK[msg.PeerId.Value].LastAccess = DateTime.Now;

            if (!IsRegisteredVK(msg.PeerId.Value))
            {
                if (!UserListVK.ContainsKey(msg.PeerId.Value))
                    UserListVK.Add(msg.PeerId.Value, new User());
                Answer = Registration(msg);      //регистрация студента
            }//prev command was /findteacher
            else if (UserListVK[msg.PeerId.Value].ident == 4)
            {
                if (!NameMatchesVK.ContainsKey(msg.PeerId.Value))
                {
                    var lst = ReturnTeachersId(msg.Text);
                    if (lst.Length == 1)
                    {
                        Answer = LessonTechToStr(TeacherMethods.GetCurrentLesson(lst[0].id), true);
                        UserListVK[msg.PeerId.Value].ident = 3;
                    }
                    else if (lst.Length > 1)
                    {
                        NameMatchesVK.Add(msg.PeerId.Value, lst);
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
                    if (int.TryParse(msg.Text, out int n) && n - 1 < NameMatchesVK[msg.PeerId.Value].Length && n - 1 >= 0)
                    {
                        var LCG = TeacherMethods.GetCurrentLesson(NameMatchesVK[msg.PeerId.Value][n - 1].id);
                        Answer = LessonTechToStr(LCG, true);
                        UserListVK[msg.PeerId.Value].ident = 3;
                        NameMatchesVK.Remove(msg.PeerId.Value);
                    }
                    else
                    {
                        Answer = "Ошибка, введён некорректный номер.";
                    }
                }
            }
            else if (UserListVK[msg.PeerId.Value].ident == 5)
            {
                bool onOrOff = msg.Text.ToLower() == "включить";
                UserListVK[msg.PeerId.Value].eveningNotify = onOrOff;
                UserListVK[msg.PeerId.Value].ident = 3;
                JsonData.WriteData();
                string onOrOffMsg = onOrOff ? "включено" : "выключено";
                Answer = $"Вечернее уведомление *{onOrOffMsg}*.";
            }
            else if (UserListVK[msg.PeerId.Value].ident == 6)
            {
                bool onOrOff = msg.Text.ToLower() == "включить";
                UserListVK[msg.PeerId.Value].preLessonNotify = onOrOff;
                UserListVK[msg.PeerId.Value].ident = 3;
                JsonData.WriteData();
                string onOrOffMsg = onOrOff ? "включено" : "выключено";
                Answer = $"Уведомление за 15 минут до первой пары *{onOrOffMsg}*.";
            }
            else
            {
                try
                {
                    switch (msg.Text.ToLower())             // Обработка команд боту
                    {
                        case "/next":
                        case "ближайшая пара":
                            if (UserListVK[msg.PeerId.Value].Info != User.UserInfo.teacher)
                                Answer = LessonToStrVK(StudentMethods.GetCurrentLesson(UserListVK[msg.PeerId.Value].groupid), true);
                            else
                                Answer = LessonTechToStrVK(TeacherMethods.GetCurrentLesson(UserListVK[msg.PeerId.Value].teacherId), true);
                            break;
                        case "/findteacher":
                        case "найти преподавателя":
                            Answer = "Введи фамилию преподавателя";
                            UserListVK[msg.PeerId.Value].ident = 4;
                            break;
                        case "/week":
                        case "расписание на неделю":
                            if (UserListVK[msg.PeerId.Value].Info != User.UserInfo.teacher)
                                Answer = WeekSchToStrVK(StudentMethods.GetWeekSchedule(UserListVK[msg.PeerId.Value].groupid));
                            else
                                Answer = WeekSchTechToStrVK(TeacherMethods.GetWeekSchedule(UserListVK[msg.PeerId.Value].teacherId));
                            break;
                        case "/today":
                        case "расписание на сегодня":
                            if (UserListVK[msg.PeerId.Value].Info != User.UserInfo.teacher)
                                Answer = DaySchToStrVK(StudentMethods.GetTodaySchedule(UserListVK[msg.PeerId.Value].groupid));
                            else
                                Answer = DaySchTechToStrVK(TeacherMethods.GetTodaySchedule(UserListVK[msg.PeerId.Value].teacherId));
                            break;
                        case "/tomorrow":
                        case "расписание на завтра":
                            if (UserListVK[msg.PeerId.Value].Info != User.UserInfo.teacher)
                                Answer = DaySchToStrVK(StudentMethods.GetTomorrowSchedule(UserListVK[msg.PeerId.Value].groupid));
                            else
                                Answer = DaySchTechToStrVK(TeacherMethods.GetTomorrowSchedule(UserListVK[msg.PeerId.Value].teacherId));
                            break;
                        case "/knowme":
                        case "знаешь меня?":
                            if (UserListVK[msg.PeerId.Value].Info == User.UserInfo.teacher)
                                Answer = $"Вы {TeacherList[UserListVK[msg.PeerId.Value].teacherId].name}";     //База старая, так что выводим только ФИО!!!
                            else
                                Answer = $"Вы {vkBot.Api.Users.Get(new List<long> {msg.FromId.Value}).First().FirstName.Replace("`", "").Replace("_", "").Replace("*", "")} из группы {StudentMethods.groupIDToCourseGroup(UserListVK[msg.PeerId.Value].groupid)}";
                            break;
                        /*
                    case "/eveningnotify":
                        Answer = $"Сейчас вечернее уведомление о завтрашней первой паре *{(UserListVK[msg.PeerId.Value].eveningNotify ? "включено" : "выключено")}*. \nНастройте его.";
                        UserListVK[msg.PeerId.Value].ident = 5;
                        vkBot.Api.Messages.Send(new MessagesSendParams()
                        {
                            RandomId = DateTime.Now.Ticks,
                            PeerId = msg.PeerId,
                            Message = Answer,
                            Keyboard = notifierKeyboardVk
                        });
                        return;

                    case "/prelessonnotify":
                        Answer = $"Сейчас уведомление за 15 минут до первой пары *{(UserListVK[msg.PeerId.Value].preLessonNotify ? "включено" : "выключено")}*. \nНастройте его.";
                        UserListVK[msg.PeerId.Value].ident = 6;
                        vkBot.Api.Messages.Send(new MessagesSendParams()
                        {
                            RandomId = DateTime.Now.Ticks,
                            PeerId = msg.PeerId,
                            Message = Answer,
                            Keyboard = notifierKeyboardVk
                        });
                        return;
                        */

                        case "/stop":
                        case "/forget":
                        case "забудь меня":
                            UserListVK.Remove(msg.PeerId.Value);
                            JsonData.WriteData();
                            Answer = "Я вас забыл! Для повторной регистрации пиши /start";
                            vkBot.Api.Messages.Send(new MessagesSendParams()
                            {
                                RandomId = DateTime.Now.Ticks,
                                PeerId = msg.PeerId,
                                Message = Answer
                            });
                            return;

                        case "помощь":
                        case "/help":
                            Answer = _help;
                            break;
                        case "/info":
                        case "информация":
                            Answer = "Меня создали Миша, Дима, Дима, Глеб, Никита, Ира, Максим в рамках проектной деятельности на ФИиИТ в 2018-2019 уч. году.\n" +
                                "Я предоставляю доступ к интерактивному расписанию мехмата через платформы ботов Telegram и VK.\n" +
                                "Если обнаружили ошибку в расписании, напишите [id67145152|Михаилу].";
                            break;
                        case "/optionalcourses":
                        case "факультативы":
                            Answer = electivesStr;
                            break;
                        case "/curweek":
                            Answer = $"Сейчас *{CurrentSubject.GetCurrentWeek().ToString()}* неделя.";
                            break;
                        case "/forceupdate":
                            logger.Info($"Запрошено принудительное обновление расписаний, VK ID: {msg.PeerId.Value}.");
                            TeachersInit(false);
                            GradeInit(false);
                            GroupShedListInit(false);
                            TeachersShedInit(false);
                            WeekInit(false);
                            logger.Info($"Завершено принудительное обновление расписаний, VK ID: {msg.PeerId.Value}.");
                            Answer = "Данные расписаний обновлены!";
                            break;
                        default:
                            Answer = "Введены неверные данные, повторите попытку.";
                            break;
                    }
                }
                catch (System.Net.WebException e)
                {
                    logger.Error(e, "Catched exeption:");
                    Answer = "Ошибка! Вероятно, сервер интерактивного расписания недоступен. Пожалуйста, попробуйте повторить запрос позднее.";
                }
            }
            try
            {
                if (IsRegisteredVK(msg.PeerId.Value))
                    vkBot.Api.Messages.Send(new MessagesSendParams()
                    {
                        RandomId = DateTime.Now.Ticks,
                        PeerId = msg.PeerId,
                        Message = Answer,
                        Keyboard = UserListVK[msg.PeerId.Value].Info == User.UserInfo.teacher ? teacherKeyboardVk : studentKeyboardVk
                    });
                else if (UserListVK[msg.PeerId.Value].ident == 1)
                    vkBot.Api.Messages.Send(new MessagesSendParams()
                    {
                        RandomId = DateTime.Now.Ticks,
                        PeerId = msg.PeerId,
                        Message = Answer,
                        Keyboard = registrationKeyboardVk
                    });
                else
                    vkBot.Api.Messages.Send(new MessagesSendParams()
                    {
                        RandomId = DateTime.Now.Ticks,
                        PeerId = msg.PeerId,
                        Message = Answer
                    });
            }
            catch (Exception ex) when (ex is System.Net.Http.HttpRequestException && ex.Message.Contains("429"))
            {
                logger.Warn(ex, $"Сетевая ошибка при ответе в чат id: {msg.PeerId.Value}");
            }
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

            if (UserList.ContainsKey(msg.Chat.Id))
                UserList[msg.Chat.Id].LastAccess = DateTime.Now;

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
                        Answer = LessonTechToStr(TeacherMethods.GetCurrentLesson(lst[0].id), true);
                        UserList[msg.Chat.Id].ident = 3;
                    }
                    else if (lst.Length > 1)
                    {
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
                        var LCG = TeacherMethods.GetCurrentLesson(NameMatches[msg.Chat.Id][n - 1].id);
                        Answer = LessonTechToStr(LCG, true);
                        UserList[msg.Chat.Id].ident = 3;
                        NameMatches.Remove(msg.Chat.Id);
                    }
                    else
                    {
                        Answer = "Ошибка, введён некорректный номер.";
                    }
                }
            }
            else if (UserList[msg.Chat.Id].ident == 5)
            {
                bool onOrOff = msg.Text.ToLower() == "включить";
                UserList[msg.Chat.Id].eveningNotify = onOrOff;
                UserList[msg.Chat.Id].ident = 3;
                JsonData.WriteData();
                string onOrOffMsg = onOrOff ? "включено" : "выключено";
                Answer = $"Вечернее уведомление *{onOrOffMsg}*.";
            }
            else if (UserList[msg.Chat.Id].ident == 6)
            {
                bool onOrOff = msg.Text.ToLower() == "включить";
                UserList[msg.Chat.Id].preLessonNotify = onOrOff;
                UserList[msg.Chat.Id].ident = 3;
                JsonData.WriteData();
                string onOrOffMsg = onOrOff ? "включено" : "выключено";
                Answer = $"Уведомление за 15 минут до первой пары *{onOrOffMsg}*.";
            }
            else
            {
                try
                {
                    switch (msg.Text.ToLower())             // Обработка команд боту
                    {
                        case "/next":
                        case "ближайшая пара":
                            if (UserList[msg.Chat.Id].Info != User.UserInfo.teacher)
                                Answer = LessonToStr(StudentMethods.GetCurrentLesson(UserList[msg.Chat.Id].groupid), true);
                            else
                                Answer = LessonTechToStr(TeacherMethods.GetCurrentLesson(UserList[msg.Chat.Id].teacherId), true);
                            break;
                        case "/findteacher":
                        case "найти преподавателя":
                            Answer = "Введи фамилию преподавателя";
                            UserList[msg.Chat.Id].ident = 4;
                            break;
                        case "/week":
                        case "расписание на неделю":
                            if (UserList[msg.Chat.Id].Info != User.UserInfo.teacher)
                                Answer = WeekSchToStr(StudentMethods.GetWeekSchedule(UserList[msg.Chat.Id].groupid));
                            else
                                Answer = WeekSchTechToStr(TeacherMethods.GetWeekSchedule(UserList[msg.Chat.Id].teacherId));
                            break;
                        case "/today":
                        case "расписание на сегодня":
                            if (UserList[msg.Chat.Id].Info != User.UserInfo.teacher)
                                Answer = DaySchToStr(StudentMethods.GetTodaySchedule(UserList[msg.Chat.Id].groupid));
                            else
                                Answer = DaySchTechToStr(TeacherMethods.GetTodaySchedule(UserList[msg.Chat.Id].teacherId));
                            break;
                        case "/tomorrow":
                        case "расписание на завтра":
                            if (UserList[msg.Chat.Id].Info != User.UserInfo.teacher)
                                Answer = DaySchToStr(StudentMethods.GetTomorrowSchedule(UserList[msg.Chat.Id].groupid));
                            else
                                Answer = DaySchTechToStr(TeacherMethods.GetTomorrowSchedule(UserList[msg.Chat.Id].teacherId));
                            break;
                        case "/knowme":
                        case "знаешь меня?":
                            if (UserList[msg.Chat.Id].Info == User.UserInfo.teacher)
                                Answer = $"Вы {TeacherList[UserList[msg.Chat.Id].teacherId].name}";     //База старая, так что выводим только ФИО!!!
                            else
                                Answer = $"Вы {msg.Chat.FirstName.Replace("`","").Replace("_","").Replace("*","")} из группы {StudentMethods.groupIDToCourseGroup(UserList[msg.Chat.Id].groupid)}";
                            break;

                        case "/eveningnotify":
                            Answer = $"Сейчас вечернее уведомление о завтрашней первой паре *{(UserList[msg.Chat.Id].eveningNotify? "включено" : "выключено")}*. \nНастройте его.";
                            UserList[msg.Chat.Id].ident = 5;
                            await BOT.SendTextMessageAsync(msg.Chat.Id, Answer, ParseMode.Markdown, replyMarkup: notifierKeyboard);
                            return;

                        case "/prelessonnotify":
                            Answer = $"Сейчас уведомление за 15 минут до первой пары *{(UserList[msg.Chat.Id].preLessonNotify ? "включено" : "выключено")}*. \nНастройте его.";
                            UserList[msg.Chat.Id].ident = 6;
                            await BOT.SendTextMessageAsync(msg.Chat.Id, Answer, ParseMode.Markdown, replyMarkup: notifierKeyboard);
                            return;


                        case "/stop":
                        case "/forget":
                        case "забудь меня":
                            UserList.Remove(msg.Chat.Id);
                            JsonData.WriteData();
                            Answer = "Я вас забыл! Для повторной регистрации пиши /start";
                            await BOT.SendTextMessageAsync(msg.Chat.Id, Answer, replyMarkup: new ReplyKeyboardRemove());
                            return;

                        case "помощь":
                        case "/help":
                            Answer = _help;
                            break;
                        case "/info":
                        case "информация":
                            Answer = "Меня создали Миша, Дима, Дима, Глеб, Никита, Ира, Максим в рамках проектной деятельности на ФИиИТ в 2018-2019 уч. году.\n" +
                                "Я предоставляю доступ к интерактивному расписанию мехмата через платформы ботов Telegram и VK.\n" +
                                "Если обнаружили ошибку в расписании, напишите [Михаилу](tg://user?id=61026374).";
                            break;
                        case "/optionalcourses":
                        case "факультативы":
                            Answer = electivesStr;
                            break;
                        case "/curweek":
                            Answer = $"Сейчас *{CurrentSubject.GetCurrentWeek().ToString()}* неделя.";
                            break;
                        case "/forceupdate":
                            logger.Info($"Запрошено принудительное обновление расписаний, ID: {msg.Chat.Id}, @{msg.Chat.Username}.");
                            TeachersInit(false);
                            GradeInit(false);
                            GroupShedListInit(false);
                            TeachersShedInit(false);
                            WeekInit(false);
                            logger.Info($"Завершено принудительное обновление расписаний, ID: {msg.Chat.Id}, @{msg.Chat.Username}.");
                            Answer = "Данные расписаний обновлены!";
                            break;
                        default:
                            Answer = "Введены неверные данные, повторите попытку.";
                            break;
                    }
                }
                catch (System.Net.WebException e)
                {
                    logger.Error(e, "Catched exeption:");
                    Answer = "Ошибка! Вероятно, сервер интерактивного расписания недоступен. Пожалуйста, попробуйте повторить запрос позднее.";
                }
            }
            try
            {
                if (IsRegistered(msg.Chat.Id))
                    await BOT.SendTextMessageAsync(msg.Chat.Id, Answer, ParseMode.Markdown, replyMarkup: UserList[msg.Chat.Id].Info == User.UserInfo.teacher ? teacherKeyboard : studentKeyboard);
                else if (UserList[msg.Chat.Id].ident == 1)
                    await BOT.SendTextMessageAsync(msg.Chat.Id, Answer, replyMarkup: registrationKeyboard);
                else
                    await BOT.SendTextMessageAsync(msg.Chat.Id, Answer);
            }
            catch (Exception ex) when (ex is System.Net.Http.HttpRequestException && ex.Message.Contains("429"))
            {
                logger.Warn(ex, $"Сетевая ошибка при ответе @{msg.Chat.Username}");
            }
        }

        static MessageKeyboard studentKeyboardVk = new MessageKeyboard()
        {
            Buttons = (new[] {
                            new[]{ new MessageKeyboardButton() { Action = new MessageKeyboardButtonAction() { Label = "Ближайшая пара" } },new MessageKeyboardButton() { Action = new MessageKeyboardButtonAction() { Label = "Расписание на сегодня" } } },      //Кастомная клава для студентов
                            new[]{ new MessageKeyboardButton() { Action = new MessageKeyboardButtonAction() { Label = "Расписание на неделю" } },new MessageKeyboardButton() { Action = new MessageKeyboardButtonAction() { Label = "Помощь" } } }
                            }
                        )
        };

        static MessageKeyboard teacherKeyboardVk = new MessageKeyboard()
        {
            Buttons = (new[] {
                            new[]{ new MessageKeyboardButton() { Action = new MessageKeyboardButtonAction() { Label = "Ближайшая пара" } },new MessageKeyboardButton() { Action = new MessageKeyboardButtonAction() { Label = "Расписание на сегодня" } } },      //Кастомная клава для препода
                            new[]{ new MessageKeyboardButton() { Action = new MessageKeyboardButtonAction() { Label = "Расписание на неделю" } },new MessageKeyboardButton() { Action = new MessageKeyboardButtonAction() { Label = "Помощь" } } }
                            }
                        )
        };

        static MessageKeyboard registrationKeyboardVk = new MessageKeyboard()
        {
            Buttons = (new[] {
                            new[]{ new MessageKeyboardButton() { Action = new MessageKeyboardButtonAction() { Label = "Бакалавр" } },new MessageKeyboardButton() { Action = new MessageKeyboardButtonAction() { Label = "Магистр" } } },      //Кастомная клава для регистрации
                            new[]{ new MessageKeyboardButton() { Action = new MessageKeyboardButtonAction() { Label = "Аспирант" } },new MessageKeyboardButton() { Action = new MessageKeyboardButtonAction() { Label = "Преподаватель" } } }
                            }
                        ),
            OneTime = true
        };

        static MessageKeyboard notifierKeyboardVk = new MessageKeyboard()
        {
            Buttons = (new[] {
                            new[]{ new MessageKeyboardButton() { Action = new MessageKeyboardButtonAction() { Label = "Включить" } },new MessageKeyboardButton() { Action = new MessageKeyboardButtonAction() { Label = "Выключить" } } },      //Keyboard for notifier settings
                            }
                        ),
            OneTime = true
        };

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

        static ReplyKeyboardMarkup notifierKeyboard = new ReplyKeyboardMarkup(new[] {
                                    new[]{ new KeyboardButton("Включить"),new KeyboardButton("Выключить") },      //Keyboard for notifier settings
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
            notifierKeyboard.ResizeKeyboard = true;
        }

        /// <summary>
        /// Checks if user is already registered
        /// </summary>
        /// <param name="id">Telegram user ID</param>
        /// <returns></returns>
        static bool IsRegistered(long id) => UserList.ContainsKey(id) && UserList[id].ident > 2;

        /// <summary>
        /// Checks if user is already registered
        /// </summary>
        /// <param name="id">Telegram user ID</param>
        /// <returns></returns>
        static bool IsRegisteredVK(long id) => UserListVK.ContainsKey(id) && UserListVK[id].ident > 2;

        /// <summary>
        /// Checks if entered course and group exist
        /// </summary>
        /// <param name="s">C.G</param>
        /// <returns></returns>
        static bool IsCourseGroup(User user, string s)
        {
			var lst = s.Split('.');
			if (lst.Length != 2 || lst[0] == String.Empty || lst[1] == String.Empty)
			{
				logger.Info($"ID: {user.id}, IsCourseGroup(\"{s}\") - Ошибка ввода!");
				return false;
			}
			var (course, group) = (-1, -1);
			bool IsCourse = int.TryParse(lst[0], out course);
			bool IsGroup = int.TryParse(lst[1], out group);
			if (!IsCourse || !IsGroup)
			{
				logger.Info($"ID: {user.id}, IsCourseGroup(\"{s}\") - Ошибка парсинга!");
				return false;
			}
			try
			{
				int groupid = 0;
                switch (user.Info)
                {
                    case User.UserInfo.bachelor:
                        groupid = GradeList.Find(y => y.degree == "bachelor" && y.num == course).Groups.Find(y => y.num == group).id;
                        break;
                    case User.UserInfo.graduate:
                        groupid = GradeList.Find(y => y.degree == "postgraduate" && y.num == course).Groups.Find(y => y.num == group).id;
                        break;
                    case User.UserInfo.master:
                        groupid = GradeList.Find(y => y.degree == "master" && y.num == course).Groups.Find(y => y.num == group).id;
                        break;
                }
                user.groupid = groupid;
                return true;
            }
            catch (NullReferenceException e)
            {
                logger.Info(e, $"ID: {user.id}, IsCourseGroup(\"{s}\") - Исключение!");
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
                if (s.Length > 3 && x.Value.name.ToLower().StartsWith(s))
                    lst.Add(x.Value);
            return lst.ToArray();
        }
        /// <summary>
        /// Registration of new user in bot's DB
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        static string Registration(VkNet.Model.Message msg)
        {
            //TODO: Replace switch by msg to switch by user.ident
            string Answer = "Введены неверные данные, повторите попытку.";

            msg.Text = msg.Text.ToLower();

            switch (msg.Text)
            {
                case "/start":
                    if (UserListVK[msg.PeerId.Value].ident == 0)
                    {
                        UserListVK[msg.PeerId.Value].id = msg.PeerId.Value;      //Запись айди

                        logger.Info($"ID: {msg.PeerId.Value.ToString()}, регистрация: иницирована.");

                        Answer = "Вы бакалавр, магистр, аспирант или преподаватель?";
                        UserListVK[msg.PeerId.Value].ident++;
                    }
                    else
                        Answer = "Введены неверные данные, повторите попытку.";
                    break;
                case "аспирант":
                    if (UserListVK[msg.PeerId.Value].ident == 1)
                    {
                        UserListVK[msg.PeerId.Value].Info = User.UserInfo.graduate; //Запись данных
                        Answer = "Напиши номер курса и группы через точку. (x.x)";

                        logger.Info($"ID: {msg.PeerId.Value.ToString()}, регистрация: записан тип пользователя - {UserListVK[msg.PeerId.Value].Info.ToString()}.");

                        UserListVK[msg.PeerId.Value].ident++;
                    }
                    else
                        Answer = "Введены неверные данные, повторите попытку.";
                    break;
                case "бакалавр":
                    if (UserListVK[msg.PeerId.Value].ident == 1)
                    {
                        UserListVK[msg.PeerId.Value].Info = User.UserInfo.bachelor;  //Запись данных
                        Answer = "Напиши номер курса и группы через точку. (x.x)";

                        logger.Info($"ID: {msg.PeerId.Value.ToString()}, регистрация: записан тип пользователя - {UserListVK[msg.PeerId.Value].Info.ToString()}.");

                        UserListVK[msg.PeerId.Value].ident++;
                    }
                    else
                        Answer = "Введены неверные данные, повторите попытку.";
                    break;
                case "магистр":
                    if (UserListVK[msg.PeerId.Value].ident == 1)
                    {

                        UserListVK[msg.PeerId.Value].Info = User.UserInfo.master;  //Запись данных
                        Answer = "Напиши номер курса и группы через точку. (x.x)";

                        logger.Info($"ID: {msg.PeerId.Value.ToString()}, регистрация: записан тип пользователя - {UserListVK[msg.PeerId.Value].Info.ToString()}.");

                        UserListVK[msg.PeerId.Value].ident++;
                    }
                    else
                        Answer = "Введены неверные данные, повторите попытку.";
                    break;
                case "преподаватель":
                    if (UserListVK[msg.PeerId.Value].ident == 1)
                    {
                        UserListVK[msg.PeerId.Value].Info = User.UserInfo.teacher;  //Запись данных
                        Answer = "Введите вашу фамилию.";

                        logger.Info($"ID: {msg.PeerId.Value.ToString()}, регистрация: записан тип пользователя - {UserListVK[msg.PeerId.Value].Info.ToString()}.");

                        UserListVK[msg.PeerId.Value].ident++;
                    }
                    else
                        Answer = "Введены неверные данные, повторите попытку.";
                    break;
                case "/stop":
                case "/forget":
                case "забудь меня":
                    UserListVK[msg.PeerId.Value].ident = 0;
                    Answer = "Я вас забыл! Для повторной регистрации пиши /start";
                    break;
                default:
                    if (UserListVK[msg.PeerId.Value].ident == 2 && UserListVK[msg.PeerId.Value].Info == User.UserInfo.teacher)
                    {
                        if (!NameMatchesVK.ContainsKey(msg.PeerId.Value))
                        {
                            var lst = ReturnTeachersId(msg.Text);
                            if (lst.Length == 1)
                            {
                                UserListVK[msg.PeerId.Value].teacherId = lst[0].id;
                                logger.Info($"ID: {msg.PeerId.Value.ToString()}, регистрация: завершена - {UserListVK[msg.PeerId.Value].Info.ToString()} (teacherID {UserListVK[msg.PeerId.Value].teacherId}).");
                                Answer = "Вы получили доступ к функционалу.\n" + _help;
                                UserListVK[msg.PeerId.Value].ident++;

                                JsonData.WriteData();
                            }
                            else if (lst.Length > 1)
                            {
                                NameMatchesVK.Add(msg.PeerId.Value, lst);
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
                            if (int.TryParse(msg.Text, out int n) && n - 1 < NameMatchesVK[msg.PeerId.Value].Length && n - 1 >= 0)
                            {
                                UserListVK[msg.PeerId.Value].teacherId = NameMatchesVK[msg.PeerId.Value][n - 1].id;
                                logger.Info($"ID: {msg.PeerId.Value.ToString()}, регистрация: завершена - {UserListVK[msg.PeerId.Value].Info.ToString()} (teacherID {UserListVK[msg.PeerId.Value].teacherId}).");
                                Answer = "Вы получили доступ к функционалу.\n" + _help;
                                UserListVK[msg.PeerId.Value].ident++;
                                NameMatchesVK.Remove(msg.PeerId.Value);

                                JsonData.WriteData();
                            }
                            else
                            {
                                Answer = "Ошибка, введён некорректный номер.";
                            }
                        }
                    }
                    else
                    {
                        if (UserListVK[msg.PeerId.Value].ident == 2 && IsCourseGroup(UserListVK[msg.PeerId.Value], msg.Text))               //проверка введённого номера курса\группы
                        {
                            UserListVK[msg.PeerId.Value].ident++;
                            Answer = "Вы получили доступ к функционалу.\n" + _help;
                            JsonData.WriteData();

                            logger.Info($"ID: {msg.PeerId.Value.ToString()}, регистрация: завершена - {UserListVK[msg.PeerId.Value].Info.ToString()} (groupID {UserListVK[msg.PeerId.Value].groupid}).");
                        }
                        else
                        {
                            if (UserListVK[msg.PeerId.Value].ident == 0)
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
        /// Registration of new user in bot's DB
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        static string Registration(Telegram.Bot.Types.Message msg)
        {
            //TODO: Replace switch by msg to switch by user.ident
            string Answer = "Введены неверные данные, повторите попытку.";

            msg.Text = msg.Text.ToLower();

            switch (msg.Text)
            {
                case "/start":
                    if (UserList[msg.Chat.Id].ident == 0)
                    {
                        UserList[msg.Chat.Id].id = msg.Chat.Id;      //Запись айди

                        logger.Info($"ID: {msg.Chat.Id.ToString()}, регистрация: иницирована.");

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

                        logger.Info($"ID: {msg.Chat.Id.ToString()}, регистрация: записан тип пользователя - {UserList[msg.Chat.Id].Info.ToString()}.");

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

                        logger.Info($"ID: {msg.Chat.Id.ToString()}, регистрация: записан тип пользователя - {UserList[msg.Chat.Id].Info.ToString()}.");

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

                        logger.Info($"ID: {msg.Chat.Id.ToString()}, регистрация: записан тип пользователя - {UserList[msg.Chat.Id].Info.ToString()}.");

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

                        logger.Info($"ID: {msg.Chat.Id.ToString()}, регистрация: записан тип пользователя - {UserList[msg.Chat.Id].Info.ToString()}.");

                        UserList[msg.Chat.Id].ident++;
                    }
                    else
                        Answer = "Введены неверные данные, повторите попытку.";
                    break;
                case "/stop":
                case "/forget":
                case "забудь меня":
                    UserList[msg.Chat.Id].ident = 0;
                    Answer = "Я вас забыл! Для повторной регистрации пиши /start";
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
                                logger.Info($"ID: {msg.Chat.Id.ToString()}, регистрация: завершена - {UserList[msg.Chat.Id].Info.ToString()} (teacherID {UserList[msg.Chat.Id].teacherId}).");
                                Answer = "Вы получили доступ к функционалу.\n" + _help;
                                UserList[msg.Chat.Id].ident++;

                                JsonData.WriteData();
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
                            if (int.TryParse(msg.Text, out int n) && n - 1 < NameMatches[msg.Chat.Id].Length && n - 1 >= 0)
                            {
                                UserList[msg.Chat.Id].teacherId = NameMatches[msg.Chat.Id][n - 1].id;
                                logger.Info($"ID: {msg.Chat.Id.ToString()}, регистрация: завершена - {UserList[msg.Chat.Id].Info.ToString()} (teacherID {UserList[msg.Chat.Id].teacherId}).");
                                Answer = "Вы получили доступ к функционалу.\n" + _help;
                                UserList[msg.Chat.Id].ident++;
                                NameMatches.Remove(msg.Chat.Id);

                                JsonData.WriteData();
                            }
                            else
                            {
                                Answer = "Ошибка, введён некорректный номер.";
                            }
                        }
                    }
                    else
                    {
                        if (UserList[msg.Chat.Id].ident == 2 && IsCourseGroup(UserList[msg.Chat.Id], msg.Text))               //проверка введённого номера курса\группы
                        {
                            UserList[msg.Chat.Id].ident++;
                            Answer = "Вы получили доступ к функционалу.\n" + _help;
                            JsonData.WriteData();

                            logger.Info($"ID: {msg.Chat.Id.ToString()}, регистрация: завершена - {UserList[msg.Chat.Id].Info.ToString()} (groupID {UserList[msg.Chat.Id].groupid}).");
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
        /// Enum for days of week
        /// </summary>
        public enum DayOfWeek { Понедельник = 0, Вторник, Среда, Четверг, Пятница, Суббота, Воскресенье };
        public enum DayOfW { ПН = 0, ВТ, СР, ЧТ, ПТ, СБ, ВС };

        private static readonly string _help = @"Список команд: 
/next — какая ближайшая пара
/today — расписание на сегодня
/tomorrow — список пар на завтра
/week — расписание на неделю
/findteacher — поиск преподавателя
/info — краткое описание бота    
/knowme — информация о пользователе
/eveningNotify — настроить вечернее уведомление
/preLessonNotify — настроить уведомление за 15 минут до первой пары
/optionalcourses — информация о факультативах
/curweek — текущая неделя
/forget — сменить пользователя
/help — список команд";

        public static string StuDegreeShort(string degree)
        {
            switch (degree)
            {
                case "bachelor": return "бак.";
                case "master": return "маг.";
                case "specialist": return "спец.";
                case "postgraduate": return "асп.";
                default: return "н/д";
            }
        }


        /// <summary>
        /// Guesses the type of lesson (lecture or practice)
        /// </summary>
        /// <param name="LC"></param>
        /// <returns></returns>
        public static string LessonTypeVK((Lesson, List<Curriculum>) LC)
        {
            if (LC.Item2.Count == 1 && LC.Item2.First().roomname.Contains(',') ||
                LC.Item2.TrueForAll(c => c.subjectid == LC.Item2[0].subjectid) && !LC.Item2.TrueForAll(c => c.teacherid == LC.Item2[0].teacherid) ||
                LC.Item2.First().subjectname == "Иностранный язык")
                return " практ.";
            var groups = TeacherSchedule[LC.Item2.First().teacherid].Item1.Where(lcc => lcc.Item1.id == LC.Item1.id).First().Item3;
            if (LC.Item2.Count == 1 && groups.Count > 1)
                return " лекц.";
            else
            {
                var grade = GradeList.First(gr => gr.degree == groups.First().degree && gr.num == groups.First().gradenum).Groups;
                var groupName = grade.First(g => g.num == groups.First().groupnum).name;
                var Potoki = grade.Select(grs => grs.name).Distinct().Where(grName => grade.Select(grs => grs.name).Count(n => n == grName) > 1);
                if (Potoki.Contains(groupName))
                    return " практ.";
            }
            return "";
        }


            /// <summary>
            /// Convert a tuple representing Lesson at time-slot and it's descr. to string to string, short version w/o Markdown
            /// </summary>
            /// <param name="LC"></param>
            /// <returns></returns>
            public static string LessonToStrVK((Lesson, List<Curriculum>) LC, bool showDoW = false)
        {
            string res = string.Empty;
            if (LC.Item2.Count > 0)
            {
                var ts = TimeOfLesson.Parse(LC.Item1.timeslot);
                res = (showDoW ? (DayOfW)ts.day + " ": "") + $"{ts.starth}:{ts.startm.ToString("D2")}–{ts.finishh}:{ts.finishm.ToString("D2")}" + (ts.week != -1 ? (ts.week == 0 ? " в.н." : " н.н.") : "");
                //res = LC.Item1.timeslot + "\n";
                if (LC.Item2.Count > 1)
                {
                    if (LC.Item2.TrueForAll(c => c.subjectid == LC.Item2[0].subjectid))
                        if (LC.Item2.TrueForAll(c => c.teacherid == LC.Item2[0].teacherid))
                            res += " — " + LC.Item2[0].SubjNameShort() + LessonTypeVK(LC) + ", " + LC.Item2[0].TeacherLastname() + ", а." + String.Join("; ", LC.Item2.Select(c => c.roomname));
                        else
                            res += " — " + LC.Item2[0].SubjNameShort() + LessonTypeVK(LC) + ",\n" + String.Join("\n", LC.Item2.Select(c => "__" + c.TeacherLastname() + ", а." + c.roomname));
                    else
                        res += "\n" + String.Join('\n', LC.Item2.Select(c => $"__{c.SubjNameShort() + LessonTypeVK(LC)}, \n    преп. {c.TeacherLastname()}, а.{c.roomname}"));
                }
                else
                    res += " — " + String.Join('\n', LC.Item2.Select(c => $"{c.SubjNameShort() + LessonTypeVK(LC)}, {c.TeacherLastname()}, а.{c.roomname}"));
            }
            else
                res = "Нет информации о парах для вашей группы.";
            return res;
        }


        /// <summary>
        /// Guesses the type of lesson (lecture or practice)
        /// </summary>
        /// <param name="LC"></param>
        /// <returns></returns>
        public static string LessonType((Lesson, List<Curriculum>) LC)
        {
            if (LC.Item2.Count == 1 && LC.Item2.First().roomname.Contains(',') ||
                LC.Item2.TrueForAll(c => c.subjectid == LC.Item2[0].subjectid) && !LC.Item2.TrueForAll(c => c.teacherid == LC.Item2[0].teacherid) ||
                LC.Item2.First().subjectname== "Иностранный язык")
                return " *(практ.)*";
            var groups = TeacherSchedule[LC.Item2.First().teacherid].Item1.Where(lcc => lcc.Item1.id == LC.Item1.id).First().Item3;
            if (LC.Item2.Count == 1 && groups.Count > 1)
                return " *(лекц.)*";
            else
            {
                var grade = GradeList.First(gr => gr.degree == groups.First().degree && gr.num == groups.First().gradenum).Groups;
                var groupName = grade.First(g => g.num == groups.First().groupnum).name;
                var Potoki = grade.Select(grs => grs.name).Distinct().Where(grName => grade.Select(grs => grs.name).Count(n => n == grName) > 1);
                if (Potoki.Contains(groupName))
                    return " *(практ.)*";
            }
            return "";
        }

        /// <summary>
        /// Convert a tuple representing Lesson at time-slot and it's descr. to string to string
        /// </summary>
        /// <param name="LC"></param>
        /// <returns></returns>
        public static string LessonToStr((Lesson, List<Curriculum>) LC, bool showDoW = false)
        {
            string res = string.Empty;
            if (LC.Item2.Count > 0)
            {
                var ts = TimeOfLesson.Parse(LC.Item1.timeslot);
                res = (showDoW ? "*" + (DayOfW)ts.day + "* " : "") + $"*{ts.starth}:{ts.startm.ToString("D2")}–{ts.finishh}:{ts.finishm.ToString("D2")}*" + (ts.week != -1 ? (ts.week == 0 ? " в.н." : " н.н.") : "");
                //res = LC.Item1.timeslot + "\n";
                if (LC.Item2.Count > 1)
                {
                    //TODO: Use subjabbr if length of subjname is too long
                    if (LC.Item2.TrueForAll(c => c.subjectid == LC.Item2[0].subjectid))
                        if (LC.Item2.TrueForAll(c => c.teacherid == LC.Item2[0].teacherid))
                            res += " — " + LC.Item2[0].subjectname + LessonType(LC) + ",\n    преп. _" + LC.Item2[0].teachername + "_, ауд. " + String.Join("; ", LC.Item2.Select(c => "*" + c.roomname + "*"));
                        else
                            res += " — " + LC.Item2[0].subjectname + LessonType(LC) + ",\n" + String.Join("\n", LC.Item2.Select(c => "    преп. _" + c.teachername + "_, ауд. *" + c.roomname + "*"));
                    else
                        res += "\n" + String.Join('\n', LC.Item2.Select(c => $"    {c.subjectname + LessonType(LC)}, \n    преп. _{c.teachername}_, ауд. *{c.roomname}*"));
                }
                else
                    res += " — " + String.Join('\n', LC.Item2.Select(c => $"{c.subjectname + LessonType(LC)}, \n    преп. _{c.teachername}_, ауд. *{c.roomname}*"));
            }
            else
                res = "Нет информации о парах для вашей группы.";
            return res;
        }

        /// <summary>
        /// Convert a list of tuples representing day schedule to string, short version w/o Markdown
        /// </summary>
        /// <param name="ds">Day schedule</param>
        /// <returns></returns>
        static string DaySchToStrVK(List<(Lesson, List<Curriculum>)> ds)
        {
            string res = String.Empty;
            if (ds.Count > 0)
            {
                foreach (var l in ds)
                    res += LessonToStrVK(l) + "\n";
            }
            else
                res = "В этот день нет пар.";
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
        /// Convert a list of tuples representing week schedule to string, short version w/o Markdown
        /// </summary>
        /// <param name="ws">Week schedule</param>
        /// <returns></returns>
        static string WeekSchToStrVK(List<(Lesson, List<Curriculum>)> ws)
        {
            string res = String.Empty;
            if (ws.Count > 0)
            {
                for (var i = 0; i < 7; i++)
                {
                    var daysch = ws.FindAll(LLC => TimeOfLesson.Parse(LLC.Item1.timeslot).day == i);
                    if (daysch.Count > 0)
                    {
                        res += ((DayOfWeek)i).ToString() + ":\n";
                        foreach (var l in daysch)
                            res += LessonToStrVK(l) + "\n";
                    }
                    res += "\n";
                }
            }
            else
                res = "Расписание недоступно.";
            return res;
        }

        /// <summary>
        /// Convert a list of tuples representing week schedule to string
        /// </summary>
        /// <param name="ws">Week schedule</param>
        /// <returns></returns>
        static string WeekSchToStr(List<(Lesson, List<Curriculum>)> ws)
        {
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
        ///  Convert a tuple representing Lesson at time-slot and it's descr. to string, short version w/o Markdown
        /// </summary>
        /// <param name="LCG"></param>
        /// <returns></returns>
        public static string LessonTechToStrVK((Lesson, List<Curriculum>, List<TechGroup>) LCG, bool showDoW = false)
        {
            string res = string.Empty;

            if (LCG.Item3.Count > 0)
            {
                var ts = TimeOfLesson.Parse(LCG.Item1.timeslot);
                res = (showDoW ? (DayOfW)ts.day + " " : "") +
                    $"{ts.starth}:{ts.startm.ToString("D2")}–{ts.finishh}:{ts.finishm.ToString("D2")}"
                    + (ts.week != -1 ? (ts.week == 0 ? " в.н." : " н.н.") : "") + " — ";
                res += string.Join('\n', LCG.Item2.Select(c => c.subjectabbr + ", а." + c.roomname)) + ", " +
                        StuDegreeShort(LCG.Item3.First().degree) + " " +
                        string.Join(", ", LCG.Item3.Select(g => g.gradenum + "." + g.groupnum));
            }
            else
                res = "Нет информации о парах для вас.";
            return res;
        }

        /// <summary>
        ///  Convert a tuple representing Lesson at time-slot and it's descr. to string
        /// </summary>
        /// <param name="LCG"></param>
        /// <returns></returns>
        public static string LessonTechToStr((Lesson, List<Curriculum>, List<TechGroup>) LCG, bool showDoW = false)
        {
            string res = string.Empty;

            if (LCG.Item3.Count > 0)
            {
                var ts = TimeOfLesson.Parse(LCG.Item1.timeslot);
                res = (showDoW ? "*" + (DayOfW)ts.day + "* " : "") +
                    $"*{ts.starth}:{ts.startm.ToString("D2")}–{ts.finishh}:{ts.finishm.ToString("D2")}*"
                    + (ts.week != -1 ? (ts.week == 0 ? " в.н." : " н.н.") : "") + " — ";
                res += string.Join('\n', LCG.Item2.Select(c => c.subjectname + ", ауд.*" + c.roomname)) + "*\n    " +
                        StuDegreeShort(LCG.Item3.First().degree) + " " +
                        "_" + string.Join(", ", LCG.Item3.Select(g => g.gradenum + "." + g.groupnum)) + "_";
            }
            else
                res = "Нет информации о парах для вас.";
            return res;
        }

        /// <summary>
        /// Convert a list of tuples representing day schedule for teacher to string, short version w/o Markdown
        /// </summary>
        /// <param name="ds">Day schedule</param>
        /// <returns></returns>
        static string DaySchTechToStrVK(List<(Lesson, List<Curriculum>, List<TechGroup>)> ds)
        {
            string res = String.Empty;
            if (ds.Count > 0)
            {
                foreach (var l in ds)
                    res += LessonTechToStrVK(l) + "\n";
            }
            else
                res = "В этот день нет пар.";
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


        /// <summary>
        /// Convert a list of tuples representing week schedule for teacher to string, short version w/o Markdown
        /// </summary>
        /// <param name="ws">Week schedule</param>
        /// <returns></returns>
        static string WeekSchTechToStrVK(List<(Lesson, List<Curriculum>, List<TechGroup>)> ws)
        {
            string res = String.Empty;
            if (ws.Count > 0)
            {
                for (var i = 0; i < 7; i++)
                {
                    var daysch = ws.FindAll(LLC => TimeOfLesson.Parse(LLC.Item1.timeslot).day == i);
                    if (daysch.Count > 0)
                    {
                        res += ((DayOfWeek)i).ToString() + ":\n";
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
        /// Convert a list of tuples representing week schedule for teacher to string
        /// </summary>
        /// <param name="ws">Week schedule</param>
        /// <returns></returns>
        static string WeekSchTechToStr(List<(Lesson, List<Curriculum>, List<TechGroup>)> ws)
        {
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
    }
}
