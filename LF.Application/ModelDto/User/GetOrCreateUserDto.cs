using System;
using System.Collections.Generic;
using System.Text;

namespace LF.Application.ModelDto.User
{
    public sealed class GetOrCreateUserDto
    {
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; } = null;
    }
}
