using System;
using System.Collections.Generic;
using System.Runtime.Serialization; 

namespace API
{
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
    public class SchOfGroup
    {
        [DataMember] 
        // Список предметов
        public List<Lesson> lessons { get; set; } 

        [DataMember] 
        // Список расписаний 
        public List<Curriculum> curricula { get; set; }

        public void Print()
        {
            foreach (Lesson lesson in lessons)
                lesson.Print();
            foreach (Curriculum cur in curricula)
                cur.Print();
        }
    }

    /// <summary>
    /// Расписание преподавателя 
    /// </summary>
    [DataContract]
    public class SchOfTeacher
    {
        [DataMember]
        // Список предметов
        public List<Lesson> lessons { get; set; }

        [DataMember]
        // Список расписаний 
        public List<Curriculum> curricula { get; set; }
        [DataMember]
        //список групп
        public List<TechGroup> groups { get; set; }
        public void Print()
        {
            foreach (var lesson in lessons)
                lesson.Print();
            foreach (var cur in curricula)
                cur.Print();
            foreach (var gr in groups)
                gr.Print();
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

		public override string ToString()
		{
				return type == 0 ? "верхняя" : "нижняя";
		}
		public int reversedtype() => type == 0 ? 1 : 0;
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

    /// <summary>
    /// Преподаватель (для списка преподов)
    /// </summary>
    [DataContract]
    public class Teacher
    {
        [DataMember]
        // Индентификатор
        public int id { get; set; }

        [DataMember]
        // ФИО
        public string name { get; set; }

        [DataMember]
        // Учёная степень
        public string degree { get; set; }

        List<(Lesson, List<Curriculum>, List<TechGroup>)> schedule { get; set; }

        /// Конструктор
        Teacher(int id, string name, string degree)
        {
            this.id = id;
            this.name = name;
            this.degree = degree;
        }

        // Конструктор по умолчанию
        Teacher() { }

        public void Print()
        {
            Console.WriteLine($"{id}, {name}, {degree}");
        }

        public override string ToString()
        {
            return $"{id}, {name}, {degree}";
        }
    }


    [DataContract]
    public class Grade
    {
        [DataMember]
        // Индентификатор
        public int id { get; set; }

        [DataMember]
        // Номер
        public int num { get; set; }

        [DataMember]
        // Звание студента
        public string degree { get; set; }

        // Список групп
        public List<Group> Groups { get; set; }

        // Конструктор
        Grade(int id, int num, string degree)
        {
            this.id = id;
            this.num = num;
            this.degree = degree;
        }

        // Конструктор по умолчанию
        Grade() { }

        public void Print()
        {
            Console.WriteLine($"{id}, {num}, {degree}");
        }

        public override string ToString()
        {
            return $"{id}, {degree}";
        }
    }
	[DataContract]
	/// <summary>
	/// Класс группы 
	/// </summary>
	public class TechGroup
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

}
