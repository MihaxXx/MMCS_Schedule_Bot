using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using SchRequests;
using System;
using System.IO;

namespace API
{
    public class Elective
    {
        public string name { get; set; }
        public string teacher { get; set; }
        public string day { get; set; }
        public string time { get; set; }
        public int? room { get; set; }

        public Elective(string n, string teach, string d, string t, string r) 
        {
            name = n;
            teacher = teach;
            day = d;
            time = t;
            if (int.TryParse(r, out var tmp))
                room = tmp;
            else
                room = null;

        }

        public override string ToString()
        {
            return $"{(day=="уточняйте" ? "День уточняйте" : $"*{day}*" )}, " +
                $"{(time == "уточняйте" ? "начало уточняйте" : $"*{time}*")} " +
                $"— *{name}*, \n" +
                $"\t преп. _{teacher}_, " +
                $"{(!room.HasValue ? "ауд уточняйте" : $"ауд. {room}")} \n\n";
        }

        static public IEnumerable<Elective> GetElectives(string fName = "Electives.csv")
        {
            return File.ReadLines(fName).Select(x=>x.Split(';')).
                Select(l=>new Elective(l[0], l[1], l[2], l[3], l[4]));
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
        /// <param name="ToL">Lesson's time-slot</param>
        /// <returns></returns>
        static public int GetMinsToLesson(TimeOfLesson ToL, Week week)
		{
			int res = 0;
			int CurWeek = week.type;
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
                var cur_week = GetCurrentWeek();
				var l_res = schedule.lessons.OrderBy(l1 => TimeOfLesson.GetMinsToLesson(TimeOfLesson.Parse(l1.timeslot),cur_week)).First();
				var c_res = schedule.curricula.FindAll(c => c.lessonid == l_res.id);
				return (l_res, c_res);
			}
            return (new Lesson(),new List<Curriculum>());  
        }

        /// <summary> 
        /// Gets nearest Lesson for the Teacher 
        /// </summary> 
        /// <param name="teacherID"></param> 
        /// <returns></returns> 
        public static (Lesson, List<Curriculum>, List<TechGroup>) GetCurrentLessonforTeacher(int teacherID)
        {
            string url = "http://schedule.sfedu.ru/APIv1/schedule/teacher/" + teacherID;
            string response = SchRequests.SchRequests.Request(url);
            SchOfTeacher schedule = SchRequests.SchRequests.DeSerializationObjFromStr<SchOfTeacher>(response);
            if (schedule.lessons.Count > 0)
            {
                var cur_week = GetCurrentWeek();
                var les_res = schedule.lessons.
                    OrderBy(l1 => TimeOfLesson.GetMinsToLesson(TimeOfLesson.Parse(l1.timeslot),cur_week)).First();
                var cur_res = schedule.curricula.FindAll(c => c.lessonid == les_res.id);
                var gr_res = schedule.groups.FindAll(g => g.uberid == les_res.uberid);
                return (les_res, cur_res, gr_res);
            }
            else
            {
                return (new Lesson(), new List<Curriculum>(), new List<TechGroup>());
            }
        }
		/// <summary>
		/// Comparison(Lesson, List of Curriculum)
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
		/// Ordered Week Schedule
		/// </summary>
		/// <param name="groupID"></param>
		/// <returns></returns>
		public static List<(Lesson, List<Curriculum>)> GetWeekSchedule(int groupID)
		{
			var week = CurrentSubject.GetCurrentWeek();
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
		/// Ordered <paramref name="day"/> schedule
		/// </summary>
		/// <param name="groupID"></param>
		/// <param name="day">Day of week, 0..6</param>
		/// <returns></returns>
		public static List<(Lesson, List<Curriculum>)> GetDaySchedule(int groupID, int day)
		{
			return GetWeekSchedule(groupID).FindAll(LLC => TimeOfLesson.Parse(LLC.Item1.timeslot).day == day);
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
		/// Ordered Week Schedule for teacher
		/// </summary>
		/// <param name="teacherID"></param>
		/// <returns></returns>
		public static List<(Lesson, List<Curriculum>, List<TechGroup>)> GetWeekScheduleforTeacher(int teacherID)
		{
			var week = CurrentSubject.GetCurrentWeek();
			string url = "http://schedule.sfedu.ru/APIv1/schedule/teacher/" + teacherID;
			string response = SchRequests.SchRequests.Request(url);
			var schedule = SchRequests.SchRequests.DeSerializationObjFromStr<SchOfTeacher>(response);
			var res = new List<(Lesson, List<Curriculum>, List<TechGroup>)>();
			if (schedule.lessons.Count > 0)
			{
				foreach (var les in schedule.lessons)
					res.Add((les, schedule.curricula.FindAll(c => c.lessonid == les.id),schedule.groups.FindAll(g => g.uberid == les.uberid)));
			}
			res.Sort(CmpLLCGByDayAndTime);
			return res;
		}

		/// <summary>
		/// Ordered <paramref name="day"/> schedule for teacher
		/// </summary>
		/// <param name="teacherID"></param>
		/// <param name="day">Day of week, 0..6</param>
		/// <returns></returns>
		public static List<(Lesson, List<Curriculum>, List<TechGroup>)> GetDayScheduleforTeacher(int teacherID, int day)
		{
			return GetWeekScheduleforTeacher(teacherID).FindAll(LLCG => TimeOfLesson.Parse(LLCG.Item1.timeslot).day == day);
		}
	}

    public static class TeacherMethods
    {
        public static Teacher[] GetTeachersList()
        {
            string url = $"http://schedule.sfedu.ru/APIv0/teacher/list";
            string response = SchRequests.SchRequests.Request(url);
            return SchRequests.SchRequests.DeSerializationFromStr<Teacher>(response);
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
