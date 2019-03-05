using System; 
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using SchRequests; 
using static System.DateTime;
using Console_Schedule_Bot; 

/// <summary>
/// Содержит методы для получения взаимодействия с API расписания
/// </summary>
namespace API
{
	/// <summary>
	/// Time-slot struct
	/// </summary>
	public struct TimeOfLesson
    {
        public int day { get; set; }
        public int starth { get; set; }
        public int startm { get; set; }
        public int finishh { get; set; }
        public int finishm { get; set; }
		public int week { get; set; }

		/// <summary>
		/// Silent convert time units from string to integer
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		private static int ToIntegerTime(string time)
		{
			int int_x;
			return int.TryParse(time, out int_x) ? int_x : -1;
		}
		/// <summary>
		/// Converts the string representation of a time-slot to its TimeOfLesson equivalent.
		/// </summary>
		/// <param name="s">A string containing a time-slot to convert.</param>
		/// <returns></returns>
		public static TimeOfLesson Parse(string s)
		{
			//TODO:Maybe change to init with {} after Split by , and :
			s = s.Substring(1, s.Length - 2);
			TimeOfLesson t = new TimeOfLesson();
			string[] times = s.Split(',');
			t.day = ToIntegerTime(times[0]);
			t.starth = ToIntegerTime(times[1].Substring(0, 2));
			t.startm = ToIntegerTime(times[1].Substring(3, 2));
			t.finishh = ToIntegerTime(times[2].Substring(0, 2));
			t.finishm = ToIntegerTime(times[2].Substring(3, 2));
			switch (times[3])
			{
				case "full": t.week = -1; break;
				case "upper": t.week = 0; break;
				case "lower": t.week = 1; break;
			}
			return t;
		}
		/// <summary>
		/// Counts number of minutes before lesson starts
		/// </summary>
		/// <param name="Lesson's time-slot"></param>
		/// <returns></returns>
		static public int GetMinsToLesson(TimeOfLesson ToL)
		{
			int res = 0;
			int CurWeek = CurrentSubject.GetCurrentWeek().type;
			var CurTime = System.DateTime.Now;
			int CurDay = (int)CurTime.DayOfWeek - 1;
			int days = 0;

			if (ToL.day < CurDay || (ToL.day == CurDay && ToL.starth < CurTime.Hour) || (ToL.day == CurDay && ToL.starth == CurTime.Hour && ToL.startm < CurTime.Minute))//lesson is on the next week
			{
				if (ToL.starth < CurTime.Hour || (ToL.starth == CurTime.Hour && ToL.startm < CurTime.Minute))//is it necessary to include today into full days before lesson count
					days += (ToL.day + 6 - CurDay);
				else
					days += (ToL.day + 7 - CurDay);
				if (ToL.week != -1 && ToL.week != (CurWeek+1)%2)//is lesson held only on other type of weeks(U/L)
					days += 7;
			}
			else//lesson is on this week
			{
				if (ToL.starth < CurTime.Hour || (ToL.starth == CurTime.Hour && ToL.startm < CurTime.Minute))//is it necessary to include today into full days before lesson count
					days += (ToL.day - CurDay - 1);
				else
					days += (ToL.day - CurDay);
				if (ToL.week != -1 && ToL.week != CurWeek)//is lesson held only on other type of weeks(U/L)
					days += 7;
			}
			res += days * 24 * 60;

			if (ToL.starth < CurTime.Hour || (ToL.starth == CurTime.Hour && ToL.startm < CurTime.Minute))//Lesson is not today
				res += (23 - CurTime.Hour + ToL.starth) * 60 + (60 - CurTime.Minute)+ToL.startm;
			else //Lesson is held today
				res += (ToL.starth - CurTime.Hour) * 60 + (ToL.startm - CurTime.Minute);

			return res;
		}
    }

    public static class CurrentSubject
	{ 
		/// <summary>
		/// Request current week
		/// </summary>
		/// <returns></returns>
        public static Week GetCurrentWeek()
        {
            string url = "http://schedule.sfedu.ru/APIv0/time/week/";
            string response = SchRequests.SchRequests.Request(url);
			Week week = SchRequests.SchRequests.DeSerializationObjFromStr<Week>(response);
            return week; 
        }

		/// <summary>
		/// Gets Schedule GroupID by <paramref name="course"/> and <paramref name="group"/>
		/// </summary>
		/// <param name="course"></param>
		/// <param name="group"></param>
		/// <returns>GroupID</returns>
		public static int CourseGroupToID(int course, int group)
		{
			//TODO:Change to Dict to prevent unnecessary requests
			string response1 = SchRequests.SchRequests.Request("http://schedule.sfedu.ru/APIv0/group/list/"+ course);
			Group[] lstOfGroups = SchRequests.SchRequests.DeSerializationFromStr<Group>(response1);
			return lstOfGroups.Where(g => (g.num == group)).First().id;
		}
		/// <summary>
		/// Получаем информацию о предмете по его ID
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static LessonInfo GetLessonInfo(string id)
		{
			string url = "http://schedule.sfedu.ru/APIv0/schedule/lesson/" + id;
			string response = SchRequests.SchRequests.Request(url);
			LessonInfo l = SchRequests.SchRequests.DeSerializationObjFromStr<LessonInfo>(response);
			return l;
		}
		/// <summary>
		/// Gets nearest Lesson and it's curriculum
		/// </summary>
		/// <param name="groupID"></param>
		/// <returns></returns>
		public static (Lesson,List<Curriculum>) GetCurrentLesson(int groupID)
        {
            string url = "http://schedule.sfedu.ru/APIv0/schedule/group/" + groupID;
            string response = SchRequests.SchRequests.Request(url);
			SchOfGroup schedule = SchRequests.SchRequests.DeSerializationObjFromStr<SchOfGroup>(response);
			if (schedule.lessons.Count > 0)
			{
				var l_res = schedule.lessons.OrderBy(l1 => TimeOfLesson.GetMinsToLesson(TimeOfLesson.Parse(l1.timeslot))).First();
				var c_res = schedule.curricula.FindAll(c => c.lessonid == l_res.id);
				return (l_res, c_res);
			}
            return (new Lesson(),new List<Curriculum>());  
        }
	}

    enum DayOfWeek { Monday = 0, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday };

    /// <summary>
    /// Дата
    /// </summary>
    public class Date
    {
        int year;
        int month;
        int day;

        public static int GetDayOfWeek()
        {
            return (int)System.DateTime.Now.DayOfWeek; 
        }
        public static Week GetWeek()
        {
            return CurrentSubject.GetCurrentWeek(); 
        }
    }

    /// <summary>
    /// Расписание на день
    /// </summary>
    public class DaySchedule
    {
        User user; 
        Week week;
        Date date;
        List<Lesson> lessons;

        /// <summary>
        /// Конструктор класса расписания на день
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="week">Тип недели</param>
        /// <param name="date">Дата</param>
        /// <param name="lessons">Список пар</param>
        public DaySchedule(User user, Week week, Date date, List<Lesson> lessons)
        {
            this.user = user; 
            this.week = week;
            this.date = date;
            this.lessons = lessons;
        }

        /// <summary>
        /// Получает расписание на день для пользователя User и номера дня недели dayofweek
        /// </summary>
        /// <param name="user"></param>
        /// <param name="dayofweek"></param>
        /// <returns></returns>
        public static DaySchedule GetDaySchedule(User user, int dayofweek)
        {
            string url = "";
            if (user.Info == User.UserInfo.bachelor)
            {
                url = "http://schedule.sfedu.ru/APIv0/schedule/group/" + user.group;
            }
            if (user.Info == User.UserInfo.teacher)
            {
                url = "http://schedule.sfedu.ru/APIv1/schedule/teacher/" + user.id; // ?
            }
            string response = SchRequests.SchRequests.Request(url);
            SchOfGroup schedule = SchRequests.SchRequests.DeSerializationObjFromStr<SchOfGroup>(response);
            Week week = Date.GetWeek();
            List<Lesson> les = schedule.lessons.Where(l => (TimeOfLesson.Parse(l.timeslot).day ==
            dayofweek) && ((TimeOfLesson.Parse(l.timeslot).week == week.type) ||
            (TimeOfLesson.Parse(l.timeslot).week == -1))).ToList();
            return new DaySchedule(user, week, new Date(), les);
        }

        /// <summary>
        /// Создаёт массив строк, в которых записано расписание на день (первая строчка - заголовок)
        /// </summary>
        /// <returns></returns>
        public string[] ToText()
        {
            string[] str = new string[lessons.Count + 1];
            str[0] = $"Расписание на день для '{user.FIO}'";
            int i = 1;
            foreach (var les in lessons)
            {
                str[i] = $"Занятие №{i}: {CurrentSubject.GetLessonInfo(les.id.ToString()).lesson.ToString()} \n {CurrentSubject.GetLessonInfo(les.id.ToString()).curricula.ToString()}";
                i++;
            }
            return str; 
        }
    }

    /// <summary>
    /// Расписание на неделю
    /// </summary>
    public class WeekSchedule
    {
        User user; 
        List<DaySchedule> weekschedule;
        Week week;

        /// <summary>
        /// Конструктор класса расписания на неделю
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <param name="weekschedule">Список пар</param>
        /// <param name="week">Тип недели</param>
        public WeekSchedule(User user, List<DaySchedule> weekschedule, Week week)
        {
            this.user = user; 
            this.weekschedule = weekschedule;
            this.week = week;
        }

        /// <summary>
        /// Получает расписание на неделю для пользователя User
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static WeekSchedule GetWeekSchedule(User user)
        {
            var lst = new List<DaySchedule>();
            lst.Capacity = 7;
            var time = System.DateTime.Now;
            Week week = CurrentSubject.GetCurrentWeek();
            string url = "";
            Schedule schedule = new Schedule(); 
            if (user.Info == User.UserInfo.bachelor)
            {
                url = "http://schedule.sfedu.ru/APIv0/schedule/group/" + user.group;
                string response = SchRequests.SchRequests.Request(url);
                schedule = SchRequests.SchRequests.DeSerializationObjFromStr<SchOfGroup>(response);
            }
            if (user.Info == User.UserInfo.teacher)
            {
                url = "http://schedule.sfedu.ru/APIv1/schedule/teacher/" + user.id; // ?
                string response = SchRequests.SchRequests.Request(url);
                schedule = SchRequests.SchRequests.DeSerializationObjFromStr<SchOfTeacher>(response);
            }
            for (int i = 0; i < 7; i++)
            {
                List<Lesson> dayles = schedule.lessons.Where(l => (TimeOfLesson.Parse(l.timeslot).day == i) &&
                (TimeOfLesson.Parse(l.timeslot).week == week.type)).ToList();
                lst[i] = new DaySchedule(user, week, new Date(), dayles);
            }
            return new WeekSchedule(user, lst, week);
        }
    }
}