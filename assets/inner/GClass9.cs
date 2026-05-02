using System;
using System.Runtime.CompilerServices;
using ProtoBuf;

// Token: 0x0200001D RID: 29
[ProtoContract]
public class GClass9 : GClass2
{
	// Token: 0x17000029 RID: 41
	// (get) Token: 0x060000D4 RID: 212 RVA: 0x00002821 File Offset: 0x00000A21
	// (set) Token: 0x060000D5 RID: 213 RVA: 0x00002829 File Offset: 0x00000A29
	[ProtoMember(1)]
	public string String_0 { get; set; }

	// Token: 0x060000D7 RID: 215 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static GClass9()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x060000D8 RID: 216 RVA: 0x00002832 File Offset: 0x00000A32
	internal static bool smethod_1()
	{
		return GClass9.object_1 == null;
	}

	// Token: 0x0400006E RID: 110
	[CompilerGenerated]
	private string string_0;

	// Token: 0x0400006F RID: 111
	internal static object object_1;
}
