using System;
using GTA.Math;
using GTA.Native;

namespace ModdedCamera
{
    /// <summary>
    /// Centralized camera rendering utilities.
    /// Eliminates duplicated rendering code between SplineCamera and PositionSelector.
    /// </summary>
    public static class CameraRenderer
    {
        /// <summary>
        /// Set focus area to camera position for proper rendering.
        /// Call this periodically when camera is active.
        /// </summary>
        public static void UpdateFocusArea(Vector3 position)
        {
            try
            {
                Function.Call(NativeHashes.SET_FOCUS_AREA, new InputArgument[]
                {
                    position.X,
                    position.Y,
                    position.Z
                });
            }
            catch (Exception ex)
            {
                Logger.Debug("SET_FOCUS_AREA native warning: " + ex.Message);
            }
        }

        /// <summary>
        /// Draw render scene natives (camera shake and lighting).
        /// </summary>
        public static void DrawRenderScene()
        {
            try
            {
                Function.Call(NativeHashes.SET_CAM_SHAKE, new InputArgument[0]);
            }
            catch (Exception ex)
            {
                Logger.Debug("RenderScene native #1 (SET_CAM_SHAKE) warning: " + ex.Message);
            }

            try
            {
                Function.Call(NativeHashes.DRAW_LIGHT_WITH_RANGE, new InputArgument[] { 18 });
            }
            catch (Exception ex)
            {
                Logger.Debug("RenderScene native #2 (DRAW_LIGHT_WITH_RANGE) warning: " + ex.Message);
            }
        }

        /// <summary>
        /// Draw a marker sprite showing camera position and movement direction.
        /// Used in PositionSelector to visualize camera placement.
        /// </summary>
        public static void DrawPositionMarker(Vector3 cameraPos, Vector3 previousPos)
        {
            try
            {
                Vector3 direction = Vector3.Subtract(cameraPos, previousPos);
                Function.Call(NativeHashes.DRAW_MARKER_SPRITE, new InputArgument[]
                {
                    cameraPos.X,
                    cameraPos.Y,
                    cameraPos.Z,
                    direction.X,
                    direction.Y,
                    direction.Z
                });
            }
            catch (Exception ex)
            {
                Logger.Debug("Draw marker warning: " + ex.Message);
            }
        }
    }
}
