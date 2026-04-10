using System;
using GTA.Native;
using ModdedCamera.Input_Handlers;

namespace ModdedCamera.Gamepad
{
	// Token: 0x02000006 RID: 6
	public class GamepadHandler
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x06000017 RID: 23 RVA: 0x00002124 File Offset: 0x00000324
		// (remove) Token: 0x06000018 RID: 24 RVA: 0x0000215C File Offset: 0x0000035C
		public event ButtonPressedEventHandler AButtonPressed;

		// Token: 0x14000002 RID: 2
		// (add) Token: 0x06000019 RID: 25 RVA: 0x00002194 File Offset: 0x00000394
		// (remove) Token: 0x0600001A RID: 26 RVA: 0x000021CC File Offset: 0x000003CC
		public event ButtonPressedEventHandler BButtonPressed;

		// Token: 0x14000003 RID: 3
		// (add) Token: 0x0600001B RID: 27 RVA: 0x00002204 File Offset: 0x00000404
		// (remove) Token: 0x0600001C RID: 28 RVA: 0x0000223C File Offset: 0x0000043C
		public event ButtonPressedEventHandler XButtonPressed;

		// Token: 0x14000004 RID: 4
		// (add) Token: 0x0600001D RID: 29 RVA: 0x00002274 File Offset: 0x00000474
		// (remove) Token: 0x0600001E RID: 30 RVA: 0x000022AC File Offset: 0x000004AC
		public event ButtonPressedEventHandler YButtonPressed;

		// Token: 0x14000005 RID: 5
		// (add) Token: 0x0600001F RID: 31 RVA: 0x000022E4 File Offset: 0x000004E4
		// (remove) Token: 0x06000020 RID: 32 RVA: 0x0000231C File Offset: 0x0000051C
		public event TriggerChangedEventHandler RightTriggerChanged;

		// Token: 0x14000006 RID: 6
		// (add) Token: 0x06000021 RID: 33 RVA: 0x00002354 File Offset: 0x00000554
		// (remove) Token: 0x06000022 RID: 34 RVA: 0x0000238C File Offset: 0x0000058C
		public event TriggerChangedEventHandler LeftTriggerChanged;

		// Token: 0x14000007 RID: 7
		// (add) Token: 0x06000023 RID: 35 RVA: 0x000023C4 File Offset: 0x000005C4
		// (remove) Token: 0x06000024 RID: 36 RVA: 0x000023FC File Offset: 0x000005FC
		public event ButtonPressedEventHandler RightBumperPressed;

		// Token: 0x14000008 RID: 8
		// (add) Token: 0x06000025 RID: 37 RVA: 0x00002434 File Offset: 0x00000634
		// (remove) Token: 0x06000026 RID: 38 RVA: 0x0000246C File Offset: 0x0000066C
		public event ButtonPressedEventHandler LeftBumperPressed;









		// Token: 0x1400000D RID: 13
		// (add) Token: 0x0600002F RID: 47 RVA: 0x00002664 File Offset: 0x00000864
		// (remove) Token: 0x06000030 RID: 48 RVA: 0x0000269C File Offset: 0x0000089C
		public event AnalogStickChangedEventHandler LeftStickChanged;

		// Token: 0x1400000E RID: 14
		// (add) Token: 0x06000031 RID: 49 RVA: 0x000026D4 File Offset: 0x000008D4
		// (remove) Token: 0x06000032 RID: 50 RVA: 0x0000270C File Offset: 0x0000090C
		public event AnalogStickChangedEventHandler RightStickChanged;

		// Token: 0x1400000F RID: 15
		// (add) Token: 0x06000033 RID: 51 RVA: 0x00002744 File Offset: 0x00000944
		// (remove) Token: 0x06000034 RID: 52 RVA: 0x0000277C File Offset: 0x0000097C
		public event ButtonPressedEventHandler LeftStickPressed;

		// Token: 0x14000010 RID: 16
		// (add) Token: 0x06000035 RID: 53 RVA: 0x000027B4 File Offset: 0x000009B4
		// (remove) Token: 0x06000036 RID: 54 RVA: 0x000027EC File Offset: 0x000009EC
		public event ButtonPressedEventHandler RightStickPressed;

		// Token: 0x06000038 RID: 56 RVA: 0x0000282C File Offset: 0x00000A2C
		public void Update()
		{
			bool flag = this.GetControlValue(220) != 127 || this.GetControlValue(221) != 127;
			if (flag)
			{
				this.OnRightStickChanged(new AnalogStickChangedEventArgs(this.GetControlValue(220), this.GetControlValue(221)));
			}
			bool flag2 = this.GetControlValue(218) != 127 || this.GetControlValue(219) != 127;
			if (flag2)
			{
				this.OnLeftStickChanged(new AnalogStickChangedEventArgs(this.GetControlValue(218), this.GetControlValue(219)));
			}
			bool controlInput = this.GetControlInput(230);
			if (controlInput)
			{
				this.OnLeftStickPressed(new ButtonPressedEventArgs(this.GetControlValue(230)));
			}
			bool controlInput2 = this.GetControlInput(231);
			if (controlInput2)
			{
				this.OnRightStickPressed(new ButtonPressedEventArgs(this.GetControlValue(231)));
			}
			bool flag3 = this.GetControlValue(229) > 127;
			if (flag3)
			{
				this.OnRightTriggerChanged(new TriggerChangedEventArgs(this.GetControlValue(229)));
			}
			bool flag4 = this.GetControlValue(228) > 127;
			if (flag4)
			{
				this.OnLeftTriggerChanged(new TriggerChangedEventArgs(this.GetControlValue(228)));
			}
			bool controlInput3 = this.GetControlInput(222);
			if (controlInput3)
			{
				this.OnYPressed(new ButtonPressedEventArgs(this.GetControlValue(222)));
			}
			bool controlInput4 = this.GetControlInput(223);
			if (controlInput4)
			{
				this.OnAPressed(new ButtonPressedEventArgs(this.GetControlValue(223)));
			}
			bool controlInput5 = this.GetControlInput(224);
			if (controlInput5)
			{
				this.OnXPressed(new ButtonPressedEventArgs(this.GetControlValue(224)));
			}
			bool controlInput6 = this.GetControlInput(225);
			if (controlInput6)
			{
				this.OnBPressed(new ButtonPressedEventArgs(this.GetControlValue(225)));
			}
			bool controlInput7 = this.GetControlInput(226);
			if (controlInput7)
			{
				this.OnLBPressed(new ButtonPressedEventArgs(this.GetControlValue(226)));
			}
			bool controlInput8 = this.GetControlInput(227);
			if (controlInput8)
			{
				this.OnRBPressed(new ButtonPressedEventArgs(this.GetControlValue(227)));
			}
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00002A62 File Offset: 0x00000C62
		protected virtual void OnAPressed(ButtonPressedEventArgs e)
		{
			ButtonPressedEventHandler abuttonPressed = this.AButtonPressed;
			if (abuttonPressed != null)
			{
				abuttonPressed(this, e);
			}
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00002A79 File Offset: 0x00000C79
		protected virtual void OnBPressed(ButtonPressedEventArgs e)
		{
			ButtonPressedEventHandler bbuttonPressed = this.BButtonPressed;
			if (bbuttonPressed != null)
			{
				bbuttonPressed(this, e);
			}
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00002A90 File Offset: 0x00000C90
		protected virtual void OnXPressed(ButtonPressedEventArgs e)
		{
			ButtonPressedEventHandler xbuttonPressed = this.XButtonPressed;
			if (xbuttonPressed != null)
			{
				xbuttonPressed(this, e);
			}
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00002AA7 File Offset: 0x00000CA7
		protected virtual void OnYPressed(ButtonPressedEventArgs e)
		{
			ButtonPressedEventHandler ybuttonPressed = this.YButtonPressed;
			if (ybuttonPressed != null)
			{
				ybuttonPressed(this, e);
			}
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00002ABE File Offset: 0x00000CBE
		protected virtual void OnLBPressed(ButtonPressedEventArgs e)
		{
			ButtonPressedEventHandler leftBumperPressed = this.LeftBumperPressed;
			if (leftBumperPressed != null)
			{
				leftBumperPressed(this, e);
			}
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00002AD5 File Offset: 0x00000CD5
		protected virtual void OnRBPressed(ButtonPressedEventArgs e)
		{
			ButtonPressedEventHandler rightBumperPressed = this.RightBumperPressed;
			if (rightBumperPressed != null)
			{
				rightBumperPressed(this, e);
			}
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00002AEC File Offset: 0x00000CEC
		protected virtual void OnLeftTriggerChanged(TriggerChangedEventArgs e)
		{
			TriggerChangedEventHandler leftTriggerChanged = this.LeftTriggerChanged;
			if (leftTriggerChanged != null)
			{
				leftTriggerChanged(this, e);
			}
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00002B03 File Offset: 0x00000D03
		protected virtual void OnRightTriggerChanged(TriggerChangedEventArgs e)
		{
			TriggerChangedEventHandler rightTriggerChanged = this.RightTriggerChanged;
			if (rightTriggerChanged != null)
			{
				rightTriggerChanged(this, e);
			}
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00002B1A File Offset: 0x00000D1A
		protected virtual void OnLeftStickChanged(AnalogStickChangedEventArgs e)
		{
			AnalogStickChangedEventHandler leftStickChanged = this.LeftStickChanged;
			if (leftStickChanged != null)
			{
				leftStickChanged(this, e);
			}
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00002B31 File Offset: 0x00000D31
		protected virtual void OnRightStickChanged(AnalogStickChangedEventArgs e)
		{
			AnalogStickChangedEventHandler rightStickChanged = this.RightStickChanged;
			if (rightStickChanged != null)
			{
				rightStickChanged(this, e);
			}
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00002B48 File Offset: 0x00000D48
		protected virtual void OnLeftStickPressed(ButtonPressedEventArgs e)
		{
			ButtonPressedEventHandler leftStickPressed = this.LeftStickPressed;
			if (leftStickPressed != null)
			{
				leftStickPressed(this, e);
			}
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00002B5F File Offset: 0x00000D5F
		protected virtual void OnRightStickPressed(ButtonPressedEventArgs e)
		{
			ButtonPressedEventHandler rightStickPressed = this.RightStickPressed;
			if (rightStickPressed != null)
			{
				rightStickPressed(this, e);
			}
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00002B78 File Offset: 0x00000D78
		private bool GetControlInput(int control)
		{
			return Function.Call<bool>((Hash)6342219533232326959L, new InputArgument[]
			{
				0,
				control
			});
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00002BB0 File Offset: 0x00000DB0
		private int GetControlValue(int control)
		{
			return Function.Call<int>(unchecked((Hash)(-2783653480577029081L)), new InputArgument[]
			{
				0,
				control
			});
		}

		// Dispose method to clean up resources
		public void Dispose()
		{
			// Note: Events in C# can only be set from within the class that declares them
			// Setting them to null here is correct since we're in the declaring class
			// This removes all subscribers
			this.AButtonPressed = null;
			this.BButtonPressed = null;
			this.XButtonPressed = null;
			this.YButtonPressed = null;
			this.LeftStickPressed = null;
			this.RightStickPressed = null;
			this.LeftStickChanged = null;
			this.RightStickChanged = null;
			this.LeftTriggerChanged = null;
			this.RightTriggerChanged = null;
		}
	}
}
