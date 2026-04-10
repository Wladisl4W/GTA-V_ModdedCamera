namespace ModdedCamera
{
    /// <summary>
    /// Interpolation modes for camera path smoothing.
    /// </summary>
    public enum InterpolationMode
    {
        /// <summary>
        /// Linear interpolation - straight lines between waypoints.
        /// </summary>
        Linear = 0,

        /// <summary>
        /// Catmull-Rom spline interpolation - smooth curves through waypoints.
        /// </summary>
        Smooth = 2
    }
}
