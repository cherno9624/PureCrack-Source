using System;
using System.Security.Cryptography;

// Token: 0x02000021 RID: 33
public class GClass12
{
	// Token: 0x060000EE RID: 238 RVA: 0x00009854 File Offset: 0x00007A54
	private static Random smethod_0()
	{
		if (GClass12.random_0 == null)
		{
			byte[] array = new byte[4];
			GClass12.randomNumberGenerator_0.GetBytes(array);
			GClass12.random_0 = new Random(BitConverter.ToInt32(array, 0));
		}
		return GClass12.random_0;
	}

	// Token: 0x060000EF RID: 239 RVA: 0x000028C0 File Offset: 0x00000AC0
	public int method_0()
	{
		return GClass12.smethod_0().Next();
	}

	// Token: 0x060000F0 RID: 240 RVA: 0x000028CC File Offset: 0x00000ACC
	public int method_1(int int_0)
	{
		return GClass12.smethod_0().Next(int_0);
	}

	// Token: 0x060000F1 RID: 241 RVA: 0x000028D9 File Offset: 0x00000AD9
	public int method_2(int int_0, int int_1)
	{
		return GClass12.smethod_0().Next(int_0, int_1);
	}

	// Token: 0x060000F2 RID: 242 RVA: 0x000028E7 File Offset: 0x00000AE7
	public void method_3(byte[] byte_0)
	{
		GClass12.smethod_0().NextBytes(byte_0);
	}

	// Token: 0x060000F3 RID: 243 RVA: 0x000028F4 File Offset: 0x00000AF4
	public double method_4()
	{
		return GClass12.smethod_0().NextDouble();
	}

	// Token: 0x060000F5 RID: 245 RVA: 0x00002900 File Offset: 0x00000B00
	// Note: this type is marked as 'beforefieldinit'.
	static GClass12()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
		GClass12.randomNumberGenerator_0 = RandomNumberGenerator.Create();
	}

	// Token: 0x060000F6 RID: 246 RVA: 0x00002911 File Offset: 0x00000B11
	internal static bool smethod_1()
	{
		return GClass12.object_0 == null;
	}

	// Token: 0x04000079 RID: 121
	private static readonly RandomNumberGenerator randomNumberGenerator_0;

	// Token: 0x0400007A RID: 122
	[ThreadStatic]
	private static Random random_0;

	// Token: 0x0400007B RID: 123
	private static object object_0;
}
