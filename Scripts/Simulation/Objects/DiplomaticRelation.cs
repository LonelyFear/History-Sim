using Godot;
using MessagePack;
using PixelHistory.Objects.States.Base;

public class DiplomaticRelations{
    [Key(0)] public ulong id;
    [Key(1)] public float opinion = 0f;
    [Key(2)] public uint truce = 0;
    [Key(3)] public bool rival = false;  
    [Key(4)] public ulong initiatorId;
    [Key(5)] public ulong recipientId;
    [IgnoreMember] public State initiator;
    [IgnoreMember] public State recipient;

    public void ChangeOpinion(float value)
    {
        opinion = Mathf.Clamp(opinion + value, -1, 1);
    }
}