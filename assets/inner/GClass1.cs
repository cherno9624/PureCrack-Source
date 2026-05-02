using System;
using System.IO;
using ProtoBuf;

// Token: 0x02000016 RID: 22
public static class GClass1
{
	// Token: 0x0600006E RID: 110 RVA: 0x000097B4 File Offset: 0x000079B4
	public static byte[] smethod_0(GClass2 object_1)
	{
		byte[] result;
		using (MemoryStream memoryStream = new MemoryStream())
		{
			Serializer.Serialize<GClass2>(memoryStream, object_1);
			memoryStream.Position = 0L;
			result = GClass14.smethod_0(memoryStream.ToArray());
		}
		return result;
	}

	// Token: 0x0600006F RID: 111 RVA: 0x00009808 File Offset: 0x00007A08
	public static GClass2 smethod_1(byte[] byte_0)
	{
		GClass2 result;
		using (MemoryStream memoryStream = new MemoryStream(GClass14.smethod_1(byte_0)))
		{
			memoryStream.Position = 0L;
			result = Serializer.Deserialize<GClass2>(memoryStream);
		}
		return result;
	}

	// Token: 0x06000070 RID: 112 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static GClass1()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x06000071 RID: 113 RVA: 0x0000250D File Offset: 0x0000070D
	internal static bool smethod_2()
	{
		return GClass1.object_0 == null;
	}

	// Token: 0x0400003F RID: 63
	internal static object object_0;
}
