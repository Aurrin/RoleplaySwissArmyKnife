using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace RoleplaySwissArmyKnife.Models
{
    public class ControlChannel
    {
        [Key]
        public ulong ControlChannelID { get; set; }

        public ulong ResultChannelID  { get; set; } = 0;
    }
}
