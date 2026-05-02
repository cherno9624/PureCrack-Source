using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ProtoBuf;

// Token: 0x02000017 RID: 23
[ProtoContract]
public class GClass3 : GClass2
{
	// Token: 0x17000001 RID: 1
	// (get) Token: 0x06000072 RID: 114 RVA: 0x00002517 File Offset: 0x00000717
	// (set) Token: 0x06000073 RID: 115 RVA: 0x0000251F File Offset: 0x0000071F
	[ProtoMember(1)]
	public List<string> List_0 { get; set; }

	// Token: 0x17000002 RID: 2
	// (get) Token: 0x06000074 RID: 116 RVA: 0x00002528 File Offset: 0x00000728
	// (set) Token: 0x06000075 RID: 117 RVA: 0x00002530 File Offset: 0x00000730
	[ProtoMember(2)]
	public List<int> List_1 { get; set; }

	public GClass3()
	{
		List_0 = new List<string>();
		List_1 = new List<int>();
	}

	// Token: 0x17000003 RID: 3
	// (get) Token: 0x06000076 RID: 118 RVA: 0x00002539 File Offset: 0x00000739
	// (set) Token: 0x06000077 RID: 119 RVA: 0x00002541 File Offset: 0x00000741
	[ProtoMember(3)]
	public string String_0 { get; set; }

	// Token: 0x17000004 RID: 4
	// (get) Token: 0x06000078 RID: 120 RVA: 0x0000254A File Offset: 0x0000074A
	// (set) Token: 0x06000079 RID: 121 RVA: 0x00002552 File Offset: 0x00000752
	[ProtoMember(4)]
	public string String_1 { get; set; }

	// Token: 0x17000005 RID: 5
	// (get) Token: 0x0600007A RID: 122 RVA: 0x0000255B File Offset: 0x0000075B
	// (set) Token: 0x0600007B RID: 123 RVA: 0x00002563 File Offset: 0x00000763
	[ProtoMember(5)]
	public bool Boolean_0 { get; set; }

	// Token: 0x17000006 RID: 6
	// (get) Token: 0x0600007C RID: 124 RVA: 0x0000256C File Offset: 0x0000076C
	// (set) Token: 0x0600007D RID: 125 RVA: 0x00002574 File Offset: 0x00000774
	[ProtoMember(6)]
	public bool Boolean_1 { get; set; }

	// Token: 0x17000007 RID: 7
	// (get) Token: 0x0600007E RID: 126 RVA: 0x0000257D File Offset: 0x0000077D
	// (set) Token: 0x0600007F RID: 127 RVA: 0x00002585 File Offset: 0x00000785
	[ProtoMember(7)]
	public string String_2 { get; set; }

	// Token: 0x17000008 RID: 8
	// (get) Token: 0x06000080 RID: 128 RVA: 0x0000258E File Offset: 0x0000078E
	// (set) Token: 0x06000081 RID: 129 RVA: 0x00002596 File Offset: 0x00000796
	[ProtoMember(8)]
	public string String_3 { get; set; }

	// Token: 0x17000009 RID: 9
	// (get) Token: 0x06000082 RID: 130 RVA: 0x0000259F File Offset: 0x0000079F
	// (set) Token: 0x06000083 RID: 131 RVA: 0x000025A7 File Offset: 0x000007A7
	[ProtoMember(9)]
	public string stadrmoOn1 { get; set; }

	// Token: 0x1700000A RID: 10
	// (get) Token: 0x06000084 RID: 132 RVA: 0x000025B0 File Offset: 0x000007B0
	// (set) Token: 0x06000085 RID: 133 RVA: 0x000025B8 File Offset: 0x000007B8
	[ProtoMember(10)]
	public bool Boolean_2 { get; set; }

	// Token: 0x06000087 RID: 135 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static GClass3()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x06000088 RID: 136 RVA: 0x000025DF File Offset: 0x000007DF
	internal static bool smethod_1()
	{
		return GClass3.object_1 == null;
	}

	// Token: 0x04000040 RID: 64
	[CompilerGenerated]
	private List<string> list_0;

	// Token: 0x04000041 RID: 65
	[CompilerGenerated]
	private List<int> list_1;

	// Token: 0x04000042 RID: 66
	[CompilerGenerated]
	private string string_0;

	// Token: 0x04000043 RID: 67
	[CompilerGenerated]
	private string string_1;

	// Token: 0x04000044 RID: 68
	[CompilerGenerated]
	private bool bool_0;

	// Token: 0x04000045 RID: 69
	[CompilerGenerated]
	private bool bool_1;

	// Token: 0x04000046 RID: 70
	[CompilerGenerated]
	private string string_2;

	// Token: 0x04000047 RID: 71
	[CompilerGenerated]
	private string string_3;

	// Token: 0x04000048 RID: 72
	[CompilerGenerated]
	private string string_4;

	// Token: 0x04000049 RID: 73
	[CompilerGenerated]
	private bool bool_2;

	// Token: 0x0400004A RID: 74
	private static object object_1;
}
