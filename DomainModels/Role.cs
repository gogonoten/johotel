using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainModels;
public class Role : Common
{
    public required string Name { get; set; }

    public List<User> Users { get; set; } = new();
}
