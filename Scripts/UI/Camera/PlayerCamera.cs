using Godot;
using System;

public partial class PlayerCamera : Camera2D
{
    [Export] public bool controlEnabled = false;
    [Export] float cameraSpeed = 500f;
    [Export] float zoomSpeed = 10;
    Vector2 zoomTarget;
    [Export] float maxZoom = 1f;
    [Export] float minZoom = 6f;

    bool dragging;
    Vector2 draggingStartMousePos;
    Vector2 draggingStartPos;
    public Vector2 mousePos;
    public Vector2 CameraPos = new(640, 360);
    public float HorizontalFactor {get
        {
            return (CameraPos.X / GetViewportRect().Size.X) + 0.5f;
        }
    }
    public override void _Process(double delta)
    {
		float mx = GetGlobalMousePosition().X + (CameraPos.X - (GetViewportRect().Size.X/2f));
		mousePos = new Vector2(Mathf.PosMod(mx, GetViewportRect().Size.X), GetGlobalMousePosition().Y);

        CameraPos = new Vector2(Mathf.PosMod(CameraPos.X, GetViewportRect().Size.X), CameraPos.Y);
        if (controlEnabled)
        {
            ZoomCamera(delta);
            SimplePan(delta);
            MousePan();
            Position = new Vector2(640, CameraPos.Y);
        }
    }
    public override void _UnhandledInput(InputEvent evnt)
    {
        if (evnt.IsAction("Zoom_In"))
        {
            zoomTarget *= 1.1f;
        }
        else if (evnt.IsAction("Zoom_Out"))
        {
            zoomTarget *= 0.9f;
        }
        zoomTarget.X = Mathf.Clamp(zoomTarget.X, maxZoom, minZoom);
        zoomTarget.Y = Mathf.Clamp(zoomTarget.Y, maxZoom, minZoom);        
    }

    void ZoomCamera(double delta)
    {
        float oldZoom = Zoom.X;
        Zoom = Zoom.Slerp(zoomTarget, (float)(zoomSpeed * delta));

        Vector2 zoomDir = mousePos - CameraPos;
        CameraPos += zoomDir - (zoomDir / (Zoom.X / oldZoom));
    }

    void MousePan()
    {
        if (!dragging && Input.IsActionJustPressed("Camera_Pan"))
        {
            dragging = true;
            draggingStartMousePos = GetViewport().GetMousePosition();
            draggingStartPos = CameraPos;
        }
        if (dragging && Input.IsActionJustReleased("Camera_Pan"))
        {
            dragging = false;
        }
        if (dragging)
        {
            Vector2 moveVector = GetViewport().GetMousePosition() - draggingStartMousePos;
            CameraPos = draggingStartPos - moveVector * (1 / Zoom.X);
        }
    }
    void SimplePan(double delta)
    {
        Vector2 moveVector = new Vector2(Input.GetAxis("Move_Left", "Move_Right"), Input.GetAxis("Move_Up", "Move_Down"));
        CameraPos += moveVector * cameraSpeed * (float)delta * (1 / Zoom.X);
    }
}
