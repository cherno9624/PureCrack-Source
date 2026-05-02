using System;
using System.Runtime.CompilerServices;
using ProtoBuf;

// Token: 0x0200001C RID: 28
[ProtoContract]
public class GClass8 : GClass2
{
	// Token: 0x17000023 RID: 35
	// (get) Token: 0x060000C5 RID: 197 RVA: 0x000027B1 File Offset: 0x000009B1
	// (set) Token: 0x060000C6 RID: 198 RVA: 0x000027B9 File Offset: 0x000009B9
	[ProtoMember(1)]
	public int Int32_0 { get; set; }

	// Token: 0x17000024 RID: 36
	// (get) Token: 0x060000C7 RID: 199 RVA: 0x000027C2 File Offset: 0x000009C2
	// (set) Token: 0x060000C8 RID: 200 RVA: 0x000027CA File Offset: 0x000009CA
	[ProtoMember(2)]
	public bool HasValue { get; set; }

	// Token: 0x17000025 RID: 37
	// (get) Token: 0x060000C9 RID: 201 RVA: 0x000027D3 File Offset: 0x000009D3
	// (set) Token: 0x060000CA RID: 202 RVA: 0x000027DB File Offset: 0x000009DB
	[ProtoMember(3)]
	public int Int32_1 { get; set; }

	// Token: 0x17000026 RID: 38
	// (get) Token: 0x060000CB RID: 203 RVA: 0x000027E4 File Offset: 0x000009E4
	// (set) Token: 0x060000CC RID: 204 RVA: 0x000027EC File Offset: 0x000009EC
	[ProtoMember(4)]
	public string String_0 { get; set; }

	// Token: 0x17000027 RID: 39
	// (get) Token: 0x060000CD RID: 205 RVA: 0x000027F5 File Offset: 0x000009F5
	// (set) Token: 0x060000CE RID: 206 RVA: 0x000027FD File Offset: 0x000009FD
	[ProtoMember(5)]
	public string String_1 { get; set; }

	// Token: 0x17000028 RID: 40
	// (get) Token: 0x060000CF RID: 207 RVA: 0x00002806 File Offset: 0x00000A06
	// (set) Token: 0x060000D0 RID: 208 RVA: 0x0000280E File Offset: 0x00000A0E
	[ProtoMember(6)]
	public byte[] Byte_0 { get; set; }

	// Token: 0x060000D2 RID: 210 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static GClass8()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x060000D3 RID: 211 RVA: 0x00002817 File Offset: 0x00000A17
	internal static bool smethod_1()
	{
		return GClass8.object_1 == null;
	}

	// Token: 0x04000067 RID: 103
	[CompilerGenerated]
	private int int_0;

	// Token: 0x04000068 RID: 104
	[CompilerGenerated]
	private bool bool_0;

	// Token: 0x04000069 RID: 105
	[CompilerGenerated]
	private int int_1;

	// Token: 0x0400006A RID: 106
	[CompilerGenerated]
	private string string_0;

	// Token: 0x0400006B RID: 107
	[CompilerGenerated]
	private string string_1;

	// Token: 0x0400006C RID: 108
	[CompilerGenerated]
	private byte[] byte_0;

	// Token: 0x0400006D RID: 109
	internal static object object_1;
}
