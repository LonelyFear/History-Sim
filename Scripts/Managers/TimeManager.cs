using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Godot;

public partial class TimeManager : Node
{
    
    [Signal]
    public delegate void TickEventHandler();
    [Signal]
    public delegate void YearEventHandler();
    string[] months = { "January", "Febuary", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

    public const uint daysPerTick = 14;
    public const uint ticksPerMonth = 28;
    public const uint ticksPerYear = ticksPerMonth*12;
    uint monthCounter = 0;
    uint yearCounter = 0;
    public uint ticks = 0;
    public ulong tickStartTime = 0;
    public double tickDelta = 1;
    public ulong monthStartTime = 0;
    public double monthDelta = 1;
    public SimManager simManager;
    public MapManager mapManager;
    [Export] BorderRenderer borderRenderer;
    bool simStart = false;
    Task tickTask;
    Task monthTask;
    Task yearTask;
    bool doYear = true;
    bool doMonth = true;
    bool didMonth = false;
    [Export]
    public bool debuggerMode = false;
    double waitTime;
    double currentTime;
    [Export] public GameSpeed gameSpeed = GameSpeed.ONE_YEAR_PER_SECOND;
    OptionButton gameSpeedUI;
    public bool forcePause = false;
    public override void _Ready()
    {
        mapManager = GetNode<MapManager>("/root/Game/Map Manager");
        gameSpeedUI = GetNode<OptionButton>("/root/Game/UI/Action Panel/HBoxContainer/TimeSpeedHolder/TimeSpeed");
        // Connection
		GetNode<SimNodeManager>("/root/Game/Simulation").simStartEvent += OnSimStart;
	}

    public void OnSimStart()
    {
        
        simStart = true;
        simManager = GetNode<SimNodeManager>("/root/Game/Simulation").simManager;
        TickGame();
	}

    public override void _Process(double delta)
    {
        gameSpeed = (GameSpeed)gameSpeedUI.Selected;
        if (forcePause)
        {
            return;
        }
        GetWaitTime();
        currentTime += delta;
        if (currentTime >= waitTime)
        {
            currentTime = 0;
            bool tickDone = tickTask == null || tickTask.IsCompleted;
            bool yearDone = yearTask == null || yearTask.IsCompleted;
            bool monthDone = monthTask == null || monthTask.IsCompleted;
            if (simStart && tickDone)
            {
                if (doMonth && !debuggerMode)
                {
                    doMonth = false;
                    monthTask = Task.Run(simManager.SimMonth);
                }
                if (monthDone)
                {
                    if (monthTask == null || monthTask.IsCompleted)
                    {
                        didMonth = true;
                    }
                    if (doYear && !debuggerMode)
                    {
                        doYear = false;
                        yearTask = Task.Run(simManager.SimYear);
                    }
                    if (yearDone)
                    {
                        tickDelta = (Time.GetTicksMsec() - (double)tickStartTime) / 1000d;
                        if (didMonth)
                        {
                            didMonth = false;
                            monthDelta = (Time.GetTicksMsec() - (double)monthStartTime) / 1000d;
                        }
                        TickGame();
                        mapManager.UpdateRegionColors(simManager.regionIds.Values);
                    }

                }
            }

        }
    }

    void GetWaitTime()
    {
        double monthTime = (double)ticksPerMonth / daysPerTick;
        switch (gameSpeed)
        {
            case GameSpeed.ONE_WEEK_PER_SECOND:
                waitTime = float.MaxValue;
                break;
            case GameSpeed.ONE_MONTH_PER_SECOND:
                waitTime = 1d / monthTime;
                break;
            case GameSpeed.SIX_MONTHS_PER_SECOND:
                waitTime = 1d / (6 * monthTime);
                break;
            case GameSpeed.ONE_YEAR_PER_SECOND:
                waitTime = 1d / (12 * monthTime);
                break;
            case GameSpeed.FIVE_YEARS_PER_SECOND:
                waitTime = 1d / (60 * monthTime);
                break;
            case GameSpeed.ONE_DECADE_PER_SECOND:
                waitTime = 1d / (120 * monthTime);
                break;
            case GameSpeed.UNLIMITED:
                waitTime = 0;
                break;
        }        
    }

    private void TickGame(){
        tickStartTime = Time.GetTicksMsec();   

        ticks += daysPerTick;
        monthCounter += daysPerTick;
        yearCounter += daysPerTick; 
        if (monthCounter >= ticksPerMonth)
        {
            monthCounter = 0;
            monthStartTime = Time.GetTicksMsec(); 

            if (!debuggerMode)
            {
                doMonth = true;
            }
            else
            {
                simManager.SimMonth();
            }
        }
        
        if (yearCounter >= ticksPerYear){
            yearCounter = 0;
            if (!debuggerMode)
            {
                doYear = true;
            }
            else
            {
                simManager.SimYear();
            }
        }
    }

    public enum GameSpeed{
        ONE_WEEK_PER_SECOND,
        ONE_MONTH_PER_SECOND,
        SIX_MONTHS_PER_SECOND,
        ONE_YEAR_PER_SECOND,
        FIVE_YEARS_PER_SECOND,
        ONE_DECADE_PER_SECOND,
        UNLIMITED
    }
    public uint GetDay(uint tick = 0)
    {
        if (tick == 0)
        {
            tick = ticks;
        }
        return (uint)Mathf.PosMod(tick, ticksPerMonth);
    }
    public uint GetMonth(uint tick = 0){
        if (tick == 0){
            tick = ticks;
        }
        return (uint)Mathf.PosMod(tick/(float)ticksPerMonth, 12) + 1;
    }
    public uint GetYear(uint tick = 0)
    {
        if (tick == 0)
        {
            tick = ticks;
        }
        return tick / ticksPerYear;
    }
    public static uint YearsToTicks(int years)
    {
        return (uint)(years * ticksPerYear);
    }
    public string GetStringDate(uint tick = 0, bool useMonth = false){
        if (tick == 0)
        {
            tick = ticks;
        }
        string date = "";
        if (useMonth)
        {
            string month = months[GetMonth(tick) - 1];
            string year = GetYear(tick).ToString();
            date = $"{month} of {year}";   
        } else
        {
            string day = GetDay(tick).ToString("00");
            string month = GetMonth(tick).ToString("00");
            string year = GetYear(tick).ToString("0000");
            date = $"{month}/{day}/{year}";            
        }

        
        return date;
    }
}
