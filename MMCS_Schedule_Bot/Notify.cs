// Notify.cs />

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Telegram.Bot.Types.Enums;
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

                IJobDetail job = JobBuilder.Create<EveningNotify>()
                    .WithIdentity("evening")
                    .Build();

                // Trigger the job to run now, and then repeat every day at the specific time
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity("evening")
                    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(21, 00))
                    .ForJob(job)
                    .Build();

                await scheduler.ScheduleJob(job, trigger);

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
                await Notifier.EveningNotify();
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
        public static Task EveningNotify()
        {
            string TOKEN = Program.ReadToken();
            BOT = new Telegram.Bot.TelegramBotClient(TOKEN);
            (List<User> students, List<User> teachers) = SplitUsers(Program.UserList);
            Console.WriteLine("Notifier running...");
            var targetStudentsInfo = FilterStudents(students);
            var targetTeachersInfo = FilterTeachers(teachers);
            Task studentsTask = RunTasks(targetStudentsInfo);
            Task teachersTask = RunTasks(targetTeachersInfo);
            //BuildPreLessonNotifiers();
            Console.WriteLine("Notifier finished.");
            return teachersTask;
        }

        /// <summary>
        /// Builds the pre lesson notifiers.
        /// </summary>
        /// <param name="UserMsgs">User msgs.</param>
        private static void BuildPreLessonNotifiers(Dictionary<long, (string, TimeOfLesson)> UserMsgs)
        {
            foreach (var item in UserMsgs)
            {
                var id = item.Key;
                var msg = item.Value.Item1;
                var timeOfLesson = item.Value.Item2;
                var hour = timeOfLesson.starth;
                var minute = timeOfLesson.startm;


            }
        }

        /// <summary>
        /// Splits the users to students and teachers.
        /// </summary>
        /// <returns>List of students and list of teachers</returns>
        /// <param name="users">Users.</param>
        private static (List<User>, List<User>) SplitUsers(Dictionary<long, User> users)
        {
            List<User> students = new List<User>();
            List<User> teachers = new List<User>();
            foreach (var user in users)
            {
                if (user.Value.Info == User.UserInfo.teacher)
                {
                    if (user.Value.eveningNotify)
                        teachers.Add(user.Value);
                }
                else
                {
                    if (user.Value.eveningNotify)
                        students.Add(user.Value);
                }
            }

            return (students, teachers);
        }

        /// <summary>
        /// Fetching group IDs by users course and group
        /// </summary>
        /// <returns>Set of group IDs</returns>
        private static HashSet<int> GroupIDs()
        {
            HashSet<int> groupIDs = new HashSet<int>();
            foreach (var user in Program.UserList)
            {
                groupIDs.Add(user.Value.groupid);
            }

            return groupIDs;
        }


        /// <summary>
        /// Fetching group IDs by users course and group
        /// </summary>
        /// <returns>Set of group IDs</returns>
        private static HashSet<int> TeachersIDs(List<User> teachers)
        {
            HashSet<int> teachersIDs = new HashSet<int>();
            foreach (var teacher in teachers)
            {
                teachersIDs.Add(teacher.teacherId);
            }

            return teachersIDs;
        }

        /// <summary>
        /// Fetching next lessons for each group ID
        /// </summary>
        /// <returns>Dict of lessons</returns>
        /// <param name="groupsIDs">Group ids.</param>
        private static Dictionary<int, (Lesson, List<Curriculum>)> NextLessons(HashSet<int> groupsIDs)
        {
            var nextLessons = new Dictionary<int, (Lesson, List<Curriculum>)>();

            foreach (int groupID in groupsIDs)
            {
                var nextLesson = CurrentSubject.GetCurrentLesson(groupID);
                nextLessons.Add(groupID, nextLesson);
            }
            return nextLessons;
        }

        /// <summary>
        /// Fetching next lessons for each teacher ID
        /// </summary>
        /// <returns>Dict of lessons</returns>
        /// <param name="teachers">Teachers.</param>
        private static Dictionary<int, (Lesson, List<Curriculum>, List<TechGroup>)> NextLessonsForTeachers(List<User> teachers)
        {
            var nextLessonsForTeachers = new Dictionary<int, (Lesson, List<Curriculum>, List<TechGroup>)>();

            foreach (User teacher in teachers)
            {
                var nextLessonForTeacher = CurrentSubject.GetCurrentLessonforTeacher(teacher.teacherId);
                nextLessonsForTeachers.Add(teacher.teacherId, nextLessonForTeacher);
            }
            return nextLessonsForTeachers;
        }

        /// <summary>
        /// Builds the message for user.
        /// </summary>
        /// <returns>The message for user.</returns>
        /// <param name="lesson">lesson.</param>
        /// <param name="curriculums">Curriculums.</param>
        private static string BuildMsgForEvening(Lesson lesson, List<Curriculum> curriculums)
        {
            string day = "Завтрашняя первая пара:";
            string lessonInfo = Program.LessonToStr((lesson, curriculums));

            return $"*{day}*\n{lessonInfo}";
        }

        /// <summary>
        /// Builds the message for teacher.
        /// </summary>
        /// <returns>The message for user.</returns>
        /// <param name="lesson">lesson.</param>
        /// <param name="curriculums">Curriculums.</param>
        /// <param name="groups">Teacher groups.</param>
        private static string BuildMsgForTeacherEvening(Lesson lesson, List<Curriculum> curriculums, List<TechGroup> groups)
        {
            string day = "Завтрашняя первая пара:";
            string lessonInfo = Program.LessonTechToStr((lesson, curriculums, groups));

            return $"*{day}*\n{lessonInfo}";
        }

        /// <summary>
        /// Filters the users, if user have lessons tommorow, then we add him, otherwise - no.
        /// </summary>
        /// <returns>Dict of user ids and messages.</returns>
        private static Dictionary<long, (string, TimeOfLesson)> FilterTeachers(List<User> teachers)
        {
            var targetTeachers = new Dictionary<long, (string, TimeOfLesson)>();
            if (!teachers.Any())
            {
                return targetTeachers;
            }
            var nextLessonsForTeachers = NextLessonsForTeachers(teachers);

            foreach (var teacher in teachers)
            {
                int teacherID = teacher.teacherId;
                (Lesson lesson, List<Curriculum> curriculums, List<TechGroup> techGroups) = nextLessonsForTeachers[teacherID];
                if (curriculums.Count == 0)
                {
                    Console.WriteLine($"Teacher {teacherID} has no lessons tommorow.");
                    continue;
                }

                TimeOfLesson timeOfLesson = TimeOfLesson.Parse(lesson.timeslot);
                int dayOfLesson = timeOfLesson.day;
                int nextDay = Program.GetNextDayOfWeek();

                if (nextDay == dayOfLesson)
                {
                    string msg = BuildMsgForTeacherEvening(lesson, curriculums, techGroups);
                    targetTeachers.Add(teacher.id, (msg, timeOfLesson));
                }
            }

            return targetTeachers;
        }

        /// <summary>
        /// Filters the users, if user have lessons tommorow, then we add him, otherwise - no.
        /// </summary>
        /// <returns>Dict of user ids and messages.</returns>
        private static Dictionary<long, (string, TimeOfLesson)> FilterStudents(List<User> students)
        {
            HashSet<int> groupIDs = GroupIDs();
            var targetStudents = new Dictionary<long, (string, TimeOfLesson)>();
            if (!groupIDs.Any())
            {
                return targetStudents;
            }
            var nextLessons = NextLessons(groupIDs);

            foreach (var student in students)
            {
                int studentGroupID = student.groupid;
                (Lesson lesson, List<Curriculum> curriculums) = nextLessons[studentGroupID];

                if (curriculums.Count == 0)
                {
                    Console.WriteLine($"Group {studentGroupID} has no lessons tommorow.");
                    continue;
                }
                TimeOfLesson timeOfLesson = TimeOfLesson.Parse(lesson.timeslot);
                int dayOfLesson = timeOfLesson.day;
                int nextDay = Program.GetNextDayOfWeek();

                if (nextDay == dayOfLesson)
                {
                    string msg = BuildMsgForEvening(lesson, curriculums);
                    targetStudents.Add(student.id, (msg, timeOfLesson));
                }
            }

            return targetStudents;
        }


        /// <summary>
        /// Runs the tasks asynchronously.
        /// </summary>
        /// <param name="tasks">Tasks dict.</param>
        public static Task RunTasks(Dictionary<long, (string, TimeOfLesson)> tasks)
        {
            Task t = Task.WhenAll(tasks.Select(i => SendMsg(i.Key, i.Value.Item1)));
            return t;
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
            await BOT.SendTextMessageAsync(id, message, ParseMode.Markdown);
        }
    }
}
