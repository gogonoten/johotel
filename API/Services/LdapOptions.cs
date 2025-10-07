using System.Collections.Generic;

namespace API.Services
{
    public sealed class LdapOptions
    {
        public string Server { get; set; } = "dc1.johotel.local";
        public int Port { get; set; } = 636;
        public bool UseSsl { get; set; } = true;

        public string Domain { get; set; } = "johotel.local";
        public string BaseDn { get; set; } = "DC=johotel,DC=local";

        public string? BindUser { get; set; }          
        public string? BindPassword { get; set; }

        public int TimeoutSeconds { get; set; } = 10;

        // Spring cert-validering over (i dev)
        public bool SkipCertValidation { get; set; } = false;

        public Dictionary<string, string> GroupMap { get; set; } = new();
    }
}
