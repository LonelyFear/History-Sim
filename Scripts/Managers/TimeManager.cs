using System.Threading.Tasks;
using Godot;

public partial class TimeManager : Node
{
    
    [Signal]
    public delegate void TickEventHandler();
    [Signal]
    public delegate void YearEventHandler();
    static readonly string[] months = ["January", "Febuary", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

    public const uint MonthsPerTick = 1;
    public const uint TicksPerYear = 12/MonthsPerTick;
    public uint ticks = 0;
    public ulong monthStartTime = 0;
    public double tickDelta = 1;
    public SimManager simManager;
    public MapManager mapManager;
    [Export] SimManagerHolder simHolder;
    bool simStart = false;
    //Task tickTask;
    Task tickTask = Task.CompletedTask;
    Task drawTask = Task.CompletedTask;
    double currentTime;
    public GameSpeed gameSpeed = GameSpeed.UNLIMITED;
    GameSpeed lastSpeed = GameSpeed.UNLIMITED;
    [Export] OptionButton gameSpeedUI;
    public bool forcePause = false;

    public override void _Ready()
    {
        mapManager = GetNode<MapManager>("/root/Game/Map Manager");
        // Connection
		simHolder.simStartEvent += OnSimStart;
	}

    public void OnSimStart()
    {
        simStart = true;
        simManager = simHolder.simManager;
	}
    public void ForceGameSpeed(GameSpeed desiredSpeed)
    {
        lastSpeed = gameSpeed;
        gameSpeed = desiredSpeed;
        gameSpeedUI.Selected = (int)desiredSpeed;
        UpdateGameSpeed();
    }
    void UpdateGameSpeed()
    {
        gameSpeed = (GameSpeed)gameSpeedUI.Selected;
    }
    public override void _Process(double delta)
    {
        if (!mapManager.initialized) return;

        if (Input.IsActionJustPressed("Pause"))
        {
            if (gameSpeed == GameSpeed.PAUSED)
            {
                ForceGameSpeed(lastSpeed);
            } else
            {
                ForceGameSpeed(GameSpeed.PAUSED);
            }
        }

        UpdateGameSpeed();
        currentTime += delta;

        if (forcePause)
        {
            return;
        }
        
        if (simStart && tickTask.IsCompleted)
        {
            tickTask = UpdateTimeManager();
        } 
        // TODO: Move this function to a more fitting place
        if (drawTask.IsCompleted) {
            drawTask = RenderGame();
        }
    }
    async Task UpdateTimeManager()
    {
        if (currentTime <= GetWaitTime()) return;
        currentTime = 0;
        
        double tickStartTime = Time.GetTicksMsec(); 
        await Task.Run(simManager.SimMonth);

        if (Mathf.PosMod(ticks, TicksPerYear) == 0){
            await Task.Run(simManager.SimYear);
        }

        tickDelta = (Time.GetTicksMsec() - tickStartTime) / 1000d;
        ticks ++;
    }
    async Task RenderGame()
    {
        // Hides region overlay 
        if (!mapManager.showRegionsCheckbox.ButtonPressed)
        {
            mapManager.regionOverlay.Visible = false;
        }
        if (drawTask.IsCompleted && mapManager.showRegionsCheckbox.ButtonPressed)
        {
            // Shows region overlay
            mapManager.regionOverlay.Visible = true;
            // Gets our buffers
            await Task.Run(() => mapManager.UpdateRegionColors(simManager.regionIds.Values));  
            // Runs the shader 
            mapManager.RunShader();
        }     
    }
    float GetWaitTime()
    {
        return gameSpeed switch
        {
            GameSpeed.PAUSED => float.MaxValue,
            GameSpeed.ONE_MONTH_PER_SECOND => 1f / MonthsPerTick,
            GameSpeed.SIX_MONTHS_PER_SECOND => 1f / (6/MonthsPerTick),
            GameSpeed.ONE_YEAR_PER_SECOND => 1f / (12/MonthsPerTick),
            GameSpeed.FIVE_YEARS_PER_SECOND => 1f / (60/MonthsPerTick),
            GameSpeed.ONE_DECADE_PER_SECOND => 1f / (120/MonthsPerTick),
            _ => 0,
        };

    }

    public enum GameSpeed{
        PAUSED,
        ONE_MONTH_PER_SECOND,
        SIX_MONTHS_PER_SECOND,
        ONE_YEAR_PER_SECOND,
        FIVE_YEARS_PER_SECOND,
        ONE_DECADE_PER_SECOND,
        UNLIMITED
    }
    public static uint GetMonth(uint tick){
        return (uint)Mathf.PosMod(tick * MonthsPerTick, 12) + 1;
    }
    public static uint GetYear(uint tick)
    {
        return tick / TicksPerYear;
    }
    public static uint YearsToTicks(int years)
    {
        return (uint)(years * TicksPerYear);
    }
    public static float TicksToYears(uint ticks)
    {
        return ticks / (float)TicksPerYear;
    }
    public string GetStringMonth(uint tick = 0)
    {
        if (tick == 0)
        {
            tick = ticks;
        }
        return months[GetMonth(tick) - 1];
    }
    public static string GetStringDate(uint tick){
        string date = "";
        
        string month = months[GetMonth(tick) - 1];
        string year = GetYear(tick).ToString();
        date = $"{month} of {year}";   

        return date;
    }
}
