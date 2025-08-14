using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;


public interface IJwtEmailService
{
    string GenerateEmailConfirmationToken(string userId, string email);
    ClaimsPrincipal ValidateEmailConfirmationToken(string token);
}
