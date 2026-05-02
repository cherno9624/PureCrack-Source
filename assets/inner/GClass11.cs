using System;
using System.Runtime.CompilerServices;
using ProtoBuf;

// Token: 0x0200001F RID: 31
[ProtoContract]
public class GClass11 : GClass2
{
	// Token: 0x1700002D RID: 45
	// (get) Token: 0x060000E2 RID: 226 RVA: 0x00002879 File Offset: 0x00000A79
	// (set) Token: 0x060000E3 RID: 227 RVA: 0x00002881 File Offset: 0x00000A81
	[ProtoMember(1)]
	public string String_0 { get; set; }

	// Token: 0x1700002E RID: 46
	// (get) Token: 0x060000E4 RID: 228 RVA: 0x0000288A File Offset: 0x00000A8A
	// (set) Token: 0x060000E5 RID: 229 RVA: 0x00002892 File Offset: 0x00000A92
	[ProtoMember(2)]
	public byte[] Byte_0 { get; set; }

	// Token: 0x1700002F RID: 47
	// (get) Token: 0x060000E6 RID: 230 RVA: 0x0000289B File Offset: 0x00000A9B
	// (set) Token: 0x060000E7 RID: 231 RVA: 0x000028A3 File Offset: 0x00000AA3
	[ProtoMember(3)]
	public string String_1 { get; set; }

	// Token: 0x060000E9 RID: 233 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static GClass11()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x060000EA RID: 234 RVA: 0x000028AC File Offset: 0x00000AAC
	internal static bool smethod_1()
	{
		return GClass11.object_1 == null;
	}

	// Token: 0x04000074 RID: 116
	[CompilerGenerated]
	private string string_0;

	// Token: 0x04000075 RID: 117
	[CompilerGenerated]
	private byte[] byte_0;

	// Token: 0x04000076 RID: 118
	[CompilerGenerated]
	private string string_1;

	// Token: 0x04000077 RID: 119
	private static object object_1;
}
