using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace ModdedCamera.Services
{
    /// <summary>
    /// Manages all camera operations: creation, disposal, playback, and settings.
    /// Extracted from Menu.cs to separate camera logic from UI logic.
    /// </summary>
    public class CameraService
    {
        public SplineCamera SplineCamera { get; private set; }
        public PositionSelector PositionSelector { get; private set; }

        // Camera settings
        public int CurrentFov { get; set; } = 50;
        public int CurrentSpeed { get; set; } = 3;
        public int CurrentInterpolationMode { get; set; } = 0; // 0 = Linear (default), 2 = Smooth
        public bool UsePlayerView { get; set; } = false;
        // REMOVED: EndAtPlayer - not needed

        // State flags
        public bool IsSplineCamActive => SplineCamera?.MainCamera?.IsActive ?? false;
        public bool IsSelectorActive => PositionSelector?.MainCamera?.IsActive ?? false;
        public bool IsAnyCameraActive => IsSplineCamActive || IsSelectorActive;

        // Node duration for new nodes (controlled by scroll wheel in selector)
        public int NodeDuration { get; set; } = 5000;

        // Track usage for optimized rendering
        private bool _selectorWasUsed = false;
        private bool _splineCamWasUsed = false;

        /// <summary>
        /// Initialize cameras. Call once during startup.
        /// </summary>
        public void Initialize()
        {
            try
            {
                Logger.Info("CameraService: Initializing cameras...");
                SplineCamera = new SplineCamera();
                PositionSelector = new PositionSelector(Vector3.Zero, Vector3.Zero);
                ApplyCameraSettings();
                Logger.Info("CameraService: Cameras initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "CameraService: Error during initialization");
                throw;
            }
        }

        /// <summary>
        /// Apply current settings to the spline camera.
        /// </summary>
        public void ApplyCameraSettings()
        {
            try
            {
                if (SplineCamera?.MainCamera != null && SplineCamera.MainCamera.Exists())
                {
                    SplineCamera.MainCamera.FieldOfView = (float)CurrentFov;
                    SplineCamera.Speed = CurrentSpeed;
                    SplineCamera.InterpolationMode = CurrentInterpolationMode;
                    SplineCamera.UsePlayerView = UsePlayerView;
                    // REMOVED: InterpToPlayer - EndAtPlayer feature removed
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "CameraService: Error applying camera settings");
            }
        }

        /// <summary>
        /// Enter point selector mode. Freezes player and activates selector camera.
        /// </summary>
        public void EnterPointSelector()
        {
            try
            {
                if (PositionSelector == null || SplineCamera == null)
                {
                    UI.Notify("~r~Cameras not initialized!");
                    Logger.Warn("CameraService: EnterPointSelector called but cameras not initialized");
                    return;
                }

                if (IsSelectorActive || IsSplineCamActive)
                {
                    UI.Notify("Camera is Active.");
                    Logger.Warn("CameraService: EnterPointSelector rejected - camera already active");
                    return;
                }

                Logger.Info("CameraService: Entering point selector mode");
                Game.Player.Character.FreezePosition = true;
                _selectorWasUsed = true;
                PositionSelector.EnterCameraView(Game.Player.Character.GetOffsetInWorldCoords(new Vector3(0f, 0f, 10f)));
            }
            catch (Exception ex)
            {
                UI.Notify("~r~Error!");
                Logger.Error(ex, "CameraService: Error in EnterPointSelector");
            }
        }

        /// <summary>
        /// Exit point selector mode. Unfreezes player.
        /// </summary>
        public void ExitPointSelector()
        {
            try
            {
                Logger.Info("CameraService: Exiting point selector mode");
                if (PositionSelector != null)
                {
                    PositionSelector.ExitCameraView();
                }
                Game.Player.Character.FreezePosition = false;
                _selectorWasUsed = false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "CameraService: Error in ExitPointSelector");
            }
        }

        /// <summary>
        /// Add a node at current selector camera position.
        /// </summary>
        public bool AddNodeAtCurrentPosition()
        {
            try
            {
                if (SplineCamera == null || PositionSelector?.MainCamera == null)
                {
                    return false;
                }

                Vector3 pos = PositionSelector.MainCamera.Position;
                Vector3 rot = PositionSelector.MainCamera.Rotation;
                SplineCamera.AddNode(pos, rot, NodeDuration);

                Logger.Info("CameraService: Node added at (" + pos.X.ToString("F1") + ", " + pos.Y.ToString("F1") + ", " + pos.Z.ToString("F1") + ")");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "CameraService: Error adding node");
                return false;
            }
        }

        /// <summary>
        /// Start spline camera playback.
        /// </summary>
        public bool StartPlayback()
        {
            try
            {
                if (SplineCamera == null)
                {
                    UI.Notify("~r~Camera not initialized!");
                    return false;
                }

                if (SplineCamera.Nodes.Count < 2)
                {
                    UI.Notify("Setup at least 2 nodes first!");
                    Logger.Warn("CameraService: StartPlayback rejected - only " + SplineCamera.Nodes.Count + " nodes");
                    return false;
                }

                Logger.Info("CameraService: Starting spline camera playback with " + SplineCamera.Nodes.Count + " nodes");
                _splineCamWasUsed = true;
                SplineCamera.EnterCameraView(Game.Player.Character.GetOffsetInWorldCoords(new Vector3(0f, 0f, 10f)));
                return true;
            }
            catch (Exception ex)
            {
                UI.Notify("~r~Error!");
                Logger.Error(ex, "CameraService: Error in StartPlayback");
                return false;
            }
        }

        /// <summary>
        /// Stop spline camera playback.
        /// </summary>
        public void StopPlayback()
        {
            try
            {
                if (SplineCamera != null && IsSplineCamActive)
                {
                    Logger.Info("CameraService: Stopping spline camera playback");
                    SplineCamera.ExitCameraView();
                    _splineCamWasUsed = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "CameraService: Error in StopPlayback");
            }
        }

        /// <summary>
        /// Restart playback if currently active. Used when speed/interpolation mode changes.
        /// NOTE: This does NOT fade out/in - it just resets the interpolator.
        /// </summary>
        public void RestartPlaybackIfActive()
        {
            try
            {
                if (SplineCamera == null || !IsSplineCamActive) return;

                Logger.Info("CameraService: Restarting playback due to settings change");

                // Rebuild spline with new settings
                if (SplineCamera.Nodes.Count > 0)
                {
                    SplineCamera.RebuildSplineWithCurrentMode();
                }

                // Restart the interpolator without deactivating camera
                SplineCamera.RestartInterpolator();
                
                Logger.Info("CameraService: Interpolator restarted successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "CameraService: Error in RestartPlaybackIfActive");
            }
        }

        /// <summary>
        /// Load a path into the spline camera.
        /// </summary>
        public bool LoadPath(CameraPath path)
        {
            try
            {
                if (path == null) return false;

                ResetAll();

                var nodes = path.ToNodes();
                for (int i = 0; i < nodes.Count; i++)
                {
                    int dur = path.Durations.Count > i ? path.Durations[i] : path.DefaultDuration;
                    SplineCamera.AddNode(nodes[i].Item1, nodes[i].Item2, dur);
                }

                NodeDuration = path.DefaultDuration;
                CurrentFov = path.Fov;
                CurrentSpeed = path.Speed;
                CurrentInterpolationMode = path.InterpolationMode;
                ApplyCameraSettings();

                Logger.Info("CameraService: Path loaded with " + nodes.Count + " nodes");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "CameraService: Error loading path");
                return false;
            }
        }

        /// <summary>
        /// Get current camera position for display.
        /// </summary>
        public Vector3 GetActiveCameraPosition()
        {
            if (IsSelectorActive && PositionSelector?.MainCamera != null)
            {
                return PositionSelector.MainCamera.Position;
            }
            if (IsSplineCamActive && SplineCamera?.MainCamera != null)
            {
                return SplineCamera.MainCamera.Position;
            }
            return Vector3.Zero;
        }

        /// <summary>
        /// Update all active cameras. Call every tick.
        /// </summary>
        public void Update()
        {
            try
            {
                if (IsSplineCamActive || _splineCamWasUsed)
                {
                    SplineCamera?.Update();
                }

                if (IsSelectorActive || _selectorWasUsed)
                {
                    PositionSelector?.Update();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "CameraService: Error in Update");
            }
        }

        /// <summary>
        /// Reset all cameras and create fresh ones.
        /// </summary>
        public void ResetAll()
        {
            try
            {
                Logger.Info("CameraService: ResetAll called");

                // Cancel any ongoing fade
                Function.Call(NativeHashes.UNDO_SCREEN_FADE);

                // Dispose old cameras
                if (SplineCamera != null)
                {
                    if (SplineCamera.MainCamera != null && SplineCamera.MainCamera.Exists())
                    {
                        SplineCamera.MainCamera.IsActive = false;
                    }
                    SplineCamera.Dispose();
                    SplineCamera = null;
                }

                if (PositionSelector != null)
                {
                    if (PositionSelector.MainCamera != null && PositionSelector.MainCamera.Exists())
                    {
                        PositionSelector.MainCamera.IsActive = false;
                    }
                    PositionSelector.Dispose();
                    PositionSelector = null;
                }

                // Disable script camera rendering FIRST, then clear
                Function.Call(NativeHashes.RENDER_SCRIPT_CAMS, false, 0, 0, false, false);
                World.RenderingCamera = null;

                // Create fresh cameras
                SplineCamera = new SplineCamera();
                PositionSelector = new PositionSelector(Vector3.Zero, Vector3.Zero);
                _selectorWasUsed = false;
                _splineCamWasUsed = false;
                ApplyCameraSettings();

                Logger.Info("CameraService: ResetAll completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "CameraService: Error in ResetAll");
            }
        }

        /// <summary>
        /// Dispose all resources. Call during mod shutdown.
        /// </summary>
        public void Dispose()
        {
            try
            {
                Logger.Info("CameraService: Disposing...");

                // Cancel any ongoing fade
                Function.Call(NativeHashes.UNDO_SCREEN_FADE);

                // Dispose spline camera
                if (SplineCamera != null)
                {
                    if (SplineCamera.MainCamera != null && SplineCamera.MainCamera.Exists())
                    {
                        if (SplineCamera.UsePlayerView)
                        {
                            SplineCamera.UsePlayerView = false;
                        }
                        SplineCamera.MainCamera.IsActive = false;
                    }
                    SplineCamera.Dispose();
                    SplineCamera = null;
                }

                // Dispose position selector
                if (PositionSelector != null)
                {
                    if (PositionSelector.MainCamera != null && PositionSelector.MainCamera.Exists())
                    {
                        PositionSelector.MainCamera.IsActive = false;
                    }
                    PositionSelector.Dispose();
                    PositionSelector = null;
                }

                // Disable script camera rendering FIRST, then clear
                Function.Call(NativeHashes.RENDER_SCRIPT_CAMS, false, 0, 0, false, false);
                World.RenderingCamera = null;

                // Ensure player is unfrozen
                Game.Player.Character.FreezePosition = false;
                Game.Player.Character.IsInvincible = false;
                Game.Player.Character.IsVisible = true;

                Logger.Info("CameraService: Disposed successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "CameraService: Error during Dispose");
            }
        }
    }
}
