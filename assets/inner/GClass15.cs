using System;
using System.Security.Cryptography;
using System.Text;

// Token: 0x02000025 RID: 37
public static class GClass15
{
	// Token: 0x060000FF RID: 255 RVA: 0x00009974 File Offset: 0x00007B74
	public static string smethod_0(string string_0)
	{
		MD5CryptoServiceProvider md5CryptoServiceProvider = new MD5CryptoServiceProvider();
		md5CryptoServiceProvider.ComputeHash(Encoding.ASCII.GetBytes(string_0));
		byte[] hash = md5CryptoServiceProvider.Hash;
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < hash.Length; i++)
		{
			stringBuilder.Append(hash[i].ToString("x2"));
		}
		return stringBuilder.ToString();
	}

	// Token: 0x06000100 RID: 256 RVA: 0x000099D0 File Offset: 0x00007BD0
	public static string smethod_1(byte[] byte_0)
	{
		MD5CryptoServiceProvider md5CryptoServiceProvider = new MD5CryptoServiceProvider();
		md5CryptoServiceProvider.ComputeHash(byte_0);
		byte[] hash = md5CryptoServiceProvider.Hash;
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < hash.Length; i++)
		{
			stringBuilder.Append(hash[i].ToString("x2"));
		}
		return stringBuilder.ToString();
	}

	// Token: 0x06000101 RID: 257 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static GClass15()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x06000102 RID: 258 RVA: 0x0000296D File Offset: 0x00000B6D
	internal static bool smethod_2()
	{
		return GClass15.object_0 == null;
	}

	// Token: 0x040000A7 RID: 167
	private static object object_0;
}
