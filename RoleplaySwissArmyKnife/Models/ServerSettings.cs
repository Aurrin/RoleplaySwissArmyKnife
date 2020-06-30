using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RoleplaySwissArmyKnife.Models
{
    public class ServerSettings
    {
        [Key]
        public ulong  ServerID       { get; set; }

        public string CommandPrefix  { get; set; } = "";
    }
}
