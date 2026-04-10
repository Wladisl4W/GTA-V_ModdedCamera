using System;

namespace ModdedCamera.Input_Handlers
{
	// Token: 0x0200000B RID: 11
	public class AnalogStickChangedEventArgs : EventArgs
	{
		// Token: 0x06000076 RID: 118 RVA: 0x0000511A File Offset: 0x0000331A
		public AnalogStickChangedEventArgs(int x, int y)
		{
			this._x = x;
			this._y = y;
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000077 RID: 119 RVA: 0x00005134 File Offset: 0x00003334
		public int X
		{
			get
			{
				return this._x;
			}
		}

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000078 RID: 120 RVA: 0x0000514C File Offset: 0x0000334C
		public int Y
		{
			get
			{
				return this._y;
			}
		}

		// Token: 0x0400002F RID: 47
		private int _x;

		// Token: 0x04000030 RID: 48
		private int _y;
	}
}
