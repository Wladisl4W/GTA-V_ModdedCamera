using System;
using System.Windows.Forms;
using GTA;
using GTA.Native;

namespace ModdedCamera.Services
{
    /// <summary>
    /// Handles all input processing: keyboard and gamepad controls.
    /// Extracted from Menu.cs to separate input logic from UI/camera logic.
    /// </summary>
    public class InputService
    {
        // Events for input handling
        public event Action OnToggleMenu;
        public event Action OnBackNavigation;
        public event Action OnAddNode;
        public event Action OnExitPointSelector;
        public event Action OnScrollDurationUp;
        public event Action OnScrollDurationDown;

        /// <summary>
        /// Process keyboard input. Call on KeyUp event.
        /// </summary>
        public void ProcessKeyUp(Keys key)
        {
            if (key == Keys.T)
            {
                OnToggleMenu?.Invoke();
            }
        }

        /// <summary>
        /// Process keyboard input. Call on KeyDown event.
        /// Returns true if input was handled.
        /// </summary>
        public bool ProcessKeyDown(Keys key)
        {
            if (key == Keys.Back)
            {
                OnBackNavigation?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Process gamepad/keyboard input for point selector mode.
        /// Call every tick when point selector is active and menus are hidden.
        /// </summary>
        public void ProcessPointSelectorInput()
        {
            // Add node — ЛКМ (Control 24 = INPUT_ATTACK)
            if (Game.IsControlJustPressed(0, GTA.Control.Attack))
            {
                OnAddNode?.Invoke();
            }

            // Scroll wheel controls
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 241, true);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 242, true);

            float scrollUp = Function.Call<float>(Hash.GET_DISABLED_CONTROL_NORMAL, 0, 241);
            float scrollDown = Function.Call<float>(Hash.GET_DISABLED_CONTROL_NORMAL, 0, 242);

            bool scrollUpPressed = scrollUp > 0.5f || Game.IsControlJustPressed(0, (GTA.Control)241);
            bool scrollDownPressed = scrollDown > 0.5f || Game.IsControlJustPressed(0, (GTA.Control)242);

            if (scrollUpPressed)
            {
                OnScrollDurationUp?.Invoke();
            }
            else if (scrollDownPressed)
            {
                OnScrollDurationDown?.Invoke();
            }

            // Exit — ПКМ (Control 25 = INPUT_AIM / Right Mouse Button)
            if (Game.IsControlJustPressed(0, GTA.Control.Aim))
            {
                OnExitPointSelector?.Invoke();
            }
            // Also allow Exit with B key (FrontendAccept = 225)
            else if (Game.IsControlJustPressed(0, GTA.Control.FrontendAccept))
            {
                OnExitPointSelector?.Invoke();
            }
        }

        /// <summary>
        /// Disable controls that interfere with mod operation.
        /// Call every tick.
        /// </summary>
        public void DisableInterferingControls()
        {
            // Disable Pause controls to prevent game from pausing
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 199, true);  // FrontendPause
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 200, true);  // FrontendPauseAlt
        }
    }
}
