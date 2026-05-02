using System;

// Token: 0x02000022 RID: 34
public static class GClass13
{
	// Token: 0x060000F7 RID: 247 RVA: 0x0000291B File Offset: 0x00000B1B
	public static bool smethod_0(this string string_0)
	{
		return string.IsNullOrEmpty(string_0) || string.IsNullOrWhiteSpace(string_0) || string_0.Length <= 0;
	}

	// Token: 0x060000F8 RID: 248 RVA: 0x0000293D File Offset: 0x00000B3D
	public static string smethod_1(this object object_1, string string_0)
	{
		return string.Join(string_0, new string[]
		{
			object_1.ToString().Trim()
		});
	}

	// Token: 0x060000F9 RID: 249 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static GClass13()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x060000FA RID: 250 RVA: 0x00002959 File Offset: 0x00000B59
	internal static bool smethod_2()
	{
		return GClass13.object_0 == null;
	}

	// Token: 0x0400007C RID: 124
	internal static object object_0;
}
