using System;
using System.Collections.Generic;
using System.Text;

namespace LF.Application.ModelDto.Authentication
{
    public sealed class UserAuthentificationDto
    {
        public string? Sub {  get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
