using System;

namespace ModdedCamera.Input_Handlers
{
	// Token: 0x0200000C RID: 12
	public class ButtonPressedEventArgs : EventArgs
	{
		// Token: 0x06000079 RID: 121 RVA: 0x00005164 File Offset: 0x00003364
		public ButtonPressedEventArgs(int value)
		{
			this._value = value;
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x0600007A RID: 122 RVA: 0x00005178 File Offset: 0x00003378
		public int Value
		{
			get
			{
				return this._value;
			}
		}

		// Token: 0x04000031 RID: 49
		private int _value;
	}
}
