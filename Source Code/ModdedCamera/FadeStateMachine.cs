using System;
using GTA;
using GTA.Native;

namespace ModdedCamera
{
    /// <summary>
    /// Fade states used during camera transitions.
    /// Extracted to eliminate duplication between SplineCamera and PositionSelector.
    /// </summary>
    public enum FadeState { None, FadingOut, Activating, FadingOutExit, Deactivating }

    /// <summary>
    /// Handles screen fade in/out state machine for camera transitions.
    /// Replaces duplicated fade logic in SplineCamera and PositionSelector.
    /// </summary>
    public class FadeStateMachine
    {
        private readonly Action _onActivate;
        private readonly Action _onDeactivate;
        private readonly string _logPrefix;

        public FadeState State { get; private set; } = FadeState.None;

        /// <summary>
        /// Creates a new fade state machine.
        /// </summary>
        /// <param name="onActivate">Called when fade-in completes after activation</param>
        /// <param name="onDeactivate">Called when fade-in completes after deactivation</param>
        /// <param name="logPrefix">Prefix for log messages (e.g., "SplineCamera" or "PositionSelector")</param>
        public FadeStateMachine(Action onActivate, Action onDeactivate, string logPrefix)
        {
            _onActivate = onActivate;
            _onDeactivate = onDeactivate;
            _logPrefix = logPrefix;
        }

        /// <summary>
        /// Start fade out sequence. Call this to begin camera activation.
        /// </summary>
        public void StartFadeOut(int fadeOutMs = 1200)
        {
            State = FadeState.FadingOut;
            Function.Call(Hash.DO_SCREEN_FADE_OUT, new InputArgument[] { fadeOutMs });
        }

        /// <summary>
        /// Start fade out sequence for exit. Call this to begin camera deactivation.
        /// </summary>
        public void StartFadeOutExit(int fadeOutMs = 1200)
        {
            State = FadeState.FadingOutExit;
            Function.Call(Hash.DO_SCREEN_FADE_OUT, new InputArgument[] { fadeOutMs });
        }

        /// <summary>
        /// Update fade state machine. Call this every tick.
        /// </summary>
        public void Update()
        {
            if (State == FadeState.None) return;

            try
            {
                if (State == FadeState.FadingOut)
                {
                    if (Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT))
                    {
                        _onActivate?.Invoke();
                        State = FadeState.Activating;
                        Function.Call(Hash.DO_SCREEN_FADE_IN, new InputArgument[] { 800 });
                    }
                }
                else if (State == FadeState.Activating)
                {
                    if (Function.Call<bool>(Hash.IS_SCREEN_FADED_IN))
                    {
                        State = FadeState.None;
                    }
                }
                else if (State == FadeState.FadingOutExit)
                {
                    if (Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT))
                    {
                        _onDeactivate?.Invoke();
                        State = FadeState.Deactivating;
                        Function.Call(Hash.DO_SCREEN_FADE_IN, new InputArgument[] { 800 });
                    }
                }
                else if (State == FadeState.Deactivating)
                {
                    if (Function.Call<bool>(Hash.IS_SCREEN_FADED_IN))
                    {
                        State = FadeState.None;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, _logPrefix + " UpdateFade: Unexpected error");
                State = FadeState.None;
            }
        }

        /// <summary>
        /// Reset fade state to None. Use with caution.
        /// </summary>
        public void Reset()
        {
            State = FadeState.None;
        }
    }
}
