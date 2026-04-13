using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace ModdedCamera
{

	public class SplineCamera
	{
		// Internal interpolation engine - uses client-side math instead of natives
		private CameraInterpolator _interpolator;

		// Fade state machine - uses shared FadeStateMachine class
		private FadeStateMachine _fadeMachine;

		// (get) Token: 0x06000047 RID: 71 RVA: 0x00002BE8 File Offset: 0x00000DE8
		public Camera MainCamera
		{
			get
			{
				return this._mainCamera;
			}
		}

		// FIXED: Added safety check to prevent NullReferenceException
		public bool IsCameraAvailable
		{
			get { return this._mainCamera != null && this._mainCamera.Exists(); }
		}


		// (get) Token: 0x06000048 RID: 72 RVA: 0x00002C00 File Offset: 0x00000E00
		// (set) Token: 0x06000049 RID: 73 RVA: 0x00002C08 File Offset: 0x00000E08
		public bool InterpToPlayer { get; set; }


		// (get) Token: 0x0600004A RID: 74 RVA: 0x00002C14 File Offset: 0x00000E14
		// (set) Token: 0x0600004B RID: 75 RVA: 0x00002C2C File Offset: 0x00000E2C
		public bool UsePlayerView
		{
			get
			{
				return this._usePlayerView;
			}
			set
			{
				if (value)
				{
					this._startPos = Game.Player.Character.Position;
					this._hasStartPosition = true;
					Game.Player.Character.IsInvincible = true;
					Game.Player.Character.IsVisible = false;
				}
				else
				{
					// Teleport back only if start position is valid
					if (this._hasStartPosition)
					{
						Game.Player.Character.Position = this._startPos;
					}
					Game.Player.Character.IsInvincible = false;
					Game.Player.Character.IsVisible = true;
					this._hasStartPosition = false;
				}
				this._usePlayerView = value;
			}
		}


		// (set) Token: 0x0600004C RID: 76 RVA: 0x00002CC7 File Offset: 0x00000EC7
		private int _currentSpeed = 3;
		private List<int> _baseDurations = new List<int>(); // Original durations before speed adjustment
		public int Speed
		{
			set
			{
				try
				{
					int speed = Math.Max(1, value); // Prevent division by zero
					int oldSpeed = _currentSpeed;
					_currentSpeed = speed;

					// Speed multiplier: higher speed = shorter durations = faster movement
					// Speed 1 = 3x slower, Speed 3 = normal (1x), Speed 10 = 3.3x faster
					float multiplier = 3.0f / speed;

					// Update all existing node durations from base values
					for (int i = 0; i < _baseDurations.Count; i++)
					{
						_durations[i] = (int)Math.Max(100, _baseDurations[i] * multiplier);
					}

					// FIXED: Removed automatic restart from setter (was causing asymmetry with InterpolationMode).
					// Caller is now responsible for explicitly restarting playback via RestartInterpolator().
					// This makes Speed and InterpolationMode behave consistently - both are "set and forget",
					// and RestartPlaybackIfActive() must be called explicitly.

					Logger.Info("Speed changed from " + oldSpeed + " to " + speed + " (multiplier: " + multiplier.ToString("F2") + "x)");
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "Error setting speed");
				}
			}
		}


		// (get) Token: 0x0600004D RID: 77 RVA: 0x00002D04 File Offset: 0x00000F04
		public List<Tuple<Vector3, Vector3>> Nodes
		{
			get
			{
				return this._nodes;
			}
		}

		// Get positions list for serialization
		public List<Vector3> GetPositions()
		{
			var positions = new List<Vector3>();
			foreach (var node in this._nodes)
			{
				positions.Add(node.Item1);
			}
			return positions;
		}

		// Get rotations list for serialization
		public List<Vector3> GetRotations()
		{
			var rotations = new List<Vector3>();
			foreach (var node in this._nodes)
			{
				rotations.Add(node.Item2);
			}
			return rotations;
		}

		// Get durations list for serialization
		public List<int> GetDurations()
		{
			return new List<int>(this._durations); // Return copy
		}


		// Interpolation mode: 0 = Linear (straight lines), 2 = Smooth (Catmull-Rom curves)
		public int InterpolationMode
		{
			get { return this._interpolationMode; }
			set
			{
				try
				{
					if (this._interpolationMode == value) return;

					Logger.Info("InterpolationMode changed from " + this._interpolationMode + " to " + value);
					this._interpolationMode = value;

					// Update interpolator mode immediately
					if (this._interpolator != null)
					{
						this._interpolator.InterpolationMode = value;
						Logger.Info("Interpolator mode set to: " + (value == 0 ? "Linear (no smoothing)" : "Smooth (Catmull-Rom)"));
					}

					Logger.Info("Interpolation mode updated to " + value);
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "Error changing interpolation mode");
					try { UI.Notify("~r~Error changing interpolation mode!"); } catch (Exception notifyEx) { Logger.Debug("Failed to show error notification: " + notifyEx.Message); }
				}
			}
		}
		private int _interpolationMode = 2;


		public SplineCamera()
		{
			try
			{
				// Initialize the client-side interpolator
				this._interpolator = new CameraInterpolator();

				// Create a regular scripted camera instead of spline camera
				// This gives us full control over looping without native spline conflicts
				int cameraHandle = Function.Call<int>(Hash.CREATE_CAM, new InputArgument[]
				{
					"DEFAULT_SCRIPTED_CAMERA",
					0
				});

				if (cameraHandle == 0)
				{
					throw new Exception("Failed to create DEFAULT_SCRIPTED_CAMERA - invalid handle returned");
				}

				this._mainCamera = new Camera(cameraHandle);

				if (this._mainCamera == null || !this._mainCamera.Exists())
				{
					throw new Exception("Camera object creation failed after CREATE_CAM");
				}

				this._nodes = new List<Tuple<Vector3, Vector3>>();
				this._renderSceneTimer = new Timer(5000);
				this._renderSceneTimer.Start();

				// Initialize fade state machine
				this._fadeMachine = new FadeStateMachine(
					onActivate: () => {
						this.MainCamera.IsActive = true;
						World.RenderingCamera = this.MainCamera;
						Function.Call(Hash.RENDER_SCRIPT_CAMS, true, 0, 0, false, false);

						// Start interpolator playback
						if (this._interpolator != null)
						{
							this._interpolator.Start();
							Logger.Info("Interpolator playback STARTED");
						}

						Function.Call(Hash.DO_SCREEN_FADE_IN, new InputArgument[] { 800 });
					},
					onDeactivate: () => {
						if (this.UsePlayerView)
						{
							this.UsePlayerView = false;
						}

						// Stop interpolator
						if (this._interpolator != null)
						{
							this._interpolator.Stop();
							Logger.Info("Interpolator playback STOPPED");
						}

						this.MainCamera.IsActive = false;
						World.RenderingCamera = null;
						Function.Call(Hash.RENDER_SCRIPT_CAMS, false, 0, 0, false, false);

						Function.Call(Hash.DO_SCREEN_FADE_IN, new InputArgument[] { 800 });
					},
					logPrefix: "SplineCamera"
				);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Error creating SplineCamera - attempting fallback");

				// Try to create a basic camera even if camera creation fails
				try
				{
					int fallbackHandle = Function.Call<int>(Hash.CREATE_CAM, new InputArgument[]
					{
						"DEFAULT_SPLINE_CAMERA",
						0
					});

					if (fallbackHandle == 0)
					{
						throw new Exception("Fallback camera creation also failed - invalid handle");
					}

					this._mainCamera = new Camera(fallbackHandle);
					this._interpolator = new CameraInterpolator();
					this._nodes = new List<Tuple<Vector3, Vector3>>();
					this._renderSceneTimer = new Timer(5000);
					this._renderSceneTimer.Start();

					Logger.Warn("WARNING: Using fallback DEFAULT_SPLINE_CAMERA - some features may not work!");
				}
				catch (Exception ex2)
				{
					Logger.Error(ex2, "CRITICAL: Failed to create any camera! Mod cannot function.");
					throw new Exception("CRITICAL: Camera initialization failed completely", ex2);
				}
			}
		}

		// Properly dispose of the camera to prevent resource leaks
		public void Dispose()
		{
			try
			{
				if (this._mainCamera != null && this._mainCamera.Exists())
				{
					if (this._mainCamera.IsActive)
					{
						this._mainCamera.IsActive = false;
					}
					// SHVDN3 uses DESTROY_CAM native instead of Camera.Delete()
					Function.Call(Hash.DESTROY_CAM, new InputArgument[] { this._mainCamera.Handle });
					this._mainCamera = null;
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Error disposing SplineCamera");
			}
		}


		public void AddNode(Vector3 position, Vector3 rotation, int duration)
		{
			try
			{
				// Safety checks
				if (this._mainCamera == null)
				{
					Logger.Error("AddNode: Camera is null!");
					return;
				}

				if (!this._mainCamera.Exists())
				{
					Logger.Error("AddNode: Camera does not exist!");
					return;
				}

				// Validate duration
				if (duration < 100)
				{
						Logger.Warn("AddNode: Duration " + duration + "ms is too small, using minimum 100ms");
					duration = 100;
				}

				// Simply add to our list - no native spline calls since we use CameraInterpolator
				this._nodes.Add(new Tuple<Vector3, Vector3>(position, rotation));
				
				// Store base duration and apply current speed multiplier
				this._baseDurations.Add(duration);
				float speedMultiplier = 3.0f / _currentSpeed;
				int adjustedDuration = (int)Math.Max(100, duration * speedMultiplier);
				this._durations.Add(adjustedDuration);
				
				this._defaultDuration = duration;

				Logger.Debug("Node added: pos=(" + position.X.ToString("F1") + ", " + position.Y.ToString("F1") + ", " + position.Z.ToString("F1") + ") rot=(" + rotation.X.ToString("F1") + ", " + rotation.Y.ToString("F1") + ", " + rotation.Z.ToString("F1") + ") duration=" + duration + "ms (adjusted: " + adjustedDuration + "ms, speed: " + _currentSpeed + ")");
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Error adding node to spline");
			}
		}

		public void ClearNodes()
		{
			try
			{
				this._nodes.Clear();
				this._durations.Clear();
				this._baseDurations.Clear();
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Error clearing nodes");
			}
		}

		// Rebuild all nodes with current interpolation mode (use after changing mode)
		public void RebuildSplineWithCurrentMode()
		{
			try
			{
				if (this._nodes.Count == 0) return;

				Logger.Info("Rebuilding spline with current mode (" + this._interpolationMode + ") for " + this._nodes.Count + " nodes");

				// Update interpolator mode if it exists
				if (this._interpolator != null)
				{
					this._interpolator.InterpolationMode = this._interpolationMode;
				}

				// FIXED: Save ORIGINAL durations from _baseDurations, not speed-adjusted _durations
				// _durations are already multiplied by speed factor, so using them would apply
				// the speed multiplier TWICE when AddNode() is called below
				var savedNodes = new List<Tuple<Vector3, Vector3>>(this._nodes);
				var savedBaseDurations = new List<int>(this._baseDurations);

				// Clear nodes
				this._nodes.Clear();
				this._durations.Clear();
				this._baseDurations.Clear();

				// Re-add all nodes using ORIGINAL durations (speed will be reapplied by AddNode)
				for (int i = 0; i < savedNodes.Count; i++)
				{
					int originalDuration = savedBaseDurations.Count > i ? savedBaseDurations[i] : this._defaultDuration;
					this.AddNode(savedNodes[i].Item1, savedNodes[i].Item2, originalDuration);
				}

				Logger.Info("Spline rebuilt successfully with " + this._nodes.Count + " nodes (mode: " + (this._interpolationMode == 0 ? "Linear" : "Smooth") + ")");
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Error rebuilding spline");
			}
		}

		// Restart interpolator playback without fading out/in
		// Used when changing speed or interpolation mode during active playback
		public void RestartInterpolator()
		{
			try
			{
				if (this._interpolator == null)
				{
					Logger.Warn("Cannot restart interpolator: interpolator is null");
					return;
				}

				if (this._nodes.Count < 2)
				{
					Logger.Warn("Cannot restart interpolator: insufficient nodes (" + this._nodes.Count + "), need at least 2");
					return;
				}

				Logger.Info("Restarting interpolator playback");

				var positions = this.GetPositions();
				var rotations = this.GetRotations();
				var durations = this.GetDurations();

				this._interpolator.SetPath(positions, rotations, durations);
				// NOTE: InterpolationMode is already set by ApplyCameraSettings() → InterpolationMode setter
				// or by RebuildSplineWithCurrentMode(). No need to set it again here.
				this._interpolator.Start();

				Logger.Info("Interpolator restarted successfully");
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Error restarting interpolator");
			}
		}


		public void EnterCameraView(Vector3 position)
		{
			this.MainCamera.Position = position;

			// Prepare interpolator for playback
			if (this._nodes.Count >= 2)
			{
				try
				{
					var positions = this.GetPositions();
					var rotations = this.GetRotations();
					var durations = this.GetDurations();
					this._interpolator.SetPath(positions, rotations, durations);

					// Pass the current interpolation mode to the interpolator
					this._interpolator.InterpolationMode = this._interpolationMode;

						Logger.Info("Interpolator ready: " + positions.Count + " waypoints loaded");
						Logger.Info("Playback mode: " + (this._interpolationMode == 0 ? "Linear (no smoothing)" : "Smooth (Catmull-Rom)"));
						Logger.Info("Looping: ENABLED (instant, no delay)");
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "Error setting interpolator path");
				}
			}

			// Start fade out
			this._fadeMachine.StartFadeOut(1200);
		}


		public void ExitCameraView()
		{
			// Start fade out exit
			this._fadeMachine.StartFadeOutExit(1200);
		}

		public void Update()
		{
			// Update fade state machine first
			this._fadeMachine.Update();

			bool isActive = this.MainCamera.IsActive;
			if (isActive)
			{
				// Use interpolator-based movement for all modes
				try
				{
					this.UpdateWithInterpolator();
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "Error in interpolator-based update");
					try { UI.Notify("~r~Camera Update Error!"); } catch (Exception notifyEx) { Logger.Debug("Failed to show error notification: " + notifyEx.Message); }
				}
			}
		}

		private void UpdateWithInterpolator()
		{
			try
			{
				if (this._mainCamera == null || !this._mainCamera.Exists())
				{
					Logger.Warn("UpdateWithInterpolator: Camera not available");
					return;
				}

				// Update interpolator position and rotation
				Vector3 interpPos;
				Vector3 interpRot;
				this._interpolator.Update(out interpPos, out interpRot);

				// ALWAYS apply position and rotation from interpolator
				// This is the ONLY system moving the camera
				if (interpPos != Vector3.Zero || interpRot != Vector3.Zero)
				{
					this._mainCamera.Position = interpPos;
					this._mainCamera.Rotation = interpRot;
					this._previousPos = this._mainCamera.Position;
				}

				// Update rendering
				this.UpdateRenderScene();
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "UpdateWithInterpolator: Critical error");
			}
		}

		// Common rendering code for both modes
		private void UpdateRenderScene()
		{
			try
			{
				if (this._mainCamera == null || !this._mainCamera.Exists())
				{
					Logger.Warn("UpdateRenderScene: Camera not available");
					return;
				}

				bool shouldRender = this._renderSceneTimer.Enabled && this._renderSceneTimer.Check();
				if (shouldRender)
				{
					// Use CameraRenderer utilities instead of direct native calls
					CameraRenderer.UpdateFocusArea(this._mainCamera.Position);
					CameraRenderer.DrawRenderScene();
					this._renderSceneTimer.Reset();
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "UpdateRenderScene: Error");
			}
		}

		private int _defaultDuration = 5000;
		private List<int> _durations = new List<int>();


		private bool _usePlayerView;


		private Timer _renderSceneTimer;


		private Camera _mainCamera;


		private List<Tuple<Vector3, Vector3>> _nodes;


		private Vector3 _startPos;
		private bool _hasStartPosition = false;


		private Vector3 _previousPos;
	}
}
