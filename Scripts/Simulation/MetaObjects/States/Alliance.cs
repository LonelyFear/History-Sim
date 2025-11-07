using System.Collections.Generic;
using MessagePack;

[MessagePackObject]
public class Alliance : NamedObject
{

    [Key(0)] public AllianceType type;
    [Key(2)] public ulong? leadStateId;
    [Key(1)] public List<ulong> memberStateIds = new List<ulong>();
    public void AddMember(ulong memberId)
    {
        State newMember = objectManager.GetState(memberId);
        if (newMember.allianceIds.Contains(id))
        {
            return;
        }
        newMember.allianceIds.Add(id);
        memberStateIds.Add(memberId);
    }
    public void SetLeader(ulong? leaderId)
    {
        leadStateId = leaderId;
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
    }
}
public enum AllianceType
{
    UNION,
    PERSONAL_UNION,
    CONFEDERATION,
    ALLIANCE,
    DEFENSIVE_PACT,
    COALITION
}