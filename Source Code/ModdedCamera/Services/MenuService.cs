using System;
using System.Collections.Generic;
using System.Drawing;
using GTA;
using GTA.Native;
using LemonUI.Menus;
using LemonUI;

namespace ModdedCamera.Services
{
    /// <summary>
    /// Manages all LemonUI menu operations: creation, display, and event handling.
    /// Extracted from Menu.cs to separate UI logic from camera/save logic.
    /// </summary>
    public class MenuService
    {
        // Menus
        public NativeMenu MainMenu { get; private set; }
        public NativeMenu CameraOptionsMenu { get; private set; }
        public NativeMenu SavedPathsMenu { get; private set; }

        // Menu items
        private NativeItem _startItem;
        private NativeItem _stopItem;
        private NativeItem _setupNodesItem;
        private NativeItem _savePathItem;
        private NativeItem _loadPathItem;
        private NativeItem _cameraOptionsItem;
        private NativeItem _resetItem;
        private NativeItem _closeItem;

        private NativeListItem<string> _speedListItem;
        private NativeListItem<string> _fovListItem;
        private NativeListItem<string> _interpolationListItem;
        private NativeCheckboxItem _usePlayerViewCheckbox;

        // Sub-menus for path actions
        private readonly List<NativeMenu> _pathSubMenus = new List<NativeMenu>();

        // Object pool
        public ObjectPool ActivePool { get; private set; }

        private readonly CameraService _cameraService;
        private readonly SaveService _saveService;
        private readonly InputService _inputService;

        public MenuService(CameraService cameraService, SaveService saveService, InputService inputService)
        {
            _cameraService = cameraService ?? throw new ArgumentNullException("cameraService");
            _saveService = saveService ?? throw new ArgumentNullException("saveService");
            _inputService = inputService ?? throw new ArgumentNullException("inputService");
        }

        /// <summary>
        /// Initialize all menus. Call once during startup.
        /// </summary>
        public void Initialize()
        {
            CreateCameraOptionsMenu();
            CreateSavedPathsMenu();
            CreateMainMenu();

            // Setup object pool
            ActivePool = new ObjectPool();
            ActivePool.Add(MainMenu);
            ActivePool.Add(CameraOptionsMenu);
            ActivePool.Add(SavedPathsMenu);

            RefreshSavedPathsMenu();
        }

        /// <summary>
        /// Process menu rendering. Call every tick.
        /// </summary>
        public void Process()
        {
            ActivePool?.Process();
        }

        /// <summary>
        /// Check if any menu is visible.
        /// </summary>
        public bool AreAnyVisible => ActivePool?.AreAnyVisible ?? false;

        /// <summary>
        /// Toggle main menu visibility.
        /// </summary>
        public void ToggleMenu()
        {
            if (AreAnyVisible)
            {
                ActivePool.HideAll();
            }
            else
            {
                MainMenu.Visible = true;
            }
        }

        /// <summary>
        /// Hide all menus.
        /// </summary>
        public void HideAll()
        {
            ActivePool?.HideAll();
        }

        /// <summary>
        /// Show main menu.
        /// </summary>
        public void ShowMainMenu()
        {
            SavedPathsMenu.Visible = false;
            CameraOptionsMenu.Visible = false;
            MainMenu.Visible = true;
        }

        /// <summary>
        /// Sync menu items with current camera settings.
        /// </summary>
        public void SyncCameraOptionsWithMenu()
        {
            try
            {
                _fovListItem.SelectedItem = _cameraService.CurrentFov.ToString();
                _speedListItem.SelectedItem = _cameraService.CurrentSpeed.ToString();
                _interpolationListItem.SelectedItem = (_cameraService.CurrentInterpolationMode == 2) ? "Smooth" : "Linear";
                _usePlayerViewCheckbox.Checked = _cameraService.UsePlayerView;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "MenuService: Error in SyncCameraOptionsWithMenu");
            }
        }

        /// <summary>
        /// Refresh the saved paths menu with current paths from disk.
        /// </summary>
        public void RefreshSavedPathsMenu()
        {
            try
            {
                // Clear old sub-menus
                _pathSubMenus.Clear();
                SavedPathsMenu.Clear();

                List<string> paths = PathManager.GetAllSavedPaths();

                if (paths.Count == 0)
                {
                    SavedPathsMenu.Add(new NativeItem("~y~No saved paths", "Save a path first!"));
                    return;
                }

                foreach (string pathName in paths)
                {
                    NativeMenu pathSubMenu = new NativeMenu(pathName, "Actions");
                    ActivePool.Add(pathSubMenu);
                    _pathSubMenus.Add(pathSubMenu);

                    // Back button
                    NativeItem backBtn = new NativeItem("~y~← Back", "Go back");
                    string currentPathName = pathName;
                    backBtn.Activated += delegate
                    {
                        pathSubMenu.Visible = false;
                        SavedPathsMenu.Visible = true;
                    };
                    pathSubMenu.Add(backBtn);

                    // Load
                    NativeItem loadBtn = new NativeItem("~g~Load", "Load and play");
                    string pn1 = pathName;
                    loadBtn.Activated += delegate
                    {
                        try
                        {
                            _saveService.LoadPath(pn1);
                            ActivePool.HideAll();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "MenuService: Error loading path: " + pn1);
                            UI.Notify("~r~Failed to load path: " + ex.Message);
                        }
                    };
                    pathSubMenu.Add(loadBtn);

                    // Delete confirm submenu
                    NativeMenu delMenu = new NativeMenu("Delete: " + pathName, "Are you sure?");
                    ActivePool.Add(delMenu);
                    _pathSubMenus.Add(delMenu);

                    NativeItem delBackBtn = new NativeItem("~y~← Back", "Cancel");
                    delBackBtn.Activated += delegate { delMenu.Visible = false; pathSubMenu.Visible = true; };
                    delMenu.Add(delBackBtn);

                    NativeItem delYesBtn = new NativeItem("~r~Yes, Delete", "Confirm");
                    string pn2 = pathName;
                    delYesBtn.Activated += delegate
                    {
                        try
                        {
                            _saveService.DeletePath(pn2);
                            RefreshSavedPathsMenu();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "MenuService: Error deleting path: " + pn2);
                            UI.Notify("~r~Failed to delete path: " + ex.Message);
                        }
                        delMenu.Visible = false;
                        pathSubMenu.Visible = true;
                    };
                    delMenu.Add(delYesBtn);

                    // Add delete button to path submenu
                    NativeItem deleteBtn = new NativeItem("~r~Delete", "Remove this path");
                    deleteBtn.Activated += delegate { pathSubMenu.Visible = false; delMenu.Visible = true; };
                    pathSubMenu.Add(deleteBtn);

                    // Add main path item
                    NativeItem pathItem = new NativeItem(pathName, "Click to open actions");
                    pathItem.Activated += delegate { SavedPathsMenu.Visible = false; pathSubMenu.Visible = true; };
                    SavedPathsMenu.Add(pathItem);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "MenuService: Error in RefreshSavedPathsMenu");
            }
        }

        /// <summary>
        /// Handle back navigation from menus. Returns true if handled.
        /// </summary>
        public bool HandleBackNavigation()
        {
            try
            {
                // Check delete confirm menus first (deepest level)
                foreach (var m in _pathSubMenus)
                {
                    if (m.Visible && m.BannerText.Text.StartsWith("Delete: "))
                    {
                        m.Visible = false;
                        // Find parent path menu
                        string pathName = m.BannerText.Text.Substring("Delete: ".Length);
                        foreach (var pm in _pathSubMenus)
                        {
                            if (pm.BannerText.Text == pathName && !pm.Visible)
                            {
                                pm.Visible = true;
                                break;
                            }
                        }
                        return true;
                    }
                }

                // Check path action menus
                foreach (var m in _pathSubMenus)
                {
                    if (m.Visible)
                    {
                        m.Visible = false;
                        SavedPathsMenu.Visible = true;
                        return true;
                    }
                }

                // Check saved paths menu
                if (SavedPathsMenu.Visible)
                {
                    SavedPathsMenu.Visible = false;
                    MainMenu.Visible = true;
                    return true;
                }

                // Check camera options menu
                if (CameraOptionsMenu.Visible)
                {
                    CameraOptionsMenu.Visible = false;
                    MainMenu.Visible = true;
                    return true;
                }

                // Check if in point selector
                if (_cameraService.IsSelectorActive)
                {
                    _cameraService.ExitPointSelector();
                    return true;
                }

                // Don't close main menu with Backspace
                if (MainMenu.Visible)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "MenuService: Error in HandleBackNavigation");
                return false;
            }
        }

        /// <summary>
        /// Get node duration for display in point selector.
        /// </summary>
        public int GetNodeDuration() => _cameraService.NodeDuration;

        /// <summary>
        /// Increase node duration.
        /// </summary>
        public void IncreaseNodeDuration()
        {
            _cameraService.NodeDuration += 100;
        }

        /// <summary>
        /// Decrease node duration.
        /// </summary>
        public void DecreaseNodeDuration()
        {
            _cameraService.NodeDuration = Math.Max(100, _cameraService.NodeDuration - 100);
        }

        /// <summary>
        /// Dispose menu resources.
        /// </summary>
        public void Dispose()
        {
            if (ActivePool != null)
            {
                ActivePool.HideAll();
                ActivePool = null;
            }
        }

        // ========== Private menu creation methods ==========

        private void CreateMainMenu()
        {
            MainMenu = new NativeMenu("Script Cam Tool", string.Empty);

            _startItem = new NativeItem("~g~Start Rendering", "");
            _startItem.Activated += (s, e) => _cameraService.StartPlayback();
            MainMenu.Add(_startItem);

            _stopItem = new NativeItem("~r~Stop Rendering", "");
            _stopItem.Activated += (s, e) => _cameraService.StopPlayback();
            MainMenu.Add(_stopItem);

            _setupNodesItem = new NativeItem("Setup Nodes", "");
            _setupNodesItem.Activated += (s, e) =>
            {
                ActivePool.HideAll(); // Hide menu before entering point selector
                _cameraService.EnterPointSelector();
            };
            MainMenu.Add(_setupNodesItem);

            _savePathItem = new NativeItem("Save Current Path", "");
            _savePathItem.Activated += (s, e) =>
            {
                ActivePool.HideAll(); // Hide menu before opening save keyboard
                _saveService.StartSave();
            };
            MainMenu.Add(_savePathItem);

            _loadPathItem = new NativeItem("Load Path", "Select a saved path to load");
            _loadPathItem.Activated += (s, e) => { MainMenu.Visible = false; SavedPathsMenu.Visible = true; };
            MainMenu.Add(_loadPathItem);

            _cameraOptionsItem = new NativeItem("Camera Options", "Adjust camera settings");
            _cameraOptionsItem.Activated += (s, e) => { MainMenu.Visible = false; CameraOptionsMenu.Visible = true; };
            MainMenu.Add(_cameraOptionsItem);

            _resetItem = new NativeItem("Reset All Cams", "");
            _resetItem.Activated += (s, e) => _cameraService.ResetAll();
            MainMenu.Add(_resetItem);

            _closeItem = new NativeItem("Close", "");
            _closeItem.Activated += (s, e) => ActivePool.HideAll();
            MainMenu.Add(_closeItem);
        }

        private void CreateCameraOptionsMenu()
        {
            CameraOptionsMenu = new NativeMenu("Camera Options", string.Empty);

            _speedListItem = new NativeListItem<string>("Speed", "");
            for (int i = 1; i <= 100; i++)
                _speedListItem.Items.Add(i.ToString());
            _speedListItem.SelectedItem = "3";
            CameraOptionsMenu.Add(_speedListItem);

            _fovListItem = new NativeListItem<string>("Field Of View", "");
            for (int i = 1; i <= 100; i++)
                _fovListItem.Items.Add(i.ToString());
            _fovListItem.SelectedItem = "50";
            CameraOptionsMenu.Add(_fovListItem);

            _usePlayerViewCheckbox = new NativeCheckboxItem("Use Player View", "(May create smoother terrain rendering when enabled but restricts player movement and is prone to bugs.)");
            CameraOptionsMenu.Add(_usePlayerViewCheckbox);

            _interpolationListItem = new NativeListItem<string>("Interpolation", "Smooth = curved path, Linear = straight lines");
            _interpolationListItem.Items.Add("Linear");
            _interpolationListItem.Items.Add("Smooth");
            _interpolationListItem.SelectedItem = "Linear"; // FIXED: Default to Linear
            CameraOptionsMenu.Add(_interpolationListItem);

            // Wire up events
            _speedListItem.ItemChanged += OnSpeedChanged;
            _fovListItem.ItemChanged += OnFovChanged;
            _interpolationListItem.ItemChanged += OnInterpolationChanged;
            _usePlayerViewCheckbox.CheckboxChanged += OnCheckboxChanged;
        }

        private void CreateSavedPathsMenu()
        {
            SavedPathsMenu = new NativeMenu("Saved Paths", string.Empty);
        }

        // ========== Event Handlers ==========

        private void OnSpeedChanged(object sender, ItemChangedEventArgs<string> e)
        {
            int v;
            if (int.TryParse(_speedListItem.SelectedItem, out v) && v > 0)
            {
                _cameraService.CurrentSpeed = v;
                _cameraService.ApplyCameraSettings();
                
                // FIXED: Explicitly restart playback if camera is active (now consistent with OnInterpolationChanged)
                if (_cameraService.IsSplineCamActive)
                {
                    _cameraService.RestartPlaybackIfActive();
                }
                
                Logger.Info("MenuService: Speed changed to: " + v);
            }
        }

        private void OnFovChanged(object sender, ItemChangedEventArgs<string> e)
        {
            int v;
            if (int.TryParse(_fovListItem.SelectedItem, out v) && v > 0)
            {
                _cameraService.CurrentFov = v;
                _cameraService.ApplyCameraSettings();
                Logger.Info("MenuService: FOV changed to: " + v);
            }
        }

        private void OnInterpolationChanged(object sender, ItemChangedEventArgs<string> e)
        {
            int mode = (_interpolationListItem.SelectedItem == "Smooth") ? 2 : 0;

            _cameraService.CurrentInterpolationMode = mode;
            _cameraService.ApplyCameraSettings();

            // Restart playback automatically if camera is active (no fade out/in)
            if (_cameraService.IsSplineCamActive)
            {
                _cameraService.RestartPlaybackIfActive();
                UI.Notify("~g~Interpolation mode: " + (mode == 2 ? "Smooth" : "Linear") + "\n~y~Playback restarted with " + _cameraService.SplineCamera.Nodes.Count + " nodes");
            }
            else if (_cameraService.SplineCamera?.Nodes.Count > 0)
            {
                _cameraService.SplineCamera.RebuildSplineWithCurrentMode();
                UI.Notify("~g~Interpolation mode: " + (mode == 2 ? "Smooth" : "Linear") + "\n~y~Spline rebuilt for " + _cameraService.SplineCamera.Nodes.Count + " nodes");
            }
            else
            {
                UI.Notify("~g~Interpolation mode: " + (mode == 2 ? "Smooth" : "Linear"));
            }

            Logger.Info("MenuService: Interpolation mode changed to: " + (mode == 2 ? "Smooth" : "Linear"));
        }

        private void OnCheckboxChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender == _usePlayerViewCheckbox)
                    _cameraService.UsePlayerView = _usePlayerViewCheckbox.Checked;

                _cameraService.ApplyCameraSettings();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "MenuService: Error in OnCheckboxChanged");
            }
        }
    }
}
