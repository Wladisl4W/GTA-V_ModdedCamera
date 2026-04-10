using GTA.Native;

namespace ModdedCamera
{
    /// <summary>
    /// Named constants for GTA V native hashes used throughout the mod.
    /// Replaces magic numbers with readable names for better maintainability.
    /// </summary>
    public static class NativeHashes
    {
        // Screen Fade
        public const Hash DO_SCREEN_FADE_OUT = Hash.DO_SCREEN_FADE_OUT;
        public const Hash DO_SCREEN_FADE_IN = Hash.DO_SCREEN_FADE_IN;
        public const Hash IS_SCREEN_FADED_OUT = Hash.IS_SCREEN_FADED_OUT;
        public const Hash IS_SCREEN_FADED_IN = Hash.IS_SCREEN_FADED_IN;
        public const Hash UNDO_SCREEN_FADE = unchecked((Hash)(-3104983138485256141L));

        // Camera
        public const Hash CREATE_CAM = Hash.CREATE_CAM;
        public const Hash DESTROY_CAM = Hash.DESTROY_CAM;
        public const Hash SET_CAM_ACTIVE = (Hash)3582399230505917858L;
        public const Hash RENDER_SCRIPT_CAMS = Hash.RENDER_SCRIPT_CAMS;

        // Focus & Rendering
        public const Hash SET_FOCUS_AREA = (Hash)658611830838489950L;
        public const Hash DRAW_MARKER = (Hash)2902427857584726153L;
        public const Hash DRAW_MARKER_SPRITE = unchecked((Hash)(-4939229729199161819L));
        public const Hash DRAW_LIGHT_WITH_RANGE = (Hash)7495895348773880760L;
        public const Hash SET_CAM_SHAKE = (Hash)8187532053442985248L;

        // Controls
        public const Hash DISABLE_CONTROL_ACTION = unchecked((Hash)(-100848937243707716L));
        public const Hash ENABLE_CONTROL_ACTION = Hash.ENABLE_CONTROL_ACTION;
        public const Hash GET_DISABLED_CONTROL_NORMAL = unchecked((Hash)(-2783653480577029081L));
        public const Hash GET_HASH_KEY = (Hash)331533201183454215L;

        // Keyboard
        public const Hash DISPLAY_ONSCREEN_KEYBOARD = Hash.DISPLAY_ONSCREEN_KEYBOARD;
        public const Hash UPDATE_ONSCREEN_KEYBOARD = Hash.UPDATE_ONSCREEN_KEYBOARD;
        public const Hash GET_ONSCREEN_KEYBOARD_RESULT = Hash.GET_ONSCREEN_KEYBOARD_RESULT;

        // Gamepad
        public const Hash IS_DISABLED_CONTROL_PRESSED = (Hash)6342219533232326959L;
        public const Hash GET_CONTROL_NORMAL = unchecked((Hash)(-2783653480577029081L));

        // Instructional Buttons
        public const Hash REQUEST_SCALEFORM_MOVIE = (Hash)1296532278728670831L;

        // Player Controls (commonly disabled)
        public const Hash INPUT_ATTACK = (Hash)24;
        public const Hash INPUT_AIM = (Hash)25;
        public const Hash INPUT_CURSOR_SCROLL_UP = (Hash)241;
        public const Hash INPUT_CURSOR_SCROLL_DOWN = (Hash)242;
        public const Hash INPUT_FRONTEND_ACCEPT = (Hash)225;
        public const Hash INPUT_FRONTEND_CANCEL = (Hash)199;
        public const Hash INPUT_FRONTEND_PAUSE = (Hash)199;
        public const Hash INPUT_FRONTEND_PAUSE_ALT = (Hash)200;
        public const Hash INPUT_PHONE_CANCEL = (Hash)177;
    }
}
