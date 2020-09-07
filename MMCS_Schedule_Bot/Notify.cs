// Notify.cs />

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Telegram.Bot.Types.Enums;
using System.Threading.Tasks;

using API;
using ScheduleBot;
using Quartz;
using Quartz.Impl;
using NLog;
using NLog.Config;


namespace Notify
{
    public class Scheduler
    {
        static public Logger logger = LogManager.GetCurrentClassLogger();

        public async static Task RefreshNotifiedToday()
        {
            var today = DateTime.Now;
            var lastModified = JsonData.LastModified();
            var diff = today - lastModified;
            var lastModifiedHour = lastModified.Hour;
            var diffDays = diff.Days;
            logger.Info($"Last modified {diffDays} days ago");
            logger.Info($"Last modified at {lastModifiedHour}");
            if (diffDays > 0 || lastModifiedHour < 3)
            {
                await Notifier.ResetNotified();
                logger.Info("Refreshing notifiedToday field.");
            }
        }

        /// <summary>
        /// Resets the type of the week.
        /// </summary>
        public static Task ResetWeekType()
        {

            var task = Task.Factory.StartNew(Program.WeekInitPlanned);
            return task;

        }
        /// <summary>
        /// Runs evening notifier.
        /// </summary>
        public static async Task RunNotifier()
        {
            await RefreshNotifiedToday();
            await ResetWeekType();

            // Grab the Scheduler instance from the Factory
            NameValueCollection props = new NameValueCollection
                {
                    { "quartz.serializer.type", "binary" }
                };
            StdSchedulerFactory factory = new StdSchedulerFactory(props);
            IScheduler scheduler = await factory.GetScheduler();

            // and start it off
            await scheduler.Start();

            IJobDetail eveningNotify = JobBuilder.Create<EveningNotify>()
                .WithIdentity("evening")
                .Build();

            // Trigger the job to run now, and then repeat every day at the specific time
            ITrigger eveningTrigger = TriggerBuilder.Create()
                .WithIdentity("evening")
                .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(21, 00))
                .ForJob(eveningNotify)
                .Build();

            await scheduler.ScheduleJob(eveningNotify, eveningTrigger);

            // Reset notified
            IJobDetail resetNotified = JobBuilder.Create<ResetNotified>()
                .WithIdentity("resetNotified")
                .Build();

            ITrigger resetNotifiedTrigger = TriggerBuilder.Create()
                .WithIdentity("resetNotified")
                .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(07, 01))
                .ForJob(resetNotified)
                .Build();

            await scheduler.ScheduleJob(resetNotified, resetNotifiedTrigger);

            // Reset week type
            IJobDetail resetWeekType = JobBuilder.Create<ResetWeekType>()
                .WithIdentity("resetWeekType")
                .Build();

            ITrigger resetWeekTypeTrigger = TriggerBuilder.Create()
                .WithIdentity("resetWeekType")
                .WithSchedule(CronScheduleBuilder.WeeklyOnDayAndHourAndMinute(DayOfWeek.Monday, 7, 0))
                .ForJob(resetWeekType)
                .Build();

            await scheduler.ScheduleJob(resetWeekType, resetWeekTypeTrigger);

            //TODO: Impl load from API
            var lessons = new Dictionary<(int, int), string>
            {
                { (7, 44), "first" },
                { (9, 34), "second" },
                { (11, 39), "third" },
                { (13, 29), "fourth" },
                { (15, 34), "fifth" },
                { (17, 29), "sixth" },
                //{ (DateTime.Now.Hour, DateTime.Now.Minute + 1), "test" },
            };

            logger.Info($"preLessonNotifiers setting...");
            foreach (var lesson in lessons)
            {
                IJobDetail preLessonNotify = JobBuilder.Create<PreLessonNotify>()
                  .WithIdentity($"{lesson.Value}_preLessonNotify")
                  .Build();

                ITrigger preLessonTrigger = TriggerBuilder.Create()
                    .WithIdentity($"{lesson.Value}_preLessonNotify")
                    .WithCronSchedule($"0 {lesson.Key.Item2} {lesson.Key.Item1} ? * MON,TUE,WED,THU,FRI,SAT *")
                    .ForJob(preLessonNotify)
                    .Build();

                await scheduler.ScheduleJob(preLessonNotify, preLessonTrigger);
            }
            logger.Info("Done.");

        }
    }


    /// <summary>
    /// Evening notify task.
    /// </summary>
    public class ResetNotified : IJob
    {
        /// <summary>
        /// Resets notifiedToday fild.
        /// </summary>
        /// <param name="context">Context.</param>
        public async Task Execute(IJobExecutionContext context)
        {
            await Notifier.ResetNotified();
        }
    }

    /// <summary>
    /// Reset week type.
    /// </summary>
    public class ResetWeekType : IJob
    {
        /// <summary>
        /// Execute the specified context.
        /// </summary>
        /// <returns>The execute.</returns>
        /// <param name="context">Context.</param>
        public async Task Execute(IJobExecutionContext context)
        {
            await Scheduler.ResetWeekType();
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

    public class PreLessonNotify : IJob
    {
        /// <summary>
        /// Execute the preLeason notifier.
        /// </summary>
        /// <param name="context">Context.</param>
        public async Task Execute(IJobExecutionContext context)
        {
            await Task.WhenAll(Notifier.PreLessonNotify().ToArray());
        }
    }
}

/// <summary>
/// Notifier.
/// </summary>
public class Notifier
{
    static public Logger logger = LogManager.GetCurrentClassLogger();
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
        logger.Info("Notifier running...");
        var targetStudentsInfo = FilterStudents(students);
        var targetTeachersInfo = FilterTeachers(teachers);
        Task studentsTask = RunTasks(targetStudentsInfo);
        Task teachersTask = RunTasks(targetTeachersInfo);
        logger.Info("Notifier finished.");
        return teachersTask;
    }

    /// <summary>
    /// Pre lesson notify.
    /// </summary>
    public static List<Task> PreLessonNotify()
    {
        string TOKEN = Program.ReadToken();
        BOT = new Telegram.Bot.TelegramBotClient(TOKEN);

        (List<User> students, List<User> teachers) = SplitUsersPreLesson(Program.UserList);
        logger.Info("PreLessonNotifier running...");
        if (students.Any())
            logger.Info($"Students cnt: {students.Count}");
        if (teachers.Any())
            logger.Info($"Teachers cnt: {teachers.Count}");

        List<Task> tasks = new List<Task>();
        var targetStudentsInfo = FilterStudentsPreLesson(students);
        if (targetStudentsInfo.Any())
        {
            logger.Info($"Target students cnt: {students.Count}");
            foreach (var target_student in targetStudentsInfo)
            {
                logger.Info($"Target teacher: {target_student}");
            }
            Task studentsTask = RunTasks(targetStudentsInfo);
            tasks.Append(studentsTask);
        }
        else
        {
            logger.Info("No target students.");
        }
        var targetTeachersInfo = FilterTeachersPreLesson(teachers);
        if (targetTeachersInfo.Any())
        {
            logger.Info($"Target teachers cnt: {teachers.Count}");
            foreach (var target_teacher in targetTeachersInfo)
            {
                logger.Info($"Target teacher: {target_teacher}");
            }
            Task teachersTask = RunTasks(targetTeachersInfo);
            tasks.Append(teachersTask);

        }
        else
        {
            logger.Info("No target teachers.");
        }
        JsonData.WriteData();
        logger.Info("PreLessonNotifier finished.");
        return tasks;
    }


    /// <summary>
    /// Resets the todayNotified field for all users.
    /// </summary>
    public static Task ResetNotified()
    {
        logger.Info("Reset notified running...");
        var task = Task.Factory.StartNew(() =>
        {
            foreach (var user in Program.UserList)
            {
                user.Value.notifiedToday = false;
            }
            logger.Info("Reset notified finished.");
            JsonData.WriteData();
        });

        return task;
    }

    /// <summary>
    /// Splits the users for pre lesson.
    /// </summary>
    /// <param name="users">Users.</param>
    private static (List<User>, List<User>) SplitUsersPreLesson(Dictionary<long, User> users)
    {
        List<User> students = new List<User>();
        List<User> teachers = new List<User>();
        foreach (var user in users)
        {
            if (user.Value.Info == User.UserInfo.teacher)
            {
                if (user.Value.preLessonNotify && !user.Value.notifiedToday)
                {
                    logger.Info($"User {user.Value.id} is a teacher and subscribed on preLesson.");
                    teachers.Add(user.Value);
                }
            }
            else
            {
                if (user.Value.preLessonNotify && !user.Value.notifiedToday)
                {
                    students.Add(user.Value);
                    logger.Info($"User {user.Value.id} is a {user.Value.Info} and subscribed.");
                }
            }
        }

        return (students, teachers);
    }

    /// <summary>
    /// Filters the students pre lesson.
    /// </summary>
    /// <returns>students which need to be notified.</returns>
    /// <param name="students">Students.</param>
    private static Dictionary<long, (string, TimeOfLesson)> FilterStudentsPreLesson(List<User> students)
    {
        HashSet<int> groupIDs = GroupIDs();
        var targetStudents = new Dictionary<long, (string, TimeOfLesson)>();
        if (!groupIDs.Any())
        {
            logger.Info($"Not students subscribed now.");
            return targetStudents;
        }
        var nextLessons = NextLessons(groupIDs);

        Week curWeek = CurrentSubject.GetCurrentWeek();
        logger.Info($"Get current week: {curWeek}");
        if (!students.Any())
        {
            logger.Info("Students dict is empty.");
            return targetStudents;
        }
        logger.Info($"Students cnt: {students.Count}");
        foreach (var student in students)
        {
            logger.Info($"Cur student: {student.ToString()}");
            int studentGroupID = student.groupid;
            (Lesson lesson, List<Curriculum> curriculums) = nextLessons[studentGroupID];

            if (!curriculums.Any())
            {
                logger.Info($"Group {studentGroupID} has no lessons tommorow.");
                continue;
            }
            else
            {
                foreach (var cur in curriculums)
                    logger.Info(cur.ToString());
            }

            int curDay = CurrentSubject.GetCurDayOfWeek();
            // If user enable preLessonNotify after first lesson, we need to send tomorrow
            var todayLessons = StudentMethods.GetTodaySchedule(studentGroupID);
            if (!todayLessons.Any())
            {
                logger.Info("Hasn't lessons today.");
                continue;
            }

            foreach (var todayLesson in todayLessons)
            {
                logger.Info($"today lesson: {todayLesson}");
            }

            TimeOfLesson firstLessonTime = TimeOfLesson.Parse(todayLessons.First().Item1.timeslot);
            logger.Info($"first lesson time: {firstLessonTime}");
            TimeOfLesson timeOfLesson = TimeOfLesson.Parse(lesson.timeslot);
            logger.Info($"time of cur lesson: {timeOfLesson}");
            if (!timeOfLesson.Equals(firstLessonTime))
            {
                logger.Info($"{student.groupid} first pair already ended.");
                logger.Info($"first lesson time: {firstLessonTime}, time of lesson: {timeOfLesson}");
                continue;
            }
            else
            {
                logger.Info($"Times are equal.");
            }

            int dayOfLesson = timeOfLesson.day;
            logger.Info($"Day of lesson: {dayOfLesson}");
            int minsToLesson = TimeOfLesson.GetMinsToLesson(timeOfLesson, curWeek);
            logger.Info($"Mins to lesson: {minsToLesson}");

            if (curDay == dayOfLesson && minsToLesson < 20)
            {
                logger.Info($"Student with id:{student.id} and teacherID: {student.teacherId} will be notified.");
                string msg = BuildMsgForStudent("Первая пара сегодня:", lesson, curriculums);
                targetStudents.Add(student.id, (msg, timeOfLesson));
                Program.UserList[student.id].notifiedToday = true;
            }
            else
            {
                logger.Info($"Minutes to lesson: {minsToLesson}");
                logger.Info($"Cur day: {curDay}, day of lesson: {dayOfLesson}");
            }
        }

        if (!targetStudents.Any())
        {
            logger.Info("No target students.");
        }
        else
        {
            foreach (var user in targetStudents)
                logger.Info($"target student: {user}");
        }
        return targetStudents;
    }

    /// <summary>
    /// Filters the teachers for pre lesson.
    /// </summary>
    /// <returns>teachers which need to be notified.</returns>
    /// <param name="teachers">Teachers.</param>
    private static Dictionary<long, (string, TimeOfLesson)> FilterTeachersPreLesson(List<User> teachers)
    {
        var targetTeachers = new Dictionary<long, (string, TimeOfLesson)>();
        if (!teachers.Any())
        {
            return targetTeachers;
        }
        var nextLessonsForTeachers = NextLessonsForTeachers(teachers);

        if (!nextLessonsForTeachers.Any())
        {
            logger.Info("No next lessons for teachers.");
            return targetTeachers;
        }

        Week curWeek = CurrentSubject.GetCurrentWeek();
        if (!teachers.Any())
        {
            logger.Info("No teachers.");
            return targetTeachers;
        }

        foreach (var teacher in teachers)
        {
            int teacherID = teacher.teacherId;
            (Lesson lesson, List<Curriculum> curriculums, List<TechGroup> techGroups) = nextLessonsForTeachers[teacherID];
            if (!curriculums.Any())
            {
                logger.Info($"Teacher {teacherID} has no lessons today.");
                continue;
            }


            int curDay = CurrentSubject.GetCurDayOfWeek();
            // If user enable preLessonNotify after first lesson, we need to send tomorrow
            var todayLessons = TeacherMethods.GetTodaySchedule(teacherID);
            if (!todayLessons.Any())
            {
                logger.Info("No today lessons");
                continue;
            }
            else
            {
                foreach (var todayLesson in todayLessons)
                    logger.Info($"today lesson: {todayLesson}");
            }

            TimeOfLesson firstLessonTime = TimeOfLesson.Parse(todayLessons.First().Item1.timeslot);

            logger.Info($"first lesson time: {firstLessonTime}");
            TimeOfLesson timeOfLesson = TimeOfLesson.Parse(lesson.timeslot);
            logger.Info($"time of lesson: {timeOfLesson}");
            if (!timeOfLesson.Equals(firstLessonTime))
            {
                logger.Info($"{teacher.teacherId} first pair already ended.");
                logger.Info($"first lesson time: {firstLessonTime}, time of lesson: {timeOfLesson}");
                continue;
            }
            else
            {
                logger.Info($"Times are equal.");
            }

            int dayOfLesson = timeOfLesson.day;
            logger.Info($"Day of lesson: {dayOfLesson}");
            int minsToLesson = TimeOfLesson.GetMinsToLesson(timeOfLesson, curWeek);
            logger.Info($"Mins to lesson: {minsToLesson}");
            if (curDay == dayOfLesson && minsToLesson < 20)
            {
                logger.Info($"Teacher with id:{teacher.id} and teacherID: {teacherID} will be notified.");
                string msg = BuildMsgForTeacher("Первая пара сегодня: ", lesson, curriculums, techGroups);
                targetTeachers.Add(teacher.id, (msg, timeOfLesson));
                Program.UserList[teacher.id].notifiedToday = true;
            }
            else
            {
                logger.Info($"Minutes to lesson: {minsToLesson}");
                logger.Info($"Cur day: {curDay}, day of lesson: {dayOfLesson}");
            }
        }

        if (!targetTeachers.Any())
        {
            logger.Info("No target teachers.");
            return targetTeachers;
        }
        else
        {
            foreach (var teacher in targetTeachers)
                logger.Info($"target teacher: {teacher}");
        }
        return targetTeachers;
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
        if (!users.Any())
            return (students, teachers);
        foreach (var user in users)
        {
            if (user.Value.Info == User.UserInfo.teacher)
            {
                if (user.Value.eveningNotify)
                {
                    logger.Info($"User {user.Value.id} is a teacher and subscribed.");
                    teachers.Add(user.Value);
                }
            }
            else
            {
                if (user.Value.eveningNotify)
                {
                    students.Add(user.Value);
                }
            }
        }

        return (students, teachers);
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
                logger.Info($"Teacher {teacherID} has no lessons tommorow.");
                continue;
            }

            TimeOfLesson timeOfLesson = TimeOfLesson.Parse(lesson.timeslot);
            int dayOfLesson = timeOfLesson.day;
            int nextDay = CurrentSubject.GetNextDayOfWeek();

            if (nextDay == dayOfLesson)
            {
                string msg = BuildMsgForTeacher("Завтрашняя первая пара:", lesson, curriculums, techGroups);
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
                logger.Info($"Group {studentGroupID} has no lessons tommorow.");
                continue;
            }
            TimeOfLesson timeOfLesson = TimeOfLesson.Parse(lesson.timeslot);
            int dayOfLesson = timeOfLesson.day;
            int nextDay = CurrentSubject.GetNextDayOfWeek();

            if (nextDay == dayOfLesson)
            {
                string msg = BuildMsgForStudent("Завтрашняя первая пара:", lesson, curriculums);
                targetStudents.Add(student.id, (msg, timeOfLesson));
            }
        }

        return targetStudents;
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
            if (user.Value.groupid != 0)
            {
                groupIDs.Add(user.Value.groupid);
            }
        }
        logger.Info($"Total groupIDS cnt: {groupIDs.Count}");

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
        logger.Info($"Total teacherIDs cnt: {teachersIDs.Count}");

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

        logger.Info($"Fetching next lessons for students.");
        foreach (int groupID in groupsIDs)
        {
            var nextLesson = StudentMethods.GetCurrentLesson(groupID);
            nextLessons.Add(groupID, nextLesson);
        }
        logger.Info($"Fetched lessons for students : {nextLessons.Count}");
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

        logger.Info($"Fetching next lessons for teachers.");
        foreach (User teacher in teachers)
        {
            var nextLessonForTeacher = TeacherMethods.GetCurrentLesson(teacher.teacherId);
            nextLessonsForTeachers.Add(teacher.teacherId, nextLessonForTeacher);
        }
        logger.Info($"Fetched lessons for teachers : {nextLessonsForTeachers.Count}");
        return nextLessonsForTeachers;
    }
    /// <summary>
    /// Builds the message for user.
    /// </summary>
    /// <returns>The message for user.</returns>
    /// <param name="lesson">lesson.</param>
    /// <param name="curriculums">Curriculums.</param>
    private static string BuildMsgForStudent(string prefix, Lesson lesson, List<Curriculum> curriculums)
    {
        string lessonInfo = Program.LessonToStr((lesson, curriculums));
        return $"*{prefix}*\n{lessonInfo}";
    }

    /// <summary>
    /// Builds the message for teacher.
    /// </summary>
    /// <returns>The message for user.</returns>
    /// <param name="lesson">lesson.</param>
    /// <param name="curriculums">Curriculums.</param>
    /// <param name="groups">Teacher groups.</param>
    private static string BuildMsgForTeacher(string prefix, Lesson lesson, List<Curriculum> curriculums, List<TechGroup> groups)
    {
        string lessonInfo = Program.LessonTechToStr((lesson, curriculums, groups));
        return $"*{prefix}*\n{lessonInfo}";
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
        logger.Info($"Sending to {id} with message: {message}");
        await BOT.SendTextMessageAsync(id, message, ParseMode.Markdown);
    }
}

