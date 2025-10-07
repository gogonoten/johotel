using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Services
{
    public interface ILdapService
    {
        Task<(bool ok, string? userName, List<string> roles, string? error)>
            AuthenticateAsync(string username, string password);
    }
}
