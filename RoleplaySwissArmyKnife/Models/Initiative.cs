using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace RoleplaySwissArmyKnife.Models
{

    public class InitiativeState
    {
        [Key]
        public ulong  InitiativeStateID     { get; set; }

        public ulong  ChannelID             { get; set; } = 0;

        public ulong  PinnedListMessageID   { get; set; } = 0;

        public ulong  LastAnnounceMessageID { get; set; } = 0;

        public double CurrentInitiative     { get; set; } = 0.0;

        public List<InitiativeEntry> Characters { get; set; } = new List<InitiativeEntry>();

        public bool   InInitiative          { get; set; }    = false;

        public void Sort()
        {
            Characters = Characters.OrderByDescending(x => x.Initiative).ToList();
        }

        public InitiativeEntry GetCurrent()
        {
            Sort();
            return Characters.Find(x => x.Initiative <= CurrentInitiative) ??
                (Characters.Count > 0 ? Characters[0] : null);
        }

        public InitiativeEntry Advance()
        {
            var retval = Characters.Find(x => x.Initiative < CurrentInitiative) ??
                (Characters.Count > 0 ? Characters[0] : null);
            CurrentInitiative = retval?.Initiative ?? 0;
            return retval;
        }
    }

    public class InitiativeEntry
    {
        [Key]
        public ulong           InitiativeEntryID { get; set; }

        public ulong           PlayerID          { get; set; } = 0;

        public string          DisplayName       { get; set; } = "";

        public double          Initiative        { get; set; } = 0.0;


        public ulong           InitiativeStateID { get; set; }

        public InitiativeState InitiativeState   { get; set; }
    }
}
