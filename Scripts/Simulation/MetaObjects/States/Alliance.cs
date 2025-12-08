using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MessagePack;

[MessagePackObject]
// Alliances
// Versatile, can represent unions and realms. Use this class for anything involving collections of states
public class Alliance : NamedObject
{

    [Key(0)] public AllianceType type;
    [Key(2)] public ulong? leadStateId;
    [Key(1)] public List<ulong> memberStateIds = new List<ulong>();
    //[IgnoreMember] List<Region> regions = new List<Region>();
    public void AddMember(ulong memberId)
    {
        State newMember = objectManager.GetState(memberId);
        if (newMember == null || newMember.allianceIds.Contains(id))
        {
            return;
        }
        newMember.allianceIds.Add(id);
        memberStateIds.Add(memberId);

        // Realm Stuff
        if (type == AllianceType.REALM)
        {
            newMember.realmId = id;
        }
    }
    public void SetLeader(ulong? leaderId)
    {
        if (memberStateIds.Contains((ulong)leaderId))
        {
            leadStateId = leaderId;
        }
    }
    public void RemoveMember(ulong memberId)
    {
        State member = objectManager.GetState(memberId);
        if (!member.allianceIds.Contains(id))
        {
            return;
        }
        member.allianceIds.Remove(id);
        memberStateIds.Remove(memberId);  

        // Realm Stuff
        if (type == AllianceType.REALM)
        {
            member.realmId = null;
        }      
    }
    public HashSet<Region> GetRegions()
    {
        HashSet<Region> regions = new HashSet<Region>();
        foreach (ulong stateId in memberStateIds)
        {
            State memberState = objectManager.GetState(stateId);
            lock (memberState.regions)
            {
                foreach (Region region in memberState.regions)
                {
                    regions.Add(region);
                }                
            }
        }
        return regions;
    }
    public long GetAllianceManpower()
    {
        long mp = 0;
        foreach (ulong stateId in memberStateIds)
        {
            State memberState = objectManager.GetState(stateId);
            mp += memberState.manpower;
        }
        return mp;
    }
}
public enum AllianceType
{
    REALM,
    UNION,
    PERSONAL_UNION,
    CONFEDERATION,
    ALLIANCE,
    DEFENSIVE_PACT,
    COALITION
}