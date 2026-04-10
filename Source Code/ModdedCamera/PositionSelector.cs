using System;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
using ModdedCamera.Gamepad;
using ModdedCamera.Input_Handlers;

namespace ModdedCamera
{

	public class PositionSelector
	{
		// Native hashes for readability
		private const Hash HASH_GET_CONTROL_NORMAL = (Hash)6342219533232326959L;  // IS_DISABLED_CONTROL_PRESSED
		private const Hash HASH_GET_CONTROL_VALUE = unchecked((Hash)(-2783653480577029081L));  // GET_CONTROL_NORMAL
		private const Hash HASH_DISABLE_CONTROL = unchecked((Hash)(-100848937243707716L));  // DISABLE_CONTROL_ACTION
		private const Hash HASH_DRAW_MARKER = (Hash)2902427857584726153L;  // DRAW_MARKER
		private const Hash HASH_GET_HASH_KEY = (Hash)331533201183454215L;  // GET_HASH_KEY
		private const Hash HASH_FADE = unchecked((Hash)(-8567153562878803281L));  // DO_SCREEN_FADE
		private const Hash HASH_SET_CAM_ACTIVE = (Hash)3582399230505917858L;  // SET_CAM_ACTIVE
		private const Hash HASH_UNFADE = unchecked((Hash)(-3104983138485256141L));  // UNDO_SCREEN_FADE
		private const Hash HASH_SET_FOCUS_AREA = (Hash)658611830838489950L;  // SET_FOCUS_AREA
		private const Hash HASH_SET_CAM_SHAKE = (Hash)8187532053442985248L;  // SET_CAM_ACTIVE_WITH_NAME
		private const Hash HASH_DRAW_LIGHT = (Hash)7495895348773880760L;  // DRAW_LIGHT_WITH_RANGE
		private const Hash HASH_DRAW_MARKER_SPRITE = unchecked((Hash)(-4939229729199161819L));  // DRAW_MARKER_SPRITE

		// (get) Token: 0x0600005A RID: 90 RVA: 0x000034D0 File Offset: 0x000016D0
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


		public PositionSelector(Vector3 position, Vector3 rotation)
		{
			this.GamepadHandler = new GamepadHandler();
			this.GamepadHandler.LeftStickChanged += this.LeftStickChanged;
			this.GamepadHandler.RightStickChanged += this.RightStickChanged;
			this.GamepadHandler.LeftStickPressed += this.LeftStickPressed;
			this._instructionalButtons = new Scaleform(Function.Call<int>(unchecked((Hash)1296532278728670831L), new InputArgument[]
			{
				"instructional_buttons"
			}));
			this._mainCamera = World.CreateCamera(position, rotation, 50f);
			this._mainCamera.IsActive = false;
			this._previousPos = position;
			this._renderSceneTimer = new Timer(5000);
			this._renderSceneTimer.Start();
		}

		// Properly dispose of all resources to prevent leaks
		public void Dispose()
		{
			try
			{
				// Unsubscribe from gamepad events to prevent memory leaks
				if (this.GamepadHandler != null)
				{
					this.GamepadHandler.LeftStickChanged -= this.LeftStickChanged;
					this.GamepadHandler.RightStickChanged -= this.RightStickChanged;
					this.GamepadHandler.LeftStickPressed -= this.LeftStickPressed;
					// Clear all event subscribers and dispose resources
					this.GamepadHandler.Dispose();
					this.GamepadHandler = null;
				}

				// Hide and release Scaleform
				if (this._instructionalButtons != null)
				{
					// Scaleform in SHVDN3 is garbage collected
					// Just null the reference - the native handle will be cleaned up
					this._instructionalButtons = null;
				}

				// Delete camera to prevent resource leak
				if (this._mainCamera != null && this._mainCamera.Exists())
				{
					if (this._mainCamera.IsActive)
					{
						this._mainCamera.IsActive = false;
					}
					// Use DESTROY_CAM native
					Function.Call(Hash.DESTROY_CAM, new InputArgument[] { this._mainCamera.Handle });
					this._mainCamera = null;
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Error disposing PositionSelector");
			}
		}


		private void LeftStickChanged(object sender, AnalogStickChangedEventArgs e)
		{
			bool flag = e.X > 127;
			if (flag)
			{
				this._previousPos -= Utils.RotationToDirection(this.MainCamera.Rotation).RightVector(new Vector3(0f, 0f, 1f)) * (Function.Call<float>(unchecked((Hash)(-1424092350868114077L)), new InputArgument[]
				{
					2,
					218
				}) * -3f);
			}
			bool flag2 = e.X < 127;
			if (flag2)
			{
				this._previousPos += Utils.RotationToDirection(this.MainCamera.Rotation).LeftVector(new Vector3(0f, 0f, 1f)) * (Function.Call<float>(unchecked((Hash)(-1424092350868114077L)), new InputArgument[]
				{
					2,
					218
				}) * -3f);
			}
			bool flag3 = e.Y != 127;
			if (flag3)
			{
				this._previousPos += Utils.RotationToDirection(this.MainCamera.Rotation) * (Function.Call<float>(unchecked((Hash)(-1424092350868114077L)), new InputArgument[]
				{
					0,
					8
				}) * -5f);
			}
			this._currentLerpTime += 0.02f;
			bool flag4 = this._currentLerpTime > this.LerpTime;
			if (flag4)
			{
				this._currentLerpTime = this.LerpTime;
			}
			float num = this._currentLerpTime / this.LerpTime;
			this._mainCamera.Position = Vector3.Lerp(this.MainCamera.Position, this._previousPos, num);
		}


		private void RightStickChanged(object sender, AnalogStickChangedEventArgs e)
		{
			Camera mainCamera = this.MainCamera;
			mainCamera.Rotation += new Vector3(Function.Call<float>(unchecked((Hash)(-1424092350868114077L)), new InputArgument[]
			{
				2,
				221
			}) * -4f, 0f, Function.Call<float>(unchecked((Hash)(-1424092350868114077L)), new InputArgument[]
			{
				2,
				220
			}) * -5f) * this.RotationSpeed;
		}


		private void LeftStickPressed(object sender, ButtonPressedEventArgs e)
		{
			this._previousPos += Utils.RotationToDirection(this.MainCamera.Rotation) * (Function.Call<float>(unchecked((Hash)(-1424092350868114077L)), new InputArgument[]
			{
				2,
				230
			}) * -5f);
		}


		public void EnterCameraView(Vector3 position)
		{
			// Start fade out, then activate camera after fade completes
			this._fadeState = FadeState.FadingOut;
			Function.Call(Hash.DO_SCREEN_FADE_OUT, new InputArgument[] { 1200 });
			this.MainCamera.Position = position;

			// Disable controls once at entry instead of every frame
			this.DisablePlayerControls();
		}

		public void ExitCameraView()
		{
			// Start fade out, then deactivate camera after fade completes
			this._fadeState = FadeState.FadingOutExit;
			Function.Call(Hash.DO_SCREEN_FADE_OUT, new InputArgument[] { 1200 });

			// Re-enable controls at exit
			this.EnablePlayerControls();
		}

		// Disable player controls to prevent interference with camera movement
		private static readonly int[] ControlsToDisable = { 80, 20, 140, 142, 264, 27, 79, 210, 209, 203, 311, 309, 233, 187, 173, 48, 43, 19, 301, 288, 289, 298, 31, 30, 34, 35, 32, 33 };

		private void DisablePlayerControls()
		{
			foreach (int control in ControlsToDisable)
			{
				Function.Call(HASH_DISABLE_CONTROL, new InputArgument[] { 2, control, true });
			}
		}

		private void EnablePlayerControls()
		{
			foreach (int control in ControlsToDisable)
			{
				Function.Call(Hash.ENABLE_CONTROL_ACTION, new InputArgument[] { 2, control, true });
			}
		}

		// Fade state machine — uses HAS_SCREEN_FADED_* instead of hardcoded timers
		private enum FadeState { None, FadingOut, Activating, FadingOutExit, Deactivating }
		private FadeState _fadeState = FadeState.None;

		private void UpdateFade()
		{
			try
			{
				if (this._fadeState == FadeState.None) return;

				if (this._mainCamera == null || !this._mainCamera.Exists())
				{
					Logger.Warn("UpdateFade (PositionSelector): Camera not available");
					this._fadeState = FadeState.None;
					return;
				}

				if (this._fadeState == FadeState.FadingOut)
				{
					if (Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT))
					{
						try
						{
							this.MainCamera.IsActive = true;
							World.RenderingCamera = this.MainCamera;
							this._fadeState = FadeState.Activating;
							Function.Call(Hash.DO_SCREEN_FADE_IN, new InputArgument[] { 800 });
						}
						catch (Exception ex)
						{
							Logger.Error(ex, "UpdateFade: Error activating position selector camera");
							this._fadeState = FadeState.None;
						}
					}
				}
				else if (this._fadeState == FadeState.Activating)
				{
					if (Function.Call<bool>(Hash.IS_SCREEN_FADED_IN))
					{
						this._fadeState = FadeState.None;
					}
				}
				else if (this._fadeState == FadeState.FadingOutExit)
				{
					if (Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT))
					{
						try
						{
							this.MainCamera.IsActive = false;
							World.RenderingCamera = null;
							this._fadeState = FadeState.Deactivating;
							Function.Call(Hash.DO_SCREEN_FADE_IN, new InputArgument[] { 800 });
						}
						catch (Exception ex)
						{
							Logger.Error(ex, "UpdateFade: Error deactivating position selector camera");
							this._fadeState = FadeState.None;
						}
					}
				}
				else if (this._fadeState == FadeState.Deactivating)
				{
					if (Function.Call<bool>(Hash.IS_SCREEN_FADED_IN))
					{
						this._fadeState = FadeState.None;
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "UpdateFade (PositionSelector): Unexpected error");
				this._fadeState = FadeState.None;
			}
		}


		public void Update()
		{
			try
			{
				// Update fade state machine first
				this.UpdateFade();
				
				if (this._mainCamera == null || !this._mainCamera.Exists())
				{
					Logger.Warn("PositionSelector.Update: Camera not available");
					return;
				}

				bool isActive = this.MainCamera.IsActive;
				if (isActive)
				{
					bool shouldRender = this._renderSceneTimer.Enabled && this._renderSceneTimer.Check();
					if (shouldRender)
					{
						try
						{
							// SET_FOCUS_AREA native
							Function.Call((Hash)658611830838489950L, new InputArgument[]
							{
								this._mainCamera.Position.X,
								this._mainCamera.Position.Y,
								this._mainCamera.Position.Z
							});
							this._renderSceneTimer.Reset();
						}
						catch (Exception ex)
						{
							Logger.Debug("SET_FOCUS_AREA native warning: " + ex.Message);
						}
					}

					// Controls are now disabled once at EnterCameraView and re-enabled at ExitCameraView
					// No need to call DISABLE_CONTROL_ACTION every frame

					// Rendering functions
					try
					{
						Function.Call((Hash)8187532053442985248L, new InputArgument[0]);
					}
					catch (Exception ex)
					{
						Logger.Debug("Rendering native #1 warning: " + ex.Message);
					}

					try
					{
						Function.Call((Hash)7495895348773880760L, new InputArgument[] { 18 });
					}
					catch (Exception ex)
					{
						Logger.Debug("Rendering native #2 warning: " + ex.Message);
					}

					// Draw position marker
					try
					{
						Vector3 vector = Vector3.Subtract(this._mainCamera.Position, this._previousPos);
						Function.Call(unchecked((Hash)(-4939229729199161819L)), new InputArgument[]
						{
							this._mainCamera.Position.X,
							this._mainCamera.Position.Y,
							this._mainCamera.Position.Z,
							vector.X,
							vector.Y,
							vector.Z
						});
					}
					catch (Exception ex)
					{
						Logger.Debug("Draw marker warning: " + ex.Message);
					}

					this._previousPos = this._mainCamera.Position;
					this.RenderEntityPosition();
					
					try
					{
						this.GamepadHandler.Update();
					}
					catch (Exception ex)
					{
						Logger.Debug("GamepadHandler.Update warning: " + ex.Message);
					}

					try
					{
						this.RenderIntructionalButtons();
					}
					catch (Exception ex)
					{
						Logger.Debug("RenderIntructionalButtons warning: " + ex.Message);
					}

					bool flag2 = this._currentLerpTime > 0f;
					if (flag2)
					{
						this._currentLerpTime -= 0.01f;
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "PositionSelector.Update: Critical error");
			}
		}


		private void RenderEntityPosition()
		{
			Vector3 position = Game.Player.Character.Position + Game.Player.Character.UpVector * 1.8f;
			Vector3 worldDown = Vector3.WorldDown;
			Vector3 rotation = new Vector3(90f, 0f, 0f);
			Vector3 scale3D = new Vector3(2f, 2f, 2f);
			Color yellow = Color.Yellow;
			this.DrawMarker(20, position, worldDown, rotation, scale3D, yellow, true, false, false);
		}


		private void DrawMarker(int type, Vector3 position, Vector3 direction, Vector3 rotation, Vector3 scale3D, Color color, bool animate = false, bool faceCam = false, bool rotate = false)
		{
			Function.Call((Hash)2902427857584726153L, new InputArgument[]
			{
				type,
				position.X,
				position.Y,
				position.Z,
				direction.X,
				direction.Y,
				direction.Z,
				rotation.X,
				rotation.Y,
				rotation.Z,
				scale3D.X,
				scale3D.Y,
				scale3D.Z,
				(int)color.R,
				(int)color.G,
				(int)color.B,
				(int)color.A,
				animate,
				faceCam,
				2,
				rotate,
				0,
				0,
				0
			});
		}


		private void RenderIntructionalButtons()
		{
			this._instructionalButtons.CallFunction("CLEAR_ALL", new object[0]);
			this._instructionalButtons.CallFunction("TOGGLE_MOUSE_BUTTONS", new object[]
			{
				false
			});
			string text = Function.Call<string>((Hash)331533201183454215L, new InputArgument[]
			{
				2,
				24,
				0
			});
			this._instructionalButtons.CallFunction("SET_DATA_SLOT", new object[]
			{
				4,
				text,
				"Select Position"
			});
			text = Function.Call<string>((Hash)331533201183454215L, new InputArgument[]
			{
				3,
				17,
				0
			});
			this._instructionalButtons.CallFunction("SET_DATA_SLOT", new object[]
			{
				3,
				text,
				"Increase Duration"
			});
			text = Function.Call<string>((Hash)331533201183454215L, new InputArgument[]
			{
				1,
				16,
				0
			});
			this._instructionalButtons.CallFunction("SET_DATA_SLOT", new object[]
			{
				2,
				text,
				"Decrease Duration"
			});
			text = Function.Call<string>((Hash)331533201183454215L, new InputArgument[]
			{
				2,
				25,
				0
			});
			this._instructionalButtons.CallFunction("SET_DATA_SLOT", new object[]
			{
				1,
				text,
				"Exit"
			});
			string[] array = new string[]
			{
				Function.Call<string>((Hash)331533201183454215L, new InputArgument[]
				{
					2,
					32,
					0
				}),
				Function.Call<string>((Hash)331533201183454215L, new InputArgument[]
				{
					2,
					34,
					0
				}),
				Function.Call<string>((Hash)331533201183454215L, new InputArgument[]
				{
					2,
					33,
					0
				}),
				Function.Call<string>((Hash)331533201183454215L, new InputArgument[]
				{
					2,
					35,
					0
				})
			};
			this._instructionalButtons.CallFunction("SET_DATA_SLOT", new object[]
			{
				0,
				array[3],
				array[2],
				array[1],
				array[0],
				"Move"
			});
			this._instructionalButtons.CallFunction("SET_BACKGROUND_COLOUR", new object[]
			{
				0,
				0,
				0,
				80
			});
			this._instructionalButtons.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", new object[]
			{
				0
			});
			this._instructionalButtons.Render2D();
		}


		private Timer _renderSceneTimer;


		private float _currentLerpTime;


		private Scaleform _instructionalButtons;


		private Vector3 _previousPos;


		private Camera _mainCamera;


		private readonly float LerpTime = 0.5f;


		private readonly float RotationSpeed = 0.7f;


		public GamepadHandler GamepadHandler; // Removed readonly to allow Dispose cleanup
	}
}
