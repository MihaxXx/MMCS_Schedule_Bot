using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using SchRequests; 

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

        /// <summary> 
        /// Gets nearest Lesson for the Teacher 
        /// </summary> 
        /// <param name="teacherID"></param> 
        /// <returns></returns> 
        public static (Lesson, List<Curriculum>, List<Group>) GetCurrentLessonforTeacher(int teacherID)
        {
            string url = "http://schedule.sfedu.ru/APIv1/schedule/teacher/" + teacherID;
            string response = SchRequests.SchRequests.Request(url);
            SchOfTeacher schedule = SchRequests.SchRequests.DeSerializationObjFromStr<SchOfTeacher>(response);
            if (schedule.lessons.Count > 0)
            {
                var les_res = schedule.lessons.OrderBy(l1 => TimeOfLesson.GetMinsToLesson(TimeOfLesson.Parse(l1.timeslot))).First();
                var cur_res = schedule.curricula.FindAll(c => c.lessonid == les_res.id);
                var gr_res = schedule.groups.FindAll(c => GetCurrentLesson(c.num).Item2 == cur_res);
                return (les_res, cur_res, gr_res);
            }
            else
            {
                return (new Lesson(), new List<Curriculum>(), new List<Group>());
            }
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
