using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.NetworkInformation;
using Godot;
using MessagePack;
[MessagePackObject(keyAsPropertyName: true)]
public class Character
{
    public ulong id;
    public string name;
}

public enum TraitLevel
{
    VERY_LOW,
    LOW,
    MEDIUM,
    HIGH,
    VERY_HIGH,
}


