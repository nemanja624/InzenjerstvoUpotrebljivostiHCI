using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mercedes_Models_CMS.Models
{
    public enum UserRole { ADMIN,VISITOR};
    [Serializable]
    public class User
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public UserRole Role { get; set; }

        public User(string userName, string password,UserRole role)
        {
            UserName = userName;
            Password = password;
            Role = role;
        }
        public User()
            { }
    }
}
