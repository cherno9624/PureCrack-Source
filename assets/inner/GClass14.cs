using System;
using System.IO;
using System.IO.Compression;

// Token: 0x02000024 RID: 36
public static class GClass14
{
	// Token: 0x060000FB RID: 251 RVA: 0x00009890 File Offset: 0x00007A90
	public static byte[] smethod_0(byte[] byte_0)
	{
		byte[] result;
		using (MemoryStream memoryStream = new MemoryStream())
		{
			using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
			{
				gzipStream.Write(byte_0, 0, byte_0.Length);
				gzipStream.Close();
				result = memoryStream.ToArray();
			}
		}
		return result;
	}

	// Token: 0x060000FC RID: 252 RVA: 0x000098F8 File Offset: 0x00007AF8
	public static byte[] smethod_1(byte[] byte_0)
	{
		byte[] result;
		using (MemoryStream memoryStream = new MemoryStream(byte_0))
		{
			using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
			{
				using (MemoryStream memoryStream2 = new MemoryStream())
				{
					gzipStream.CopyTo(memoryStream2);
					result = memoryStream2.ToArray();
				}
			}
		}
		return result;
	}

	// Token: 0x060000FD RID: 253 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static GClass14()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x060000FE RID: 254 RVA: 0x00002963 File Offset: 0x00000B63
	internal static bool smethod_2()
	{
		return GClass14.object_0 == null;
	}

	// Token: 0x040000A6 RID: 166
	internal static object object_0;
}
