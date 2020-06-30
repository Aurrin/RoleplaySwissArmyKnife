using Microsoft.EntityFrameworkCore;
using RoleplaySwissArmyKnife.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace RoleplaySwissArmyKnife.Services
{
    public class StorageService : IDisposable
    {
        private Dictionary<ulong, ulong>           resultChannels;
        private Dictionary<ulong, InitiativeState> initiativeStates;
        private Dictionary<ulong, string>          prefixes;

        private StorageDetails.StorageSQLiteContext db;

        private enum StorageType
        {
            Memory,
            Database
        }
        private StorageType storageType;
        
        private bool disposedValue;

        public StorageService()
        {
            resultChannels   = new Dictionary<ulong, ulong>();
            initiativeStates = new Dictionary<ulong, InitiativeState>();
            prefixes         = new Dictionary<ulong, string>();

            db = new StorageDetails.StorageSQLiteContext();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    db.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~StorageService()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        //private void BuildTestInitative()
        //{
        //    ulong myId = 282012273596301312;
        //    var rand   = new Random();
        //    double init = 26;

        //    foreach (var c in new List<string> {
        //        "Char1",
        //        "Char2",
        //        "Char3",
        //        "Char4"
        //    })
        //    {
        //        init -= rand.Next(1, 6);
        //        initiativeState.Characters.Add( new InitiativeEntry
        //        {
        //            DisplayName = c,
        //            PlayerId    = myId,
        //            Initiative  = init,
        //        });
        //    }
        //}

        public async Task StoreInitiative(ulong channelId, InitiativeState state )
        {
            //initiativeStates[channelId] = state;
            //if ( state.InitiativeStateID == 0 )
            //{
            //    db.InitiativeStates.
            //    db.InitiativeStates.Add(state)
            //}
            //var currState = await db.InitiativeStates.FirstOrDefaultAsync(x => x.ChannelID == channelId);

            state.ChannelID = channelId;

            var oldState = await db.InitiativeStates.AsQueryable()
                .FirstOrDefaultAsync(x => x.ChannelID == channelId);

            if ( oldState == null )
            {
                await db.InitiativeStates.AddAsync(state);
            }

            foreach (var e in state.Characters)
            {
                var existing = db.InitiativeEntries.FindAsync(e.InitiativeEntryID);
                if (existing == null)
                {
                    await db.InitiativeEntries.AddAsync(e);
                }
            }

            await db.SaveChangesAsync();
        }

        public async Task<InitiativeState> GetInitiative(ulong channelId)
        {
            //return initiativeStates.ContainsKey(channelId) ?
            //    initiativeStates[channelId] : new InitiativeState() { ChannelID = channelId };

            return await db.InitiativeStates
                .Include(x => x.Characters)
                .AsAsyncEnumerable()
                .Where(x => x.ChannelID == channelId)
                .FirstOrDefaultAsync()
                ?? new InitiativeState { ChannelID = channelId };
        }

        public async Task<ulong> GetResultChannel(ulong channelId)
        {
            //return resultChannels.ContainsKey(channelId) ?
            //    resultChannels[channelId] : channelId;
            return (await db.ControlChannels.FindAsync(channelId))?.ResultChannelID ?? channelId;
        }

        public async Task SetResultChannel(ulong channelId, ulong resultChannelId)
        {
            //if (channelId == resultChannelId && resultChannels.ContainsKey(channelId))
            //    resultChannels.Remove(channelId);
            //else
            //    resultChannels[channelId] = resultChannelId;

            var cc = await db.ControlChannels.FindAsync(channelId);
            if (cc == null)
            {
                if (channelId != resultChannelId)
                {
                    await db.ControlChannels.AddAsync(new ControlChannel
                    {
                        ControlChannelID = channelId,
                        ResultChannelID  = resultChannelId
                    });
                    await db.SaveChangesAsync();
                }
            }
            else
            {
                if (channelId == resultChannelId)
                {
                    db.Remove(cc);
                    await db.SaveChangesAsync();
                }
                else
                {
                    cc.ResultChannelID = resultChannelId;
                    await db.SaveChangesAsync();
                }
            }
        }

        public async Task<string> GetServerPrefix(ulong serverId)
        {
            //return prefixes.ContainsKey(serverId) ?
            //    prefixes[serverId] : "/";

            //return (await db.ServerSettings.FindAsync(serverId))?.CommandPrefix ?? "/";

            var setting = await db.ServerSettings.FindAsync(serverId);
            return setting?.CommandPrefix ?? "/";
        }

        public async Task SetServerPrefix(ulong serverId, string prefix)
        {
            //prefixes[serverId] = prefix;

            var svr = (await db.ServerSettings.FindAsync(serverId));
            if ( svr == null )
            {
                await db.AddAsync( new ServerSettings
                {
                    ServerID      = serverId,
                    CommandPrefix = prefix
                });
            }
            else
            {
                svr.CommandPrefix = prefix;
            }
            await db.SaveChangesAsync();
        }
    }
}
