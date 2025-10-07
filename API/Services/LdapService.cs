using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.DirectoryServices.Protocols;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace API.Services
{
    // LDAP authentikation og tjekker rolle
    public sealed class LdapService : ILdapService
    {
        private readonly LdapOptions _opt;
        public LdapService(IOptions<LdapOptions> opt) => _opt = opt.Value;

        public Task<(bool ok, string? userName, List<string> roles, string? error)>
            AuthenticateAsync(string username, string password)
        {
            try
            {
                var id = new LdapDirectoryIdentifier(_opt.Server, _opt.Port, false, false); 

                // Bind som servicekonto, svc_webapp@johotel.local
                using var conn = new LdapConnection(id);
                conn.SessionOptions.SecureSocketLayer = _opt.UseSsl; 
                conn.SessionOptions.ProtocolVersion = 3;

                if (!string.IsNullOrWhiteSpace(_opt.BindUser))
                {
                    var cred = MakeCredential(_opt.BindUser!, _opt.BindPassword, _opt.Domain);
                    conn.Bind(cred); 
                }

                // Find brugeren - DN Distungished name via UPN eller sAMAccountName
                var upn = MakeUpn(username, _opt.Domain);
                var sam = ExtractSam(username);
                var findFilter = $"(|(userPrincipalName={Escape(upn)})(sAMAccountName={Escape(sam)}))";
                var findReq = new SearchRequest(
                    _opt.BaseDn,
                    findFilter,
                    SearchScope.Subtree,
                    new[] { "distinguishedName", "sAMAccountName" }
                );

                var findResp = (SearchResponse)conn.SendRequest(findReq);
                if (findResp.Entries.Count == 0)
                    return Task.FromResult((false, (string?)null, new List<string>(), "User not found"));

                var userEntry = findResp.Entries[0];
                var userDn = userEntry.DistinguishedName;
                var userSam = userEntry.Attributes["sAMAccountName"]?[0]?.ToString() ?? sam;

                // Tjekker password ved at binde som brugeren
                using (var userConn = new LdapConnection(id))
                {
                    userConn.SessionOptions.SecureSocketLayer = _opt.UseSsl;
                    userConn.SessionOptions.ProtocolVersion = 3;
                    userConn.Bind(new NetworkCredential(upn, password));
                }

                // Mapper AD-grupper til App-roller 
                var roles = new List<string>();
                foreach (var kv in _opt.GroupMap)
                {
                    var groupDn = kv.Key;     
                    var appRole = kv.Value;   

                    var roleFilter =
                        $"(&(distinguishedName={Escape(userDn)})" +
                        $"(memberOf:1.2.840.113556.1.4.1941:={Escape(groupDn)}))";

                    var roleReq = new SearchRequest(
                        _opt.BaseDn,
                        roleFilter,
                        SearchScope.Subtree,
                        new[] { "distinguishedName" }
                    );

                    var roleResp = (SearchResponse)conn.SendRequest(roleReq);
                    if (roleResp.Entries.Count > 0) roles.Add(appRole);
                }

                return Task.FromResult((
                    true,
                    userSam,
                    roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    (string?)null
                ));
            }
            catch (LdapException ex)
            {
                var msg = string.IsNullOrWhiteSpace(ex.ServerErrorMessage) ? ex.Message : ex.ServerErrorMessage;
                return Task.FromResult((false, (string?)null, new List<string>(), msg));
            }
            catch (Exception ex)
            {
                return Task.FromResult((false, (string?)null, new List<string>(), ex.Message));
            }
        }

        // HELPERS

        // Bygger NetworkCredential fra UPN eller domainuser
        private static NetworkCredential MakeCredential(string user, string? password, string domainFqdn)
        {
            if (user.Contains("@")) return new NetworkCredential(user, password);
            if (user.Contains("\\"))
            {
                var parts = user.Split('\\', 2);
                var dom = parts.Length == 2 ? parts[0] : domainFqdn.Split('.')[0];
                var usr = parts.Length == 2 ? parts[1] : user;
                return new NetworkCredential(usr, password, dom);
            }
            var netbios = domainFqdn.Split('.')[0]; 
            return new NetworkCredential(user, password, netbios);
        }

        // UPN fra input (bevarer UPN hvis allerede givet)
        private static string MakeUpn(string input, string domainFqdn)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            if (input.Contains("@")) return input;
            if (input.Contains("\\"))
            {
                var parts = input.Split('\\', 2);
                var user = parts.Length == 2 ? parts[1] : input;
                return $"{user}@{domainFqdn}";
            }
            return $"{input}@{domainFqdn}";
        }

        // SamAccountName fra input
        private static string ExtractSam(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            if (input.Contains("\\")) { var p = input.Split('\\', 2); return p.Length == 2 ? p[1] : input; }
            if (input.Contains("@")) { var p = input.Split('@', 2); return p[0]; }
            return input;
        }

        private static string Escape(string val)
        {
            if (string.IsNullOrEmpty(val)) return val;
            return val
                .Replace("\\", "\\5c")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("*", "\\2a")
                .Replace("\0", "");
        }
    }
}
