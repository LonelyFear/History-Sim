using Godot;
using System;

public partial class PlayerCamera : Camera2D
{
    [Export] float cameraSpeed = 500f;
    [Export] bool controlEnabled = false;
    [Export] float zoomSpeed = 10;
    Vector2 zoomTarget;
    [Export] float maxZoom = 1;
    [Export] float minZoom = 6;

    bool dragging;
    Vector2 draggingStartMousePos;
    Vector2 draggingStartPos;
    public override void _Process(double delta)
    {
        if (controlEnabled)
        {
            ZoomCamera(delta);
            SimplePan(delta);
            MousePan();
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

        Vector2 zoomDir = GetGlobalMousePosition() - Position;
        Position += zoomDir - (zoomDir / (Zoom.X / oldZoom));
    }

    void MousePan()
    {
        if (!dragging && Input.IsActionJustPressed("Camera_Pan"))
        {
            dragging = true;
            draggingStartMousePos = GetViewport().GetMousePosition();
            draggingStartPos = Position;
        }
        if (dragging && Input.IsActionJustReleased("Camera_Pan"))
        {
            dragging = false;
        }
        if (dragging)
        {
            Vector2 moveVector = GetViewport().GetMousePosition() - draggingStartMousePos;
            Position = draggingStartPos - moveVector * (1 / Zoom.X);
        }
    }
    void SimplePan(double delta)
    {
        Vector2 moveVector = new Vector2(Input.GetAxis("Move_Left", "Move_Right"), Input.GetAxis("Move_Up", "Move_Down"));
        Position += moveVector * cameraSpeed * (float)delta * (1 / Zoom.X);
    }
}
