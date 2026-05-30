#if UNITY_EDITOR || BL_DEBUG
using UnityEngine;

public static class SteeringDebugColors
{
    public static readonly Color Intent = new(0.2f, 1.0f, 0.2f, 0.9f);
    public static readonly Color Field = new(1.0f, 0.85f, 0.0f, 0.9f);
    public static readonly Color Threat = new(1.0f, 0.15f, 0.15f, 0.9f);
    public static readonly Color Objective = new(0.2f, 0.5f, 1.0f, 0.9f);
    public static readonly Color AllyPressure = new(0.3f, 0.8f, 1.0f, 0.9f);
    public static readonly Color Hazard = new(1.0f, 0.5f, 0.0f, 0.9f);
    public static readonly Color Lure = new(0.8f, 0.2f, 1.0f, 0.9f);
}
#endif