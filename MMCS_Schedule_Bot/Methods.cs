using System; 
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using SchRequests; 
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

    /// <summary>
    /// Возвращает расписание пользователя User
    /// </summary>
    public static SchOfGroup GetSchedule(User user)
    {
        string url = "";
        SchOfGroup schedule = new SchOfGroup(); 
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
        return schedule; 
    }

    /// <summary>
    /// Расписание на день
    /// </summary>
    public class DaySchedule
    {
        /// <summary>
        /// Получает расписание на день для пользователя User и номера дня недели dayofweek
        /// </summary>
        /// <param name="user"></param>
        /// <param name="dayofweek"></param>
        /// <returns></returns>
        public static List<LessonInfo> GetDaySchedule(SchOfGroup schedule, int dayofweek, Week week)
        {
            List<LessonInfo> les = new List<LessonInfo>(); 
            foreach (var l in schedule.lessons) 
                if ((TimeOfLesson.Parse(l.timeslot).day ==
            dayofweek) && ((TimeOfLesson.Parse(l.timeslot).week == week.type) ||
            (TimeOfLesson.Parse(l.timeslot).week == -1))) 
                    les.Add(new LessonInfo(l, schedule.curricula.Where(c => c.lessonid == l.id))); 
            return les;
        }

        /// <summary>
        /// Создаёт массив строк, в которых записано расписание на день (первая строчка - заголовок)
        /// </summary>
        /// <returns></returns>
        public string[] ToText(this List<LessonInfo> l) 
        {
            string[] str = new string[l.lessons.Count + 1];
            str[0] = $"Расписание на день для '{user.id}'";
            int i = 1;
            foreach (var c in l.curricula)
            {
                str[i] = $"#{i}: {c}";
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
        /// <summary>
        /// Получает расписание на неделю для пользователя по введённому полному расписанию
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static List<List<LessonInfo>> GetWeekSchedule(SchOfGroup schedule)
        {
            Week week = CurrentSubject.GetCurrentWeek(); 
            var lst = new List<List<LessonInfo>>();
            lst.Capacity = 7;
            for (int i = 0; i < 7; i++)
            {
                lst.Add(DaySchedule.GetDaySchedule(schedule, i, week));
            }
            return lst; 
        }

        /// <summary>
        /// Создаёт массив строковых описаний дней недели
        /// </summary>
        /// <returns></returns>
        public string[][] ToText(this List<List<LessonInfo>> l)
        {
            string[][] str = new string[7];
            for (int i = 0; i < 7; i++)
                str[i] = DaySchedule.ToText(l[i]); 
            return str; 
        }
    }
}