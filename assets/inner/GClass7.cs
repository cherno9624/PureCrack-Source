using System;
using System.Runtime.CompilerServices;
using ProtoBuf;

// Token: 0x0200001B RID: 27
[ProtoContract]
public class GClass7 : GClass2
{
	// Token: 0x17000020 RID: 32
	// (get) Token: 0x060000BC RID: 188 RVA: 0x00002774 File Offset: 0x00000974
	// (set) Token: 0x060000BD RID: 189 RVA: 0x0000277C File Offset: 0x0000097C
	[ProtoMember(1)]
	public GClass4 GClass4_0 { get; set; }

	// Token: 0x17000021 RID: 33
	// (get) Token: 0x060000BE RID: 190 RVA: 0x00002785 File Offset: 0x00000985
	// (set) Token: 0x060000BF RID: 191 RVA: 0x0000278D File Offset: 0x0000098D
	[ProtoMember(2)]
	public GEnum0 GEnum0_0 { get; set; }

	// Token: 0x17000022 RID: 34
	// (get) Token: 0x060000C0 RID: 192 RVA: 0x00002796 File Offset: 0x00000996
	// (set) Token: 0x060000C1 RID: 193 RVA: 0x0000279E File Offset: 0x0000099E
	[ProtoMember(3)]
	public string zPjUxLdehl { get; set; }

	// Token: 0x060000C3 RID: 195 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static GClass7()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x060000C4 RID: 196 RVA: 0x000027A7 File Offset: 0x000009A7
	internal static bool smethod_1()
	{
		return GClass7.object_1 == null;
	}

	// Token: 0x04000063 RID: 99
	[CompilerGenerated]
	private GClass4 gclass4_0;

	// Token: 0x04000064 RID: 100
	[CompilerGenerated]
	private GEnum0 genum0_0;

	// Token: 0x04000065 RID: 101
	[CompilerGenerated]
	private string string_0;

	// Token: 0x04000066 RID: 102
	internal static object object_1;
}
