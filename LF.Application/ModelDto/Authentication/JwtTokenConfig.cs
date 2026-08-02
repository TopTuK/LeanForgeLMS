using System;
using System.Collections.Generic;
using System.Text;

namespace LF.Application.ModelDto.Authentication
{
    public sealed class JwtTokenConfigDto
    {
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public string Key { get; set; } = null!;
        public int ExpiresDays { get; set; } = 0;
    }
}
