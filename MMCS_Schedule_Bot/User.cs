using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Console_Schedule_Bot
{
    class User
    {
		/// <summary>
		/// Stage of identification
		/// </summary>
        public int ident;
		/// <summary>
		/// Type of user
		/// </summary>
        public UserInfo Info;
		/// <summary>
		/// Full name (in case user is a prof.
		/// </summary>
        public string FIO = "";
		/// <summary>
		/// User's Telegram ID
		/// </summary>
        public long id = 0;
		/// <summary>
		/// User's course
		/// </summary>
        public int course = 0;
		/// <summary>
		/// User's group
		/// </summary>
        public int group = 0;

        //public enum Prepods  {ФИО};

		//TODO: Change to latin abbr.
		/// <summary>
		/// Possible types of users
		/// </summary>
        public enum UserInfo { препод, бак, маг, асп };
    }
}
