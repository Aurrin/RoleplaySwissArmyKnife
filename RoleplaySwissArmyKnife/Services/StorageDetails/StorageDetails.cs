using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RoleplaySwissArmyKnife.Models;

namespace RoleplaySwissArmyKnife.Services.StorageDetails
{
    public class StorageSQLiteContext : DbContext
    {
        public DbSet<InitiativeState> InitiativeStates  { get; set; }
        public DbSet<InitiativeEntry> InitiativeEntries { get; set; }
        public DbSet<ControlChannel>  ControlChannels   { get; set; }
        public DbSet<ServerSettings>  ServerSettings    { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=rpsak.db");
    }
}
