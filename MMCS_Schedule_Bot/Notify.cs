// Notify.cs />

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

using API;
using Console_Schedule_Bot;
using Quartz;
using Quartz.Impl;


namespace Notify
{
    public class Scheduler
    {
        /// <summary>
        /// Runs evening notifier.
        /// </summary>
        public static async Task RunEveningNotifier()
        {
            try
            {
                // Grab the Scheduler instance from the Factory
                NameValueCollection props = new NameValueCollection
                {
                    { "quartz.serializer.type", "binary" }
                };
                StdSchedulerFactory factory = new StdSchedulerFactory(props);
                IScheduler scheduler = await factory.GetScheduler();

                // and start it off
                await scheduler.Start();

                // define the job and tie it to our HelloJob class
                IJobDetail job = JobBuilder.Create<EveningNotify>()
                    .WithIdentity("evening")
                    .Build();

                // Trigger the job to run now, and then repeat every day at the specific time
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity("evening")
                    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(21, 00))
                    .ForJob(job)
                    .Build();

                // Tell quartz to schedule the job using our trigger
                await scheduler.ScheduleJob(job, trigger);

                // some sleep to show what's happening
                await Task.Delay(TimeSpan.FromSeconds(60));

                // and last shut down the scheduler when you are ready to close your program
                await scheduler.Shutdown();
            }
            catch (SchedulerException se)
            {
                Console.WriteLine(se);
            }
        }

        /// <summary>
        /// Evening notify task.
        /// </summary>
        public class EveningNotify : IJob
        {
            /// <summary>
            /// Execute the evening notifier.
            /// </summary>
            /// <param name="context">Context.</param>
            public async Task Execute(IJobExecutionContext context)
            {
                Notifier.EveningNotify();
            }
        }
    }

    /// <summary>
    /// Notifier.
    /// </summary>
    public class Notifier
    {
        /// <summary>
        /// Telegram bot.
        /// </summary>
        static Telegram.Bot.TelegramBotClient BOT;

        /// <summary>
        /// Evenings the notify.
        /// </summary>
        public static void EveningNotify()
        {
            BOT = new Telegram.Bot.TelegramBotClient("697446498:AAFkXTktghiTFGCILZUZ9XiKHZN4LKohXiI");
            var targetUsersInfo = FilterUsers();

            RunTasks(targetUsersInfo);
            Console.WriteLine("Finish.");
            System.Console.ReadLine();
        }

        /// <summary>
        /// Adds the zeros to minutes because if lesson starts at 9:00 time parser converts it to 9:0
        /// </summary>
        /// <returns>String with right time format</returns>
        /// <param name="minutes">Minutes.</param>
        private static string AddZerosToMinutes(int minutes)
        {
            string strMinutes = minutes.ToString();
            return strMinutes[0] == '0' ? strMinutes + "0" : strMinutes;
        }

        /// <summary>
        /// Fetching group IDs by users course and group
        /// </summary>
        /// <returns>Set of group IDs</returns>
        private static HashSet<int> GroupIds()
        {
            HashSet<int> groupIds = new HashSet<int>();
            foreach (var user in Program.UserList)
            {
                groupIds.Add(user.Value.groupid);
            }

            return groupIds;
        }

        /// <summary>
        /// Fetching next lessons for each group ID
        /// </summary>
        /// <returns>Dict of lessons</returns>
        /// <param name="groupIds">Group identifiers.</param>
        private static Dictionary<int, (Lesson, List<Curriculum>)> NextLessons(HashSet<int> groupIds)
        {
            var nextLessons = new Dictionary<int, (Lesson, List<Curriculum>)>();

            foreach (int groupId in groupIds)
            {
                Console.WriteLine(groupId);
                var nextLesson = CurrentSubject.GetCurrentLesson(groupId);
                Lesson lesson = nextLesson.Item1;
                List<Curriculum> curriculums = nextLesson.Item2;
                nextLessons.Add(groupId, (lesson, curriculums));
            }
            return nextLessons;
        }

        /// <summary>
        /// If it's sunday and we need to check for monday then 6 + 1 = 7, but week days is in (0...6)
        /// </summary>
        /// <returns>The day number.</returns>
        /// <param name="curDay">Current day.</param>
        private static int FitDay(int curDay)
        {
            return curDay % 7;
        }

        /// <summary>
        /// Builds the message for user.
        /// </summary>
        /// <returns>The message for user.</returns>
        /// <param name="timeOfLesson">Time of lesson.</param>
        /// <param name="curDay">Current day.</param>
        /// <param name="curriculums">Curriculums.</param>
        private static string BuildMsgForEvening(TimeOfLesson timeOfLesson, int curDay, List<Curriculum> curriculums)
        {
            string day = "Завтрашняя первая пара:";
            string startm = AddZerosToMinutes(timeOfLesson.startm);
            string finishm = AddZerosToMinutes(timeOfLesson.finishm);
            string timeInfo = $"{timeOfLesson.starth}:{startm} - {timeOfLesson.finishh}:{finishm}";

            string body = string.Empty;
            foreach (Curriculum curriculum in curriculums)
            {
                body += $"ауд. {curriculum.roomname}\n";
                body += $"{curriculum.subjectname}\n";
                string teachername = curriculum.teachername;
                string[] teachernameSplitted = teachername.Split(" ");
                string trimmed_teachername = $"{teachernameSplitted[0]} {teachernameSplitted[1][0]}. {teachernameSplitted[2][0]}.\n";
                body += trimmed_teachername;
            }

            return $"{day}\n{timeInfo}\n{body}";
        }

        /// <summary>
        /// Filters the users, if user have lessons tommorow, then we add him, otherwise - no.
        /// </summary>
        /// <returns>Dict of user ids and messages.</returns>
        private static Dictionary<long, string> FilterUsers()
        {
            HashSet<int> groupIds = GroupIds();
            var nextLessons = NextLessons(groupIds);

            var targetUsers = new Dictionary<long, string>();
            foreach (var item in Program.UserList)
            {
                var user = item.Value;
                int userGroupId = user.groupid;
                (Lesson lesson, List<Curriculum> curriculums) = nextLessons[userGroupId];

                TimeOfLesson timeOfLesson = TimeOfLesson.Parse(lesson.timeslot);
                int dayOfLesson = timeOfLesson.day;
                int curDay = FitDay((int)DateTime.Now.DayOfWeek + 1);

                if (curDay == dayOfLesson)
                {
                    string msg = BuildMsgForEvening(timeOfLesson, curDay, curriculums);
                    targetUsers.Add(user.id, msg);
                }
            }

            return targetUsers;
        }

        /// <summary>
        /// Runs the tasks asynchronously.
        /// </summary>
        /// <param name="tasks">Tasks dict.</param>
        public static async void RunTasks(Dictionary<long, string> tasks)
        {
            await Task.WhenAll(tasks.Select(i => SendMsg(i.Key, i.Value)));
        }

        /// <summary>
        /// Sends the message to user by id.
        /// </summary>
        /// <returns>The message.</returns>
        /// <param name="id">User ID.</param>
        /// <param name="message">Message to user.</param>
        public static async Task SendMsg(long id, string message)
        {
            Console.WriteLine($"Sending to {id} with message: {message}");
            await BOT.SendTextMessageAsync(id, message);
        }
    }
}
