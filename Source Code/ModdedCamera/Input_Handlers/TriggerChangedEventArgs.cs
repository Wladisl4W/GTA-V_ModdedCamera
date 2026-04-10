using System;

namespace ModdedCamera.Input_Handlers
{
	// Token: 0x0200000D RID: 13
	public class TriggerChangedEventArgs : EventArgs
	{
		// Token: 0x0600007B RID: 123 RVA: 0x00005190 File Offset: 0x00003390
		public TriggerChangedEventArgs(int value)
		{
			this._value = value;
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x0600007C RID: 124 RVA: 0x000051A4 File Offset: 0x000033A4
		public int Value
		{
			get
			{
				return this._value;
			}
		}

		// Token: 0x04000032 RID: 50
		private int _value;
	}
}
