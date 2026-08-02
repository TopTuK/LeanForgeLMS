using System;
using System.Collections.Generic;
using System.Text;

namespace LF.AppDomain.Models.User.Enums
{
    public enum UserRole : byte
    {
        None = 0,
        Student = 1,
        Instructor = 2,
        CourseCreator = 3,
        Admin = 4
    }
}
