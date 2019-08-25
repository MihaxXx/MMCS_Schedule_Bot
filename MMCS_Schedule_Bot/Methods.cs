using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

using static API.CurrentSubject;
using NLog;

namespace API
{
    public class Elective
    {
        public string name { get; set; }
        public string teacher { get; set; }
        public string day { get; set; }
        public string time { get; set; }
        public int? room { get; set; }
        public string url { get; set; }

        public Elective(string n, string teach, string d, string t, string r, string u) 
        {
            name = n;
            teacher = teach;
            day = d;
            time = t;
            if (int.TryParse(r, out var tmp))
                room = tmp;
            else
                room = null;
            url = u;
        }

        public override string ToString()
        {
            return
                $"*{name}* \n" +
                $"{(day == "уточняйте" ? "День уточняйте" : $"*{day}*")}, " +
                $"{(time == "уточняйте" ? "начало уточняйте" : $"*{time}*")} " +
                $"\n    преп. _{teacher}_, " +
                $"{(!room.HasValue ? "ауд уточняйте" : $"ауд. {room}")} " +
                $"{(url != "" ? $"\n ссылка: {url}" : "")} \n\n";
        }

        static public IEnumerable<Elective> GetElectives(string fName = "Electives.csv")
        {
            return File.ReadLines(fName).Select(x=>x.Split(';')).
                Select(l=>new Elective(l[0], l[1], l[2], l[3], l[4], l[5]));
        }

        static public string ElectivesToString(IEnumerable<Elective> ie)
        {
            string ans = "";
            foreach (var x in ie)
            {
                ans += x;
            }
            return ans;
        }
    }
    /// <summary>
    /// Time-slot struct
    /// </summary>
    public class TimeOfLesson
    {
	
	public static Week curWeek { get; set; }
		//0..6 = пн..вс
        public int day { get; set; }
        public int starth { get; set; }
        public int startm { get; set; }
        public int finishh { get; set; }
        public int finishm { get; set; }
		//"full" = -1,"upper"= 0, "lower"= 1
		public int week { get; set; }

		public override string ToString()
		{
			return (day + 1) + ". " + starth + ":" + startm + " - " + finishh + ":" + finishm + " " + week + ". н.";
		}

		public bool Equals(TimeOfLesson other)
        	{
        	    return string.Equals(this.ToString(), other.ToString());
        	}
		/// <summary>
		/// Silent convert time units from string to integer
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		private static int ToIntegerTime(string time)
		{
            return int.TryParse(time, out int int_x) ? int_x : -1;
        }
		/// <summary>
		/// Converts the string representation of a time-slot to its TimeOfLesson equivalent.
		/// </summary>
		/// <param name="s">A string containing a time-slot to convert.</param>
		/// <returns></returns>
		public static TimeOfLesson Parse(string s)
		{
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
        /// <param name="ToL">Lesson's time-slot</param>
        /// <returns></returns>
        static public int GetMinsToLesson(TimeOfLesson ToL, Week week)
		{
			int res = 0;
			int CurWeek = week.type;
			var CurTime = System.DateTime.Now;
			int CurDay = (((int)CurTime.DayOfWeek) + 6) % 7;
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
        public static int GetCurDayOfWeek() => (((int)System.DateTime.Now.DayOfWeek) + 6) % 7;
        public static int GetNextDayOfWeek() => (((int)System.DateTime.Now.DayOfWeek) + 7) % 7;

        /// <summary>
        /// Get current week (cached)
        /// </summary>
        /// <returns></returns>
        public static Week GetCurrentWeek()
        {
            return TimeOfLesson.curWeek;
        }
        /// <summary>
        /// Request current week
        /// </summary>
        /// <returns></returns>
        public static Week RequestCurrentWeek()
        {
            string url = "http://schedule.sfedu.ru/APIv0/time/week/";
            string response = SchRequests.SchRequests.Request(url);
            Week week = SchRequests.SchRequests.DeSerializationObjFromStr<Week>(response);
            return week;
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
    }
    public static class StudentMethods
    {
        static public Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// String representation (grade, program, course.group) for students groupID
        /// </summary>
        /// <param name="groupID"></param>
        /// <returns></returns>
        public static string groupIDToCourseGroup(int groupID)
        {
            var grade = ScheduleBot.Program.GradeList.Find(grad => grad.Groups.Any(group => group.id == groupID));
            var grup = grade.Groups.Find(group => group.id == groupID);
            return $"{ScheduleBot.Program.StuDegreeShort(grade.degree)} {grup.name} {grade.num}.{grup.num}";
        }

		/// <summary>
		/// Get next Lesson and it's curriculum (cached)
		/// </summary>
		/// <param name="groupID"></param>
		/// <returns></returns>
		public static (Lesson,List<Curriculum>) GetCurrentLesson(int groupID)
        {
            var schedule = GetWeekSchedule(groupID);
            if (schedule.Count > 0)
			{
                var cur_week = GetCurrentWeek();
                return GetWeekSchedule(groupID).OrderBy(l1 => TimeOfLesson.GetMinsToLesson(TimeOfLesson.Parse(l1.Item1.timeslot), cur_week)).First();
			}
            return (new Lesson(),new List<Curriculum>());  
        }

		/// <summary>
		/// Comparison (Lesson, List of Curriculum) by day of week and time
		/// </summary>
		/// <param name="llc1"></param>
		/// <param name="llc2"></param>
		/// <returns></returns>
		private static int CmpLLCByDayAndTime((Lesson, List<Curriculum>) llc1, (Lesson, List<Curriculum>) llc2)
		{
			var tol1 = TimeOfLesson.Parse(llc1.Item1.timeslot);
			var tol2 = TimeOfLesson.Parse(llc2.Item1.timeslot);
			return (tol1.day * 24 + tol1.starth * 60 + tol1.startm) - (tol2.day * 24 + tol2.starth * 60 + tol2.startm);
		}
        /// <summary>
        /// Request week schedule for <paramref name="groupID"/>
        /// </summary>
        /// <param name="groupID"></param>
        /// <returns>Ordered by day of week and time week schedule</returns>
        public static List<(Lesson, List<Curriculum>)> RequestWeekSchedule(int groupID)
		{
			string url = "http://schedule.sfedu.ru/APIv0/schedule/group/" + groupID;
			string response = SchRequests.SchRequests.Request(url);
			var schedule = SchRequests.SchRequests.DeSerializationObjFromStr<SchOfGroup>(response);
			var res = new List<(Lesson, List<Curriculum>)>();
			if (schedule.lessons.Count > 0)
			{
				foreach (var les in schedule.lessons)
					res.Add((les, schedule.curricula.FindAll(c => c.lessonid == les.id)));
			}
			res.Sort(CmpLLCByDayAndTime);
			return res;
		}
        /// <summary>
		/// Get week schedule for <paramref name="groupID"/> (cached)
		/// </summary>
		/// <param name="groupID"></param>
		/// <returns>Ordered by day of week and time week schedule</returns>
        public static List<(Lesson, List<Curriculum>)> GetWeekSchedule(int groupID)
        {
            //if cached schedule is too old than request new
            if (!ScheduleBot.Program.GroupShedule.ContainsKey(groupID) || DateTime.Now - ScheduleBot.Program.GroupShedule[groupID].Item2 > TimeSpan.FromHours(7*24))
            {
                try
                { ScheduleBot.Program.GroupShedule[groupID] = (RequestWeekSchedule(groupID), DateTime.Now); }
                catch (System.Net.WebException e)
                {
                    logger.Warn($"Не удалось обновить расписание для группы groupID {groupID}.", e);
                }
            }
            return ScheduleBot.Program.GroupShedule[groupID].Item1;
        }

        /// <summary>
        /// Ordered <paramref name="day"/> schedule
        /// </summary>
        /// <param name="groupID"></param>
        /// <param name="day">Day of week, 0..6</param>
        /// <returns></returns>
        public static List<(Lesson, List<Curriculum>)> GetDaySchedule(int groupID, int day)
		{
			return GetWeekSchedule(groupID).FindAll(LLC => TimeOfLesson.Parse(LLC.Item1.timeslot).day == day);
		}

        public static List<(Lesson, List<Curriculum>)> GetTodaySchedule(int groupID)
        {
            var day = GetCurDayOfWeek();
            var week = GetCurrentWeek().type;
            return GetWeekSchedule(groupID).FindAll(LLC => { var tol = TimeOfLesson.Parse(LLC.Item1.timeslot); return tol.day == day && (tol.week == -1 || tol.week == week); } );
        }
        public static List<(Lesson, List<Curriculum>)> GetTomorrowSchedule(int groupID)
        {
            var day = GetNextDayOfWeek();
            var week = day != 0 ? GetCurrentWeek().type : GetCurrentWeek().reversedtype();
            return GetWeekSchedule(groupID).FindAll(LLC => { var tol = TimeOfLesson.Parse(LLC.Item1.timeslot); return tol.day == day && (tol.week == -1 || tol.week == week); });
        }
    }

    public static class TeacherMethods
    {
        static public Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Get teachers list (not cached)
        /// </summary>
        /// <returns></returns>
        public static Teacher[] GetTeachersList()
        {
            string url = $"http://schedule.sfedu.ru/APIv0/teacher/list";
            string response = SchRequests.SchRequests.Request(url);
            return SchRequests.SchRequests.DeSerializationFromStr<Teacher>(response);
        }

        /// <summary> 
        /// Get next Lesson for the Teacher (cached)
        /// </summary> 
        /// <param name="teacherID"></param> 
        /// <returns></returns> 
        public static (Lesson, List<Curriculum>, List<TechGroup>) GetCurrentLesson(int teacherID)
        {
            var schedule = GetWeekSchedule(teacherID);
            if (schedule.Count > 0)
            {
                var cur_week = GetCurrentWeek();
                return GetWeekSchedule(teacherID).OrderBy(l1 => TimeOfLesson.GetMinsToLesson(TimeOfLesson.Parse(l1.Item1.timeslot), cur_week)).First();
            }
            return (new Lesson(), new List<Curriculum>(), new List<TechGroup>());
        }

        /// <summary>
        /// Comparison(Lesson, List of Curriculum, List of TechGroup)
        /// </summary>
        /// <param name="llc1"></param>
        /// <param name="llc2"></param>
        /// <returns></returns>
        private static int CmpLLCGByDayAndTime((Lesson, List<Curriculum>, List<TechGroup>) llc1, (Lesson, List<Curriculum>, List<TechGroup>) llc2)
        {
            var tol1 = TimeOfLesson.Parse(llc1.Item1.timeslot);
            var tol2 = TimeOfLesson.Parse(llc2.Item1.timeslot);
            return (tol1.day * 24 + tol1.starth * 60 + tol1.startm) - (tol2.day * 24 + tol2.starth * 60 + tol2.startm);
        }

        /// <summary>
        /// Request week schedule for <paramref name="teacherID"/>
        /// </summary>
        /// <param name="teacherID"></param>
        /// <returns>Ordered by day of week and time week schedule</returns>
		public static List<(Lesson, List<Curriculum>, List<TechGroup>)> RequestWeekSchedule(int teacherID)
        {
            string url = "http://schedule.sfedu.ru/APIv1/schedule/teacher/" + teacherID;
            string response = SchRequests.SchRequests.Request(url);
            var schedule = SchRequests.SchRequests.DeSerializationObjFromStr<SchOfTeacher>(response);
            var res = new List<(Lesson, List<Curriculum>, List<TechGroup>)>();
            if (schedule.lessons.Count > 0)
            {
                foreach (var les in schedule.lessons)
                    res.Add((les, schedule.curricula.FindAll(c => c.lessonid == les.id), schedule.groups.FindAll(g => g.uberid == les.uberid)));
            }
            res.Sort(CmpLLCGByDayAndTime);
            return res;
        }

        /// <summary>
		/// Get week schedule for <paramref name="teacherID"/> (cached)
		/// </summary>
		/// <param name="teacherID"></param>
		/// <returns>Ordered by day of week and time week schedule</returns>
        public static List<(Lesson, List<Curriculum>, List<TechGroup>)> GetWeekSchedule(int teacherID)
        {
            //if cached schedule is too old than request new
            if (!ScheduleBot.Program.TeacherSchedule.ContainsKey(teacherID) || DateTime.Now - ScheduleBot.Program.TeacherSchedule[teacherID].Item2 > TimeSpan.FromHours(7 * 24))
            {
                try
                { ScheduleBot.Program.TeacherSchedule[teacherID] = (RequestWeekSchedule(teacherID), DateTime.Now); }
                catch (System.Net.WebException e)
                {
                    logger.Warn($"Не удалось обновить расписание для преподавателя teacherID {teacherID}.", e);
                }
            }
            return ScheduleBot.Program.TeacherSchedule[teacherID].Item1;
        }

        /// <summary>
        /// Ordered <paramref name="day"/> schedule for teacher
        /// </summary>
        /// <param name="teacherID"></param>
        /// <param name="day">Day of week, 0..6</param>
        /// <returns></returns>
        public static List<(Lesson, List<Curriculum>, List<TechGroup>)> GetDaySchedule(int teacherID, int day)
        {
            return GetWeekSchedule(teacherID).FindAll(LLCG => TimeOfLesson.Parse(LLCG.Item1.timeslot).day == day);
        }

        public static List<(Lesson, List<Curriculum>, List<TechGroup>)> GetTodaySchedule(int teacherID)
        {
            var day = GetCurDayOfWeek();
            var week = GetCurrentWeek().type;
            return GetWeekSchedule(teacherID).FindAll(LLCG => { var tol = TimeOfLesson.Parse(LLCG.Item1.timeslot); return tol.day == day && (tol.week == -1 || tol.week == week); });
        }

        public static List<(Lesson, List<Curriculum>, List<TechGroup>)> GetTomorrowSchedule(int teacherID)
        {
            var day = GetNextDayOfWeek();
            var week = day != 0 ? GetCurrentWeek().type : GetCurrentWeek().reversedtype();
            return GetWeekSchedule(teacherID).FindAll(LLCG => { var tol = TimeOfLesson.Parse(LLCG.Item1.timeslot); return tol.day == day && (tol.week == -1 || tol.week == week); });
        }
    }

    public static class GradeMethods
    {
        public static Grade[] GetGradesList()
        {
            string url = $"http://schedule.sfedu.ru/APIv1/grade/list";
            string response = SchRequests.SchRequests.Request(url);
            return SchRequests.SchRequests.DeSerializationFromStr<Grade>(response);
        }

        public static Group[] GetGroupsList(int GradeId)
        {
            string url = $"http://schedule.sfedu.ru/APIv0/group/list/" + GradeId;
            string response = SchRequests.SchRequests.Request(url);
            return SchRequests.SchRequests.DeSerializationFromStr<Group>(response);
        }
    }

}
