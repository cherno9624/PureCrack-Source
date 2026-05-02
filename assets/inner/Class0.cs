using System;
using System.Runtime.InteropServices;
using System.Text;

// Token: 0x02000003 RID: 3
internal static class Class0
{
	// Token: 0x06000009 RID: 9
	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern Class0.Enum0 SetThreadExecutionState(Class0.Enum0 enum0_0);

	// Token: 0x0600000A RID: 10
	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern IntPtr GetForegroundWindow();

	// Token: 0x0600000B RID: 11
	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern int GetWindowText(IntPtr intptr_0, StringBuilder stringBuilder_0, int int_0);

	// Token: 0x0600000C RID: 12
	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern int GetWindowTextLength(IntPtr a);

	// Token: 0x0600000D RID: 13
	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool SetProcessDPIAware();

	// Token: 0x0600000E RID: 14
	[DllImport("user32.dll")]
	public static extern bool GetLastInputInfo(ref Class0.Struct0 struct0_0);

	// Token: 0x0600000F RID: 15 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static Class0()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x06000010 RID: 16 RVA: 0x00002319 File Offset: 0x00000519
	internal static bool smethod_0()
	{
		return Class0.object_0 == null;
	}

	// Token: 0x04000003 RID: 3
	private static object object_0;

	// Token: 0x02000004 RID: 4
	public enum Enum0 : uint
	{
		// Token: 0x04000005 RID: 5
		const_0 = 2147483648U,
		// Token: 0x04000006 RID: 6
		const_1 = 2U,
		// Token: 0x04000007 RID: 7
		const_2 = 1U
	}

	// Token: 0x02000005 RID: 5
	public struct Struct0
	{
		// Token: 0x04000008 RID: 8
		public uint uint_0;

		// Token: 0x04000009 RID: 9
		public uint uint_1;
	}
}
