using System;
using System.Runtime.CompilerServices;
using ProtoBuf;

// Token: 0x0200001E RID: 30
[ProtoContract]
public class GClass10 : GClass2
{
	// Token: 0x1700002A RID: 42
	// (get) Token: 0x060000D9 RID: 217 RVA: 0x0000283C File Offset: 0x00000A3C
	// (set) Token: 0x060000DA RID: 218 RVA: 0x00002844 File Offset: 0x00000A44
	[ProtoMember(1)]
	public GClass11 GClass11_0 { get; set; }

	// Token: 0x1700002B RID: 43
	// (get) Token: 0x060000DB RID: 219 RVA: 0x0000284D File Offset: 0x00000A4D
	// (set) Token: 0x060000DC RID: 220 RVA: 0x00002855 File Offset: 0x00000A55
	[ProtoMember(2)]
	public bool Boolean_0 { get; set; }

	// Token: 0x1700002C RID: 44
	// (get) Token: 0x060000DD RID: 221 RVA: 0x0000285E File Offset: 0x00000A5E
	// (set) Token: 0x060000DE RID: 222 RVA: 0x00002866 File Offset: 0x00000A66
	[ProtoMember(3)]
	public string String_0 { get; set; }

	// Token: 0x060000E0 RID: 224 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static GClass10()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x060000E1 RID: 225 RVA: 0x0000286F File Offset: 0x00000A6F
	internal static bool smethod_1()
	{
		return GClass10.object_1 == null;
	}

	// Token: 0x04000070 RID: 112
	[CompilerGenerated]
	private GClass11 gclass11_0;

	// Token: 0x04000071 RID: 113
	[CompilerGenerated]
	private bool bool_0;

	// Token: 0x04000072 RID: 114
	[CompilerGenerated]
	private string string_0;

	// Token: 0x04000073 RID: 115
	private static object object_1;
}
