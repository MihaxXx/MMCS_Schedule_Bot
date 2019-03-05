using System;
using System.Collections.Generic;
using System.Runtime.Serialization; 

namespace API
{
    [DataContract]
    public class Schedule
    {
        [DataMember]
        // Список предметов
        public List<Lesson> lessons { get; set; }

        [DataMember]
        // Список расписаний 
        public List<Curriculum> curricula { get; set; }
    }

	/// <summary>
	/// Занятие
	/// </summary>
    [DataContract]
    public class Lesson
    {
        [DataMember]
        // Индентификатор
        public int id { get; set; } 

        [DataMember]
        // Другой идентификатор
        public int uberid { get; set; } 

        [DataMember]
        // Количество предметов
        public int subcount { get; set; }

        [DataMember]
        // Время предмета
        public string timeslot { get; set; }

        public void Print()
        {
            Console.WriteLine($"{id} {uberid} {subcount} {timeslot}");
        }

		public override string ToString()
		{
			return $"{id} {uberid} {subcount} {timeslot}";
		}
	} 
	/// <summary>
	/// Данные о занятии
	/// </summary>
    [DataContract] 
    public class Curriculum
    {
        [DataMember]
        // Идентификатор
        public int id { get; set; } 

        [DataMember]
        // Идентификатор предмета
        public int lessonid { get; set; } 

        [DataMember]
        // Число предметов
        public int subnum { get; set; } 

        [DataMember]
        // Идентификатор предмета
        public int subjectid { get; set; } 

        [DataMember] 
        // Имя предмеита
        public string subjectname { get; set; } 

        [DataMember] 
        // Аббревиатура предмета
        public string subjectabbr { get; set; } 

        [DataMember]
        // Идентификатор преподавателя
        public int teacherid { get; set; } 

        [DataMember]
        // ФИО преподавателя
        public string teachername { get; set; } 

        [DataMember]
        // Ученая степень преподавателя
        public string teacherdegree { get; set; }
         
        [DataMember]
        // Идентификатор аудитории
        public int roomid { get; set; } 

        [DataMember]
        // Наименование аудитории
        public string roomname { get; set; }

        public void Print()
        {
            Console.WriteLine($"{subjectname}, преп. {teachername}, ауд. {roomname}");
        }

		public override string ToString()
		{
			return $"{subjectname}, преп. {teachername}, ауд. {roomname}";
		}
	}

	/// <summary>
	/// Расписание группы
	/// </summary>
	[DataContract] 
    public class SchOfGroup: Schedule
    {
        public void Print()
        {
            foreach (Lesson lesson in lessons)
                lesson.Print();
            foreach (Curriculum cur in curricula)
                cur.Print();
        }
    }

    [DataContract]
    /// <summary>
    /// Класс группы 
    /// </summary>
    public class Groups
    {
        [DataMember]
        public int uberid { get; set; }

        [DataMember]
        public int groupnum { get; set; }

        [DataMember]
        public int gradenum { get; set; }

        [DataMember]
        public string degree { get; set; }

        [DataMember]
        public string name { get; set; }

        public void Print()
        {
            Console.WriteLine($"{uberid}, {groupnum}, {gradenum}, {degree}, {name}");
        }

        public override string ToString()
        {
            return $"{uberid}, {groupnum}, {gradenum}, {degree}, {name}";
        }
    }

    /// <summary>
    /// Расписание преподавателя
    /// </summary>
    [DataContract]
    public class SchOfTeacher: Schedule
    {
        [DataMember]
        // Список групп
        public List<Groups> groups { get; set; }

        public void Print()
        {
            foreach (Lesson lesson in lessons)
                lesson.Print();
            foreach (Curriculum cur in curricula)
                cur.Print();
            foreach (Groups group in groups)
                group.Print(); 
        }
    }

    /// <summary>
    /// Группа
    /// </summary>
    [DataContract]
	public class Group
	{
		[DataMember]
		// Идентификатор группы
		public int id { get; set; }

		[DataMember]
		// Имя группы
		public string name { get; set; }

		[DataMember]
		// Номер группы
		public int num { get; set; }

		[DataMember]
		// Уровень группы
		public int gradeid { get; set; }

		public void Print()
		{
			Console.WriteLine($"id: {id}, имя: {name}, номер: {num}, gradeid: {gradeid}");
		}
	}
	/// <summary>
	/// Неделя
	/// </summary>
	[DataContract]
	public class Week
	{
		// Текущая неделя 0 - верхняя, 1 - нижняя 
		[DataMember]
		public int type { get; set; }
	}

	/// <summary>
	/// Класс дисциплины
	/// </summary>
	[DataContract]
	public class Subject
	{
		// Индентификатор
		[DataMember]
		public int id { get; set; }
		// Наименование предмета
		[DataMember]
		public string name { get; set; }
		// Сокращенное наименование предмета
		[DataMember]
		public string abbr { get; set; }
		// Конструктор класса
		Subject(int id, string name, string abbr)
		{
			this.id = id;
			this.name = name;
			this.abbr = abbr;
		}
		// Конструктор по умолчанию
		Subject() { }
		/// <summary>
		/// Метод печати класса дисциплины
		/// </summary>
		public void Print()
		{
			Console.WriteLine($"id: {id}, предмет: {name} ({abbr})");
		}
	}
	/// <summary>
	/// Информация о конкретном предмете по его номеру ID
	/// </summary>
	[DataContract]
	public class LessonInfo
	{
		[DataMember]
		// Список предметов
		public Lesson lesson { get; set; }

		[DataMember]
		// Список расписаний 
		public Curriculum curricula { get; set; }
	}
}
