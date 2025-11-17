using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessagePack;

[MessagePackObject(AllowPrivate = true)]
public partial class StateVassalManager
{

    [IgnoreMember] public static ObjectManager objectManager;
    [IgnoreMember] State state;
    [Key(0)] ulong stateId;
    [Key(1)] public ulong? liegeId = null;
    [Key(17)] public List<ulong?> vassalIds { get; set; } = new List<ulong?>();
    [Key(21)] public Sovereignty sovereignty = Sovereignty.INDEPENDENT;
    public StateVassalManager() { }
    public StateVassalManager(State selectedState)
    {
        selectedState.vassalManager = this;
        stateId = selectedState.id;
        state = selectedState;
    }
    public void Init(State state)
    {
        this.state = state;
    }
    public void UpdateRealm()
    {
        Alliance realm = objectManager.GetAlliance(state.realmId);
        if (realm == null && vassalIds.Count > 0)
        {
            // Creates a realm
            state.realmId = objectManager.CreateAlliance(state, AllianceType.REALM).id;
            
            realm = objectManager.GetAlliance(state.realmId);
            foreach (ulong vassalId in vassalIds)
            {
                realm.AddMember(vassalId);
            }
        }
    }
    public void AddVassal(ulong vassalId)
    {
        // Realm stuff
        if (vassalIds.Count < 1 && state.realmId == null)
        {
            state.realmId = objectManager.CreateAlliance(state, AllianceType.REALM).id;
        }
        Alliance realm = objectManager.GetAlliance(state.realmId);

        State newVassal = objectManager.GetState(vassalId);
        //Relation usToThem = state.diplomacy.GetRelations(vassalId);
        //Relation themToUs = newVassal.diplomacy.GetRelations(stateId);

        // Removes vassal from old liege
        if (newVassal.vassalManager.liegeId != null)
        {
            objectManager.GetState(newVassal.vassalManager.liegeId).vassalManager.RemoveVassal(vassalId);
        }

        // Adds vassal to us
        vassalIds.Add(vassalId);
        realm.AddMember(vassalId);
        newVassal.sovereignty = Sovereignty.PUPPET;
    }
    public void RemoveVassal(ulong vassalId)
    {
        // Gets Realm
        Alliance realm = objectManager.GetAlliance(state.realmId);

        // Gets Object
        State vassal = objectManager.GetState(vassalId);
        StateVassalManager vassalManager = vassal.vassalManager;

        vassalManager.liegeId = null;
        vassalManager.sovereignty = Sovereignty.INDEPENDENT;

        // Removes Associations
        vassalIds.Remove(vassalId);
        realm.RemoveMember(vassalId);
        foreach (ulong subVassalId in vassalManager.vassalIds)
        {
            realm.RemoveMember(subVassalId);
        }
    }

    // Utility
    public State GetOverlord(bool includeSelf)
    {
        Alliance realm = objectManager.GetAlliance(state.realmId);
        if (realm != null)
        {
            return objectManager.GetState(realm.leadStateId);         
        }
        // Otherwise returns the liege as overlord
        return GetLiege(includeSelf);
    }
    public State GetLiege(bool includeSelf = false)
    {
        State liege = objectManager.GetState(liegeId);
        if (includeSelf)
        {
            return liege != null ? /*T*/ liege : /*F*/ state;
        }
        return liege;
    }
    public List<State> GetVassals()
    {
        List<State> vassals = new List<State>();
        foreach (ulong vassalId in vassalIds)
        {
            // Iterates over vassals and adds them to list
            vassals.Add(objectManager.GetState(vassalId));
        }
        return vassals;
    }
}
public enum Sovereignty
{
    INDEPENDENT = 4,
    REBELLIOUS = 3,
    PUPPET = 2,
    COLONY = 1,
    PROVINCE = 0
}