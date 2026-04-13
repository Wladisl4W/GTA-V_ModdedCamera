using System;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using ModdedCamera.Services;

namespace ModdedCamera
{
    /// <summary>
    /// REFACTORED: Thin coordinator that delegates all work to specialized services.
    /// Reduced from 942 lines to ~130 lines.
    /// 
    /// Responsibilities:
    /// - Wire up services
    /// - Forward game events (Tick, KeyUp, KeyDown)
    /// - Handle disposal
    /// </summary>
    public class Menu : Script
    {
        private CameraService _cameraService;
        private SaveService _saveService;
        private InputService _inputService;
        private MenuService _menuService;

        public Menu()
        {
            try
            {
                Logger.Info("=== ModdedCamera Mod Starting ===");

                // Initialize services
                _cameraService = new CameraService();
                _saveService = new SaveService(_cameraService);
                _inputService = new InputService();
                _menuService = new MenuService(_cameraService, _saveService, _inputService);

                // Wire up events
                WireInputEvents();
                WireServiceEvents();

                // Initialize
                _cameraService.Initialize();
                _menuService.Initialize();
                _menuService.SyncCameraOptionsWithMenu();

                // Subscribe to game events
                Tick += OnTick;
                KeyUp += OnKeyUp;
                KeyDown += OnKeyDown;

                Logger.Info("=== ModdedCamera Mod Started Successfully ===");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "CRITICAL: Error during mod initialization");
                UI.Notify("~r~ModdedCamera: Initialization failed! Check log.");
            }
        }

        /// <summary>
        /// Wire input events to service handlers.
        /// </summary>
        private void WireInputEvents()
        {
            _inputService.OnToggleMenu += _menuService.ToggleMenu;
            _inputService.OnBackNavigation += OnBackNavigation;
            _inputService.OnAddNode += OnAddNode;
            _inputService.OnExitPointSelector += () => _cameraService.ExitPointSelector();
            _inputService.OnScrollDurationUp += OnScrollDurationUp;
            _inputService.OnScrollDurationDown += OnScrollDurationDown;
        }

        /// <summary>
        /// Wire service events for cross-service communication.
        /// </summary>
        private void WireServiceEvents()
        {
            _saveService.OnPathSaved += (pathName) => _menuService.RefreshSavedPathsMenu();
            _saveService.OnPathDeleted += (pathName) => _menuService.RefreshSavedPathsMenu();
            // Sync menu options with loaded path settings
            _saveService.OnPathLoaded += (pathName) => _menuService.SyncCameraOptionsWithMenu();
        }

        // ===================== GAME TICK =====================

        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                // Skip processing when player doesn't exist (during loading/cutscenes)
                if (!Game.Player.Character.Exists()) return;

                int gameTime = Game.GameTime;

                // Draw corner notification for node duration
                DrawNodeDurationNotification(gameTime);

                // Handle save state machine
                _saveService.Update();
                if (_saveService.State != SaveService.SaveState.None)
                {
                    _menuService.Process();
                    return;
                }

                // Process point selector input when active and menus hidden
                bool pointSelectorActive = _cameraService.IsSelectorActive;
                bool menusVisible = _menuService.AreAnyVisible;

                if (pointSelectorActive && !menusVisible)
                {
                    _inputService.ProcessPointSelectorInput();
                }

                // Disable interfering controls
                _inputService.DisableInterferingControls();

                // Process menus
                _menuService.Process();

                // Update cameras
                _cameraService.Update();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in OnTick");
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            _inputService.ProcessKeyUp(e.KeyCode);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            bool handled = _inputService.ProcessKeyDown(e.KeyCode);
            e.SuppressKeyPress = handled;
            e.Handled = handled;
        }

        // ===================== INPUT HANDLERS =====================

        private void OnBackNavigation()
        {
            Logger.Info("OnBackNavigation called");
            _menuService.HandleBackNavigation();
        }

        private void OnAddNode()
        {
            if (_cameraService.AddNodeAtCurrentPosition())
            {
                Vector3 pos = _cameraService.GetActiveCameraPosition();
                int duration = _menuService.GetNodeDuration();
                UI.ShowSubtitle("Node added\nPos: (" + pos.X.ToString("F1") + ", " + pos.Y.ToString("F1") + ", " + pos.Z.ToString("F1") + ")\nDuration: " + duration + "ms");
            }
        }

        private void OnScrollDurationUp()
        {
            _menuService.IncreaseNodeDuration();
            ShowDurationNotification();
        }

        private void OnScrollDurationDown()
        {
            _menuService.DecreaseNodeDuration();
            ShowDurationNotification();
        }

        private void ShowDurationNotification()
        {
            int duration = _menuService.GetNodeDuration();
            _cornerMessage = "Duration: " + duration + "ms";
            _cornerShowTime = Game.GameTime;
        }

        // ===================== CORNER NOTIFICATIONS =====================

        private string _cornerMessage = null;
        private int _cornerShowTime = 0;
        private const int CORNER_DURATION = 2000;

        private void DrawNodeDurationNotification(int gameTime)
        {
            if (_cornerMessage != null)
            {
                if (gameTime - _cornerShowTime > CORNER_DURATION)
                {
                    _cornerMessage = null;
                }
                else
                {
                    UI.ShowSubtitle(_cornerMessage);
                }
            }
        }

        // ===================== DISPOSAL =====================

        protected override void Dispose(bool disposing)
        {
            try
            {
                Logger.Info("Disposing ModdedCamera...");
                _cameraService?.Dispose();
                _menuService?.Dispose();
                Logger.Flush();
                Logger.Info("ModdedCamera disposed successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during Dispose");
            }
            base.Dispose(disposing);
        }
    }
}
