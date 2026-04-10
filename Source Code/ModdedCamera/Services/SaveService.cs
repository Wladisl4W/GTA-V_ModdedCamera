using System;
using GTA;
using GTA.Native;

namespace ModdedCamera.Services
{
    /// <summary>
    /// Manages save/load/delete operations for camera paths.
    /// Handles the keyboard input state machine for naming paths.
    /// Extracted from Menu.cs to separate save logic from UI logic.
    /// </summary>
    public class SaveService
    {
        public enum SaveState { None, Typing, ConfirmOverwrite }

        public SaveState State { get; private set; } = SaveState.None;

        private readonly CameraService _cameraService;
        private string _pendingPathName = "";
        private int _nameInputTimer = 0;
        private const int NAME_INPUT_TIMEOUT = 60000;

        // Events for UI notification
        public event Action<string> OnPathSaved;
        public event Action<string> OnPathLoaded;
        public event Action<string> OnPathDeleted;
        public event Action<string> OnError;

        public SaveService(CameraService cameraService)
        {
            _cameraService = cameraService ?? throw new ArgumentNullException("cameraService");
        }

        /// <summary>
        /// Start saving current path. Opens keyboard for naming.
        /// </summary>
        public bool StartSave()
        {
            if (_cameraService.SplineCamera?.Nodes.Count < 2)
            {
                UI.Notify("Need at least 2 nodes!");
                return false;
            }

            State = SaveState.Typing;
            _nameInputTimer = Game.GameTime;
            Function.Call(Hash.DISPLAY_ONSCREEN_KEYBOARD, new InputArgument[] { 1, "FMMC_MPM_NA", "", "", "", "", "", 64 });
            Logger.Info("SaveService: Save initiated, keyboard opened");
            return true;
        }

        /// <summary>
        /// Update save state machine. Call every tick when save is active.
        /// </summary>
        public void Update()
        {
            if (State == SaveState.None) return;

            try
            {
                if (State == SaveState.Typing)
                {
                    UpdateTypingState();
                }
                else if (State == SaveState.ConfirmOverwrite)
                {
                    UpdateConfirmOverwriteState();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "SaveService: Error in Update");
                State = SaveState.None;
            }
        }

        private void UpdateTypingState()
        {
            // Timeout with overflow protection
            int currentTime = Game.GameTime;
            int elapsed = currentTime - _nameInputTimer;
            if (elapsed < 0) elapsed = int.MaxValue;
            if (elapsed > NAME_INPUT_TIMEOUT)
            {
                Logger.Warn("SaveService: Save name input timed out");
                State = SaveState.None;
                return;
            }

            int status = Function.Call<int>(Hash.UPDATE_ONSCREEN_KEYBOARD, new InputArgument[0]);

            if (status == 0) return; // Still typing

            if (status == 2)
            {
                Logger.Info("SaveService: Save name input cancelled");
                State = SaveState.None;
                return;
            }

            // status == 1: confirmed
            string text = Function.Call<string>(Hash.GET_ONSCREEN_KEYBOARD_RESULT, new InputArgument[0]);
            if (string.IsNullOrEmpty(text))
            {
                State = SaveState.None;
                return;
            }

            // Check duplicate
            if (PathManager.PathExists(text))
            {
                _pendingPathName = text;
                State = SaveState.ConfirmOverwrite;
                UI.Notify("~r~Name exists! Space=overwrite, B=rename");
                return;
            }

            DoSave(text);
            State = SaveState.None;
        }

        private void UpdateConfirmOverwriteState()
        {
            // Reminder
            UI.Notify("~r~'" + _pendingPathName + "' exists! ~y~Space~w~=overwrite, ~r~B~w~=rename");

            // Space = overwrite (Control 223 = FrontendRbutton)
            if (Game.IsControlJustPressed(0, (GTA.Control)223))
            {
                DoSave(_pendingPathName);
                State = SaveState.None;
            }
            // B = reopen keyboard (FrontendAccept = 225)
            else if (Game.IsControlJustPressed(0, GTA.Control.FrontendAccept))
            {
                State = SaveState.Typing;
                _nameInputTimer = Game.GameTime;
                Function.Call(Hash.DISPLAY_ONSCREEN_KEYBOARD, new InputArgument[] { 1, "FMMC_MPM_NA", "", _pendingPathName, "", "", "", 64 });
            }
            // Cancel with Backspace
            else if (Game.IsControlJustPressed(0, GTA.Control.FrontendCancel) || Game.IsControlJustPressed(0, GTA.Control.FrontendPause))
            {
                Logger.Info("SaveService: Overwrite confirmation cancelled");
                State = SaveState.None;
                _pendingPathName = string.Empty;
            }
        }

        private void DoSave(string name)
        {
            try
            {
                if (_cameraService.SplineCamera == null)
                {
                    UI.Notify("~r~SplineCamera is null!");
                    Logger.Error("DoSave: SplineCamera is null");
                    return;
                }

                var positions = _cameraService.SplineCamera.GetPositions();
                var rotations = _cameraService.SplineCamera.GetRotations();
                var durations = _cameraService.SplineCamera.GetDurations();

                Logger.Info("DoSave: Attempting to save path '" + name + "' with " + positions.Count + " positions, " + 
                           rotations.Count + " rotations, " + durations.Count + " durations");

                if (positions.Count < 2)
                {
                    UI.Notify("~r~Need at least 2 nodes!");
                    Logger.Warn("DoSave: Insufficient nodes (" + positions.Count + ")");
                    return;
                }

                CameraPath cp = new CameraPath(
                    name,
                    positions,
                    rotations,
                    durations,
                    _cameraService.NodeDuration,
                    _cameraService.CurrentFov,
                    _cameraService.CurrentSpeed,
                    _cameraService.CurrentInterpolationMode
                );

                Logger.Info("DoSave: CameraPath created, calling PathManager.SavePath...");
                string result = PathManager.SavePath(cp);
                
                if (result != null)
                {
                    UI.Notify("~g~Saved: " + name);
                    _pendingPathName = string.Empty;
                    Logger.Info("SaveService: Path saved successfully: " + result);
                    OnPathSaved?.Invoke(name);
                }
                else
                {
                    UI.Notify("~r~Save failed! Check log for details.");
                    Logger.Error("DoSave: PathManager.SavePath returned null");
                    OnError?.Invoke("Save failed");
                }
            }
            catch (Exception ex)
            {
                UI.Notify("~r~Error: " + ex.Message);
                Logger.Error(ex, "SaveService: Error in DoSave");
                OnError?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// Load a path by name.
        /// </summary>
        public bool LoadPath(string pathName)
        {
            try
            {
                CameraPath cp = PathManager.LoadPath(pathName);
                if (cp == null)
                {
                    UI.Notify("~r~Failed to load!");
                    OnError?.Invoke("Failed to load path");
                    return false;
                }

                bool success = _cameraService.LoadPath(cp);
                if (success)
                {
                    UI.Notify("~g~Loaded: " + pathName);
                    // Notify that path was loaded - triggers menu sync
                    OnPathLoaded?.Invoke(pathName);
                    Logger.Info("SaveService: Path loaded: " + pathName);
                }
                else
                {
                    UI.Notify("~r~Failed to apply path!");
                    OnError?.Invoke("Failed to apply path");
                }
                return success;
            }
            catch (Exception ex)
            {
                UI.Notify("~r~Failed to load!");
                Logger.Error(ex, "SaveService: Error loading path");
                OnError?.Invoke("Error loading path: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Delete a path by name.
        /// </summary>
        public bool DeletePath(string pathName)
        {
            try
            {
                bool success = PathManager.DeletePath(pathName);
                if (success)
                {
                    UI.Notify("~g~Deleted: " + pathName);
                    OnPathDeleted?.Invoke(pathName);
                    Logger.Info("SaveService: Path deleted: " + pathName);
                }
                else
                {
                    UI.Notify("~r~Failed to delete!");
                    OnError?.Invoke("Failed to delete path");
                }
                return success;
            }
            catch (Exception ex)
            {
                UI.Notify("~r~Error: " + ex.Message);
                Logger.Error(ex, "SaveService: Error deleting path");
                OnError?.Invoke(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Cancel any ongoing save operation.
        /// </summary>
        public void Cancel()
        {
            State = SaveState.None;
            _pendingPathName = string.Empty;
        }
    }
}
