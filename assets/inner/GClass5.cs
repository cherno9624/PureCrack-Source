using System;
using System.Runtime.CompilerServices;
using ProtoBuf;

// Token: 0x02000019 RID: 25
[ProtoContract]
public class GClass5 : GClass2
{
	// Token: 0x1700001F RID: 31
	// (get) Token: 0x060000B4 RID: 180 RVA: 0x0000274F File Offset: 0x0000094F
	// (set) Token: 0x060000B5 RID: 181 RVA: 0x00002757 File Offset: 0x00000957
	[ProtoMember(3)]
	public byte[] Byte_0 { get; set; }

	// Token: 0x060000B7 RID: 183 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static GClass5()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x060000B8 RID: 184 RVA: 0x00002760 File Offset: 0x00000960
	internal static bool smethod_1()
	{
		return GClass5.object_1 == null;
	}

	// Token: 0x04000060 RID: 96
	[CompilerGenerated]
	private byte[] byte_0;

	// Token: 0x04000061 RID: 97
	internal static object object_1;
}
