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
        /// Id of teacher
        /// </summary>
        public int teacherId = 0;
        /// <summary>
        /// Id of user`s group
        /// </summary>
        public int groupid = 0;
        /// <summary>
        /// User's Telegram ID
        /// </summary>
        public long id = 0;
        /// <summary>
        /// The evening notify flag.
        /// </summary>
        public bool eveningNotify = false;
        /// <summary>
        /// The pre lesson notify flag.
        /// </summary>
        public bool preLessonNotify = false;

        /// <summary>
        /// Possible types of users
        /// </summary>
        public enum UserInfo { teacher, bachelor, master, graduate };
    }
}
