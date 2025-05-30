using Godot;
using System;

public abstract class BaseResource
{
    public string name {get; set;} = "Base Resource";
    public string id {get; set;} = "baseresource";
    public float baseWorth { get; set; } = 1;
}
