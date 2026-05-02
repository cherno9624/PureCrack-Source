using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

// Token: 0x020000BF RID: 191
internal class Class12
{
	// Token: 0x06000771 RID: 1905 RVA: 0x000201F8 File Offset: 0x0001E3F8
	static Class12()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
		Class12.bool_4 = false;
		Class12.assembly_0 = typeof(Class12).Assembly;
		Class12.uint_0 = new uint[]
		{
			3614090360U,
			3905402710U,
			606105819U,
			3250441966U,
			4118548399U,
			1200080426U,
			2821735955U,
			4249261313U,
			1770035416U,
			2336552879U,
			4294925233U,
			2304563134U,
			1804603682U,
			4254626195U,
			2792965006U,
			1236535329U,
			4129170786U,
			3225465664U,
			643717713U,
			3921069994U,
			3593408605U,
			38016083U,
			3634488961U,
			3889429448U,
			568446438U,
			3275163606U,
			4107603335U,
			1163531501U,
			2850285829U,
			4243563512U,
			1735328473U,
			2368359562U,
			4294588738U,
			2272392833U,
			1839030562U,
			4259657740U,
			2763975236U,
			1272893353U,
			4139469664U,
			3200236656U,
			681279174U,
			3936430074U,
			3572445317U,
			76029189U,
			3654602809U,
			3873151461U,
			530742520U,
			3299628645U,
			4096336452U,
			1126891415U,
			2878612391U,
			4237533241U,
			1700485571U,
			2399980690U,
			4293915773U,
			2240044497U,
			1873313359U,
			4264355552U,
			2734768916U,
			1309151649U,
			4149444226U,
			3174756917U,
			718787259U,
			3951481745U
		};
		Class12.bool_5 = false;
		Class12.fQgAnroQoI = false;
		Class12.rsacryptoServiceProvider_0 = null;
		Class12.dictionary_0 = null;
		Class12.object_3 = new object();
		Class12.int_2 = 0;
		Class12.object_2 = new object();
		Class12.list_1 = null;
		Class12.list_0 = null;
		Class12.byte_1 = new byte[0];
		Class12.byte_0 = new byte[0];
		Class12.intptr_1 = IntPtr.Zero;
		Class12.intptr_2 = IntPtr.Zero;
		Class12.string_0 = new string[0];
		Class12.int_4 = new int[0];
		Class12.int_5 = 1;
		Class12.bool_3 = false;
		Class12.sortedList_0 = new SortedList();
		Class12.int_3 = 0;
		Class12.long_0 = 0L;
		Class12.object_1 = null;
		Class12.object_0 = null;
		Class12.long_1 = 0L;
		Class12.int_1 = 0;
		Class12.bool_2 = false;
		Class12.bool_1 = false;
		Class12.int_0 = 0;
		Class12.intptr_3 = IntPtr.Zero;
		Class12.bool_0 = false;
		Class12.hashtable_0 = new Hashtable();
		Class12.delegate4_0 = null;
		Class12.delegate5_0 = null;
		Class12.delegate6_0 = null;
		Class12.delegate7_0 = null;
		Class12.delegate8_0 = null;
		Class12.delegate9_0 = null;
		Class12.intptr_0 = IntPtr.Zero;
		try
		{
			RSACryptoServiceProvider.UseMachineKeyStore = true;
		}
		catch
		{
		}
	}

	// Token: 0x06000772 RID: 1906 RVA: 0x000022D0 File Offset: 0x000004D0
	private void method_0()
	{
	}

	// Token: 0x06000773 RID: 1907 RVA: 0x0002037C File Offset: 0x0001E57C
	internal static byte[] smethod_0(byte[] object_4)
	{
		uint[] array = new uint[16];
		uint num = (uint)((448 - object_4.Length * 8 % 512 + 512) % 512);
		if (num == 0U)
		{
			num = 512U;
		}
		uint num2 = (uint)((long)object_4.Length + (long)((ulong)(num / 8U)) + 8L);
		ulong num3 = (ulong)((long)object_4.Length * 8L);
		byte[] array2 = new byte[num2];
		for (int i = 0; i < object_4.Length; i++)
		{
			array2[i] = object_4[i];
		}
		byte[] array3 = array2;
		int num4 = object_4.Length;
		array3[num4] |= 128;
		for (int j = 8; j > 0; j--)
		{
			array2[(int)(checked((IntPtr)(unchecked((ulong)num2 - (ulong)((long)j)))))] = (byte)(num3 >> (8 - j) * 8 & 255UL);
		}
		uint num5 = (uint)(array2.Length * 8 / 32);
		uint num6 = 1732584193U;
		uint num7 = 4023233417U;
		uint num8 = 2562383102U;
		uint num9 = 271733878U;
		for (uint num10 = 0U; num10 < num5 / 16U; num10 += 1U)
		{
			uint num11 = num10 << 6;
			for (uint num12 = 0U; num12 < 61U; num12 += 4U)
			{
				array[(int)(num12 >> 2)] = (uint)((int)array2[(int)(num11 + (num12 + 3U))] << 24 | (int)array2[(int)(num11 + (num12 + 2U))] << 16 | (int)array2[(int)(num11 + (num12 + 1U))] << 8 | (int)array2[(int)(num11 + num12)]);
			}
			uint num13 = num6;
			uint num14 = num7;
			uint num15 = num8;
			uint num16 = num9;
			Class12.dvsYoUdMrG(ref num6, num7, num8, num9, 0U, 7, 1U, array);
			Class12.dvsYoUdMrG(ref num9, num6, num7, num8, 1U, 12, 2U, array);
			Class12.dvsYoUdMrG(ref num8, num9, num6, num7, 2U, 17, 3U, array);
			Class12.dvsYoUdMrG(ref num7, num8, num9, num6, 3U, 22, 4U, array);
			Class12.dvsYoUdMrG(ref num6, num7, num8, num9, 4U, 7, 5U, array);
			Class12.dvsYoUdMrG(ref num9, num6, num7, num8, 5U, 12, 6U, array);
			Class12.dvsYoUdMrG(ref num8, num9, num6, num7, 6U, 17, 7U, array);
			Class12.dvsYoUdMrG(ref num7, num8, num9, num6, 7U, 22, 8U, array);
			Class12.dvsYoUdMrG(ref num6, num7, num8, num9, 8U, 7, 9U, array);
			Class12.dvsYoUdMrG(ref num9, num6, num7, num8, 9U, 12, 10U, array);
			Class12.dvsYoUdMrG(ref num8, num9, num6, num7, 10U, 17, 11U, array);
			Class12.dvsYoUdMrG(ref num7, num8, num9, num6, 11U, 22, 12U, array);
			Class12.dvsYoUdMrG(ref num6, num7, num8, num9, 12U, 7, 13U, array);
			Class12.dvsYoUdMrG(ref num9, num6, num7, num8, 13U, 12, 14U, array);
			Class12.dvsYoUdMrG(ref num8, num9, num6, num7, 14U, 17, 15U, array);
			Class12.dvsYoUdMrG(ref num7, num8, num9, num6, 15U, 22, 16U, array);
			Class12.smethod_1(ref num6, num7, num8, num9, 1U, 5, 17U, array);
			Class12.smethod_1(ref num9, num6, num7, num8, 6U, 9, 18U, array);
			Class12.smethod_1(ref num8, num9, num6, num7, 11U, 14, 19U, array);
			Class12.smethod_1(ref num7, num8, num9, num6, 0U, 20, 20U, array);
			Class12.smethod_1(ref num6, num7, num8, num9, 5U, 5, 21U, array);
			Class12.smethod_1(ref num9, num6, num7, num8, 10U, 9, 22U, array);
			Class12.smethod_1(ref num8, num9, num6, num7, 15U, 14, 23U, array);
			Class12.smethod_1(ref num7, num8, num9, num6, 4U, 20, 24U, array);
			Class12.smethod_1(ref num6, num7, num8, num9, 9U, 5, 25U, array);
			Class12.smethod_1(ref num9, num6, num7, num8, 14U, 9, 26U, array);
			Class12.smethod_1(ref num8, num9, num6, num7, 3U, 14, 27U, array);
			Class12.smethod_1(ref num7, num8, num9, num6, 8U, 20, 28U, array);
			Class12.smethod_1(ref num6, num7, num8, num9, 13U, 5, 29U, array);
			Class12.smethod_1(ref num9, num6, num7, num8, 2U, 9, 30U, array);
			Class12.smethod_1(ref num8, num9, num6, num7, 7U, 14, 31U, array);
			Class12.smethod_1(ref num7, num8, num9, num6, 12U, 20, 32U, array);
			Class12.smethod_2(ref num6, num7, num8, num9, 5U, 4, 33U, array);
			Class12.smethod_2(ref num9, num6, num7, num8, 8U, 11, 34U, array);
			Class12.smethod_2(ref num8, num9, num6, num7, 11U, 16, 35U, array);
			Class12.smethod_2(ref num7, num8, num9, num6, 14U, 23, 36U, array);
			Class12.smethod_2(ref num6, num7, num8, num9, 1U, 4, 37U, array);
			Class12.smethod_2(ref num9, num6, num7, num8, 4U, 11, 38U, array);
			Class12.smethod_2(ref num8, num9, num6, num7, 7U, 16, 39U, array);
			Class12.smethod_2(ref num7, num8, num9, num6, 10U, 23, 40U, array);
			Class12.smethod_2(ref num6, num7, num8, num9, 13U, 4, 41U, array);
			Class12.smethod_2(ref num9, num6, num7, num8, 0U, 11, 42U, array);
			Class12.smethod_2(ref num8, num9, num6, num7, 3U, 16, 43U, array);
			Class12.smethod_2(ref num7, num8, num9, num6, 6U, 23, 44U, array);
			Class12.smethod_2(ref num6, num7, num8, num9, 9U, 4, 45U, array);
			Class12.smethod_2(ref num9, num6, num7, num8, 12U, 11, 46U, array);
			Class12.smethod_2(ref num8, num9, num6, num7, 15U, 16, 47U, array);
			Class12.smethod_2(ref num7, num8, num9, num6, 2U, 23, 48U, array);
			Class12.smethod_3(ref num6, num7, num8, num9, 0U, 6, 49U, array);
			Class12.smethod_3(ref num9, num6, num7, num8, 7U, 10, 50U, array);
			Class12.smethod_3(ref num8, num9, num6, num7, 14U, 15, 51U, array);
			Class12.smethod_3(ref num7, num8, num9, num6, 5U, 21, 52U, array);
			Class12.smethod_3(ref num6, num7, num8, num9, 12U, 6, 53U, array);
			Class12.smethod_3(ref num9, num6, num7, num8, 3U, 10, 54U, array);
			Class12.smethod_3(ref num8, num9, num6, num7, 10U, 15, 55U, array);
			Class12.smethod_3(ref num7, num8, num9, num6, 1U, 21, 56U, array);
			Class12.smethod_3(ref num6, num7, num8, num9, 8U, 6, 57U, array);
			Class12.smethod_3(ref num9, num6, num7, num8, 15U, 10, 58U, array);
			Class12.smethod_3(ref num8, num9, num6, num7, 6U, 15, 59U, array);
			Class12.smethod_3(ref num7, num8, num9, num6, 13U, 21, 60U, array);
			Class12.smethod_3(ref num6, num7, num8, num9, 4U, 6, 61U, array);
			Class12.smethod_3(ref num9, num6, num7, num8, 11U, 10, 62U, array);
			Class12.smethod_3(ref num8, num9, num6, num7, 2U, 15, 63U, array);
			Class12.smethod_3(ref num7, num8, num9, num6, 9U, 21, 64U, array);
			num6 += num13;
			num7 += num14;
			num8 += num15;
			num9 += num16;
		}
		byte[] array4 = new byte[16];
		Array.Copy(BitConverter.GetBytes(num6), 0, array4, 0, 4);
		Array.Copy(BitConverter.GetBytes(num7), 0, array4, 4, 4);
		Array.Copy(BitConverter.GetBytes(num8), 0, array4, 8, 4);
		Array.Copy(BitConverter.GetBytes(num9), 0, array4, 12, 4);
		return array4;
	}

	// Token: 0x06000774 RID: 1908 RVA: 0x000068ED File Offset: 0x00004AED
	private static void dvsYoUdMrG(ref uint uint_1, uint uint_2, uint uint_3, uint uint_4, uint uint_5, ushort ushort_0, uint uint_6, uint[] object_4)
	{
		uint_1 = uint_2 + Class12.smethod_4(uint_1 + ((uint_2 & uint_3) | (~uint_2 & uint_4)) + object_4[(int)uint_5] + Class12.uint_0[(int)(uint_6 - 1U)], ushort_0);
	}

	// Token: 0x06000775 RID: 1909 RVA: 0x00006916 File Offset: 0x00004B16
	private static void smethod_1(ref uint uint_1, uint uint_2, uint uint_3, uint uint_4, uint uint_5, ushort ushort_0, uint uint_6, uint[] object_4)
	{
		uint_1 = uint_2 + Class12.smethod_4(uint_1 + ((uint_2 & uint_4) | (uint_3 & ~uint_4)) + object_4[(int)uint_5] + Class12.uint_0[(int)(uint_6 - 1U)], ushort_0);
	}

	// Token: 0x06000776 RID: 1910 RVA: 0x0000693F File Offset: 0x00004B3F
	private static void smethod_2(ref uint uint_1, uint uint_2, uint uint_3, uint uint_4, uint uint_5, ushort ushort_0, uint uint_6, uint[] object_4)
	{
		uint_1 = uint_2 + Class12.smethod_4(uint_1 + (uint_2 ^ uint_3 ^ uint_4) + object_4[(int)uint_5] + Class12.uint_0[(int)(uint_6 - 1U)], ushort_0);
	}

	// Token: 0x06000777 RID: 1911 RVA: 0x00006965 File Offset: 0x00004B65
	private static void smethod_3(ref uint uint_1, uint uint_2, uint uint_3, uint uint_4, uint uint_5, ushort ushort_0, uint uint_6, uint[] object_4)
	{
		uint_1 = uint_2 + Class12.smethod_4(uint_1 + (uint_3 ^ (uint_2 | ~uint_4)) + object_4[(int)uint_5] + Class12.uint_0[(int)(uint_6 - 1U)], ushort_0);
	}

	// Token: 0x06000778 RID: 1912 RVA: 0x0000698C File Offset: 0x00004B8C
	private static uint smethod_4(uint uint_1, ushort ushort_0)
	{
		return uint_1 >> (int)(32 - ushort_0) | uint_1 << (int)ushort_0;
	}

	// Token: 0x06000779 RID: 1913 RVA: 0x0000699E File Offset: 0x00004B9E
	internal static bool smethod_5()
	{
		if (!Class12.bool_5)
		{
			Class12.smethod_7();
			Class12.bool_5 = true;
		}
		return Class12.fQgAnroQoI;
	}

	// Token: 0x0600077A RID: 1914 RVA: 0x00002300 File Offset: 0x00000500
	internal Class12()
	{
	}

	// Token: 0x0600077B RID: 1915 RVA: 0x000209E0 File Offset: 0x0001EBE0
	private void method_1(byte[] byte_2, byte[] byte_3, byte[] byte_4)
	{
		int num = byte_4.Length % 4;
		int num2 = byte_4.Length / 4;
		byte[] array = new byte[byte_4.Length];
		int num3 = byte_2.Length / 4;
		uint num4 = 0U;
		if (num > 0)
		{
			num2++;
		}
		for (int i = 0; i < num2; i++)
		{
			int num5 = i % num3;
			int num6 = i * 4;
			uint num7 = (uint)(num5 * 4);
			uint num8 = (uint)((int)byte_2[(int)(num7 + 3U)] << 24 | (int)byte_2[(int)(num7 + 2U)] << 16 | (int)byte_2[(int)(num7 + 1U)] << 8 | (int)byte_2[(int)num7]);
			uint num9 = 255U;
			int num10 = 0;
			uint num11;
			if (i == num2 - 1 && num > 0)
			{
				num11 = 0U;
				num4 += num8;
				for (int j = 0; j < num; j++)
				{
					if (j > 0)
					{
						num11 <<= 8;
					}
					num11 |= (uint)byte_4[byte_4.Length - (1 + j)];
				}
			}
			else
			{
				num4 += num8;
				num7 = (uint)num6;
				num11 = (uint)((int)byte_4[(int)(num7 + 3U)] << 24 | (int)byte_4[(int)(num7 + 2U)] << 16 | (int)byte_4[(int)(num7 + 1U)] << 8 | (int)byte_4[(int)num7]);
			}
			uint num13;
			uint num12 = num13 = num4;
			uint num14 = 1929424900U;
			uint num15 = 2289769640U ^ num13;
			uint num16 = num15 & 16711935U;
			num15 &= 4278255360U;
			uint num17 = num15 >> 8 | num16 << 8;
			uint num18 = 932744464U;
			uint num19 = (num17 ^ num17) - num17;
			if (num13 == 0U)
			{
				num13 -= 1U;
			}
			uint num20 = num17 / num13 + num13;
			num13 = num17 - num17 - num20 + num17;
			num18 = 9495U * (num18 & 65535U) - (num18 >> 16);
			num19 = 10476U * (num19 & 65535U) - (num19 >> 16);
			num17 = 22014U * num17 + num13;
			num13 ^= num13 << 9;
			num13 += num19;
			num13 ^= num13 << 1;
			num13 += num13;
			num13 ^= num13 >> 5;
			num13 += num14;
			num13 = ((num19 << 11) + num17 ^ num19) + num13;
			num4 = num12 + (uint)num13;
			if (i == num2 - 1 && num > 0)
			{
				uint num21 = num4 ^ num11;
				for (int k = 0; k < num; k++)
				{
					if (k > 0)
					{
						num9 <<= 8;
						num10 += 8;
					}
					array[num6 + k] = (byte)((num21 & num9) >> num10);
				}
			}
			else
			{
				uint num22 = num4 ^ num11;
				array[num6] = (byte)(num22 & 255U);
				array[num6 + 1] = (byte)((num22 & 65280U) >> 8);
				array[num6 + 2] = (byte)((num22 & 16711680U) >> 16);
				array[num6 + 3] = (byte)((num22 & 4278190080U) >> 24);
			}
		}
		Class12.byte_1 = array;
	}

	// Token: 0x0600077C RID: 1916 RVA: 0x00020D3C File Offset: 0x0001EF3C
	internal static SymmetricAlgorithm smethod_6()
	{
		SymmetricAlgorithm result = null;
		if (!Class12.smethod_5())
		{
			try
			{
				return new RijndaelManaged();
			}
			catch
			{
				try
				{
					result = (SymmetricAlgorithm)Activator.CreateInstance("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Security.Cryptography.AesCryptoServiceProvider").Unwrap();
				}
				catch
				{
					result = (SymmetricAlgorithm)Activator.CreateInstance("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Security.Cryptography.AesCryptoServiceProvider").Unwrap();
				}
				return result;
			}
		}
		result = new AesCryptoServiceProvider();
		return result;
	}

	// Token: 0x0600077D RID: 1917 RVA: 0x00020DBC File Offset: 0x0001EFBC
	internal static void smethod_7()
	{
		try
		{
			new MD5CryptoServiceProvider();
		}
		catch
		{
			Class12.fQgAnroQoI = true;
			return;
		}
		try
		{
			Class12.fQgAnroQoI = CryptoConfig.AllowOnlyFipsAlgorithms;
		}
		catch
		{
		}
	}

	// Token: 0x0600077E RID: 1918 RVA: 0x000069B7 File Offset: 0x00004BB7
	internal static byte[] smethod_8(byte[] byte_2)
	{
		if (Class12.smethod_5())
		{
			return Class12.smethod_0(byte_2);
		}
		return new MD5CryptoServiceProvider().ComputeHash(byte_2);
	}

	// Token: 0x0600077F RID: 1919 RVA: 0x00020E08 File Offset: 0x0001F008
	internal static void smethod_9(HashAlgorithm hashAlgorithm_0, Stream stream_0, uint uint_1, byte[] byte_2)
	{
		while (uint_1 > 0U)
		{
			int num = (int)((uint_1 <= (uint)byte_2.Length) ? uint_1 : ((uint)byte_2.Length));
			stream_0.Read(byte_2, 0, num);
			Class12.smethod_10(hashAlgorithm_0, byte_2, 0, num);
			uint_1 -= (uint)num;
		}
	}

	// Token: 0x06000780 RID: 1920 RVA: 0x000069D2 File Offset: 0x00004BD2
	internal static void smethod_10(HashAlgorithm hashAlgorithm_0, byte[] byte_2, int int_6, int int_7)
	{
		hashAlgorithm_0.TransformBlock(byte_2, int_6, int_7, byte_2, int_6);
	}

	// Token: 0x06000781 RID: 1921 RVA: 0x00020E44 File Offset: 0x0001F044
	internal static uint smethod_11(uint uint_1, int int_6, long long_2, BinaryReader binaryReader_0)
	{
		for (int i = 0; i < int_6; i++)
		{
			binaryReader_0.BaseStream.Position = long_2 + (long)(i * 40 + 8);
			uint num = binaryReader_0.ReadUInt32();
			uint num2 = binaryReader_0.ReadUInt32();
			binaryReader_0.ReadUInt32();
			uint num3 = binaryReader_0.ReadUInt32();
			if (num2 <= uint_1 && uint_1 < num2 + num)
			{
				return num3 + uint_1 - num2;
			}
		}
		return 0U;
	}

	// Token: 0x06000782 RID: 1922 RVA: 0x00020EA0 File Offset: 0x0001F0A0
	public static void smethod_12(RuntimeTypeHandle runtimeTypeHandle_0)
	{
		try
		{
			Type typeFromHandle = Type.GetTypeFromHandle(runtimeTypeHandle_0);
			if (Class12.dictionary_0 == null)
			{
				object obj = Class12.object_3;
				lock (obj)
				{
					Dictionary<int, int> dictionary = new Dictionary<int, int>();
					BinaryReader binaryReader = new BinaryReader(typeof(Class12).Assembly.GetManifestResourceStream("U2em1bf27GlLaO8n2j.oPNUNrDDw5Jk3GVNgQ"));
					binaryReader.BaseStream.Position = 0L;
					byte[] array = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);
					binaryReader.Close();
					if (array.Length != 0)
					{
						int num = array.Length % 4;
						int num2 = array.Length / 4;
						byte[] array2 = new byte[array.Length];
						uint num3 = 0U;
						if (num > 0)
						{
							num2++;
						}
						for (int i = 0; i < num2; i++)
						{
							int num4 = i * 4;
							uint num5 = 255U;
							int num6 = 0;
							uint num7;
							if (i == num2 - 1 && num > 0)
							{
								num7 = 0U;
								for (int j = 0; j < num; j++)
								{
									if (j > 0)
									{
										num7 <<= 8;
									}
									num7 |= (uint)array[array.Length - (1 + j)];
								}
							}
							else
							{
								uint num8 = (uint)num4;
								num7 = (uint)((int)array[(int)(num8 + 3U)] << 24 | (int)array[(int)(num8 + 2U)] << 16 | (int)array[(int)(num8 + 1U)] << 8 | (int)array[(int)num8]);
							}
							num3 = num3;
							uint num9 = num3;
							uint num10 = num3;
							uint num11 = 1929424900U;
							uint num12 = 2289769640U ^ num10;
							uint num13 = num12 & 16711935U;
							num12 &= 4278255360U;
							uint num14 = num12 >> 8 | num13 << 8;
							uint num15 = 932744464U;
							uint num16 = (num14 ^ num14) - num14;
							if (num10 == 0U)
							{
								num10 -= 1U;
							}
							uint num17 = num14 / num10 + num10;
							num10 = num14 - num14 - num17 + num14;
							num15 = 9495U * (num15 & 65535U) - (num15 >> 16);
							num16 = 10476U * (num16 & 65535U) - (num16 >> 16);
							num14 = 22014U * num14 + num10;
							num10 ^= num10 << 9;
							num10 += num16;
							num10 ^= num10 << 1;
							num10 += num10;
							num10 ^= num10 >> 5;
							num10 += num11;
							num10 = ((num16 << 11) + num14 ^ num16) + num10;
							num3 = num9 + (uint)num10;
							if (i == num2 - 1 && num > 0)
							{
								uint num18 = num3 ^ num7;
								for (int k = 0; k < num; k++)
								{
									if (k > 0)
									{
										num5 <<= 8;
										num6 += 8;
									}
									array2[num4 + k] = (byte)((num18 & num5) >> num6);
								}
							}
							else
							{
								uint num19 = num3 ^ num7;
								array2[num4] = (byte)(num19 & 255U);
								array2[num4 + 1] = (byte)((num19 & 65280U) >> 8);
								array2[num4 + 2] = (byte)((num19 & 16711680U) >> 16);
								array2[num4 + 3] = (byte)((num19 & 4278190080U) >> 24);
							}
						}
						array = array2;
						int num20 = array.Length / 8;
						Class12.Class15 @class = new Class12.Class15(new MemoryStream(array));
						for (int l = 0; l < num20; l++)
						{
							int key = @class.method_3();
							int value = @class.method_3();
							dictionary.Add(key, value);
						}
						@class.method_4();
					}
					Class12.dictionary_0 = dictionary;
				}
			}
			FieldInfo[] fields = typeFromHandle.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetField);
			for (int m = 0; m < fields.Length; m++)
			{
				try
				{
					FieldInfo fieldInfo = fields[m];
					int metadataToken = fieldInfo.MetadataToken;
					int num21 = Class12.dictionary_0[metadataToken];
					bool flag2 = (num21 & 1073741824) > 0;
					num21 &= 1073741823;
					MethodInfo methodInfo = (MethodInfo)typeof(Class12).Module.ResolveMethod(num21, typeFromHandle.GetGenericArguments(), new Type[0]);
					if (methodInfo.IsStatic)
					{
						fieldInfo.SetValue(null, Delegate.CreateDelegate(fieldInfo.FieldType, methodInfo));
					}
					else
					{
						ParameterInfo[] parameters = methodInfo.GetParameters();
						int num22 = parameters.Length + 1;
						Type[] array3 = new Type[num22];
						if (methodInfo.DeclaringType.IsValueType)
						{
							array3[0] = methodInfo.DeclaringType.MakeByRefType();
						}
						else
						{
							array3[0] = typeof(object);
						}
						for (int n = 0; n < parameters.Length; n++)
						{
							array3[n + 1] = parameters[n].ParameterType;
						}
						DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, methodInfo.ReturnType, array3, typeFromHandle, true);
						ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
						for (int num23 = 0; num23 < num22; num23++)
						{
							switch (num23)
							{
							case 0:
								ilgenerator.Emit(OpCodes.Ldarg_0);
								break;
							case 1:
								ilgenerator.Emit(OpCodes.Ldarg_1);
								break;
							case 2:
								ilgenerator.Emit(OpCodes.Ldarg_2);
								break;
							case 3:
								ilgenerator.Emit(OpCodes.Ldarg_3);
								break;
							default:
								ilgenerator.Emit(OpCodes.Ldarg_S, num23);
								break;
							}
						}
						ilgenerator.Emit(OpCodes.Tailcall);
						ilgenerator.Emit(flag2 ? OpCodes.Callvirt : OpCodes.Call, methodInfo);
						ilgenerator.Emit(OpCodes.Ret);
						fieldInfo.SetValue(null, dynamicMethod.CreateDelegate(typeFromHandle));
					}
				}
				catch (Exception)
				{
				}
			}
		}
		catch (Exception)
		{
		}
	}

	// Token: 0x06000783 RID: 1923 RVA: 0x000069E0 File Offset: 0x00004BE0
	private static uint smethod_13(uint uint_1)
	{
		return (uint)"V8vU2V3RMKGsRDNiU".Length;
	}

	// Token: 0x06000784 RID: 1924 RVA: 0x000214EC File Offset: 0x0001F6EC
	private static void smethod_14(Stream stream_0, int int_6)
	{
		RSACryptoServiceProvider.UseMachineKeyStore = true;
		Class12.rsacryptoServiceProvider_0 = new RSACryptoServiceProvider();
		string location = typeof(Class12).Assembly.Location;
		if (location != null && location.Length != 0)
		{
			HashAlgorithm obj = null;
			string str = null;
			try
			{
				obj = SHA1.Create();
				str = CryptoConfig.MapNameToOID("SHA1");
				if (!File.Exists(location))
				{
					goto IL_37;
				}
			}
			catch
			{
				goto IL_37;
			}
			bool flag = false;
			try
			{
				Class12.Class15 @class = new Class12.Class15(Class12.assembly_0.GetManifestResourceStream("7uLbEBRPsZW5JihXkm.Zp8vwLYAteMhuSyYxg"));
				@class.method_0().Position = 0L;
				byte[] obj2 = @class.method_1((int)@class.method_0().Length);
				byte[] obj3 = new byte[32];
				obj3[0] = 110;
				obj3[0] = 128;
				obj3[0] = 161;
				obj3[0] = 146;
				obj3[0] = 148;
				obj3[0] = 75;
				obj3[1] = 120;
				obj3[1] = 146;
				obj3[1] = 160;
				obj3[1] = 141;
				obj3[1] = 40;
				obj3[1] = 197;
				obj3[2] = 128;
				obj3[2] = 162;
				obj3[2] = 125;
				obj3[2] = 93;
				obj3[2] = 161;
				obj3[3] = 153;
				obj3[3] = 137;
				obj3[3] = 143;
				obj3[3] = 194;
				obj3[4] = 106;
				obj3[4] = 155;
				obj3[4] = 26;
				obj3[4] = 115;
				obj3[4] = 100;
				obj3[4] = 179;
				obj3[5] = 112;
				obj3[5] = 228;
				obj3[5] = 231;
				obj3[5] = 88;
				obj3[6] = 197;
				obj3[6] = 126;
				obj3[6] = 130;
				obj3[6] = 136;
				obj3[6] = 81;
				obj3[7] = 145;
				obj3[7] = 136;
				obj3[7] = 152;
				obj3[7] = 111;
				obj3[7] = 116;
				obj3[8] = 141;
				obj3[8] = 144;
				obj3[8] = 205;
				obj3[8] = 206;
				obj3[8] = 140;
				obj3[8] = 91;
				obj3[9] = 3;
				obj3[9] = 110;
				obj3[9] = 165;
				obj3[9] = 175;
				obj3[10] = 120;
				obj3[10] = 85;
				obj3[10] = 147;
				obj3[10] = 98;
				obj3[10] = 134;
				obj3[11] = 90;
				obj3[11] = 146;
				obj3[11] = 141;
				obj3[11] = 156;
				obj3[11] = 118;
				obj3[11] = 142;
				obj3[12] = 130;
				obj3[12] = 49;
				obj3[12] = 51;
				obj3[13] = 150;
				obj3[13] = 103;
				obj3[13] = 43;
				obj3[13] = 207;
				obj3[13] = 45;
				obj3[14] = 118;
				obj3[14] = 99;
				obj3[14] = 127;
				obj3[14] = 180;
				obj3[14] = 195;
				obj3[14] = 196;
				obj3[15] = 177;
				obj3[15] = 88;
				obj3[15] = 19;
				obj3[16] = 101;
				obj3[16] = 130;
				obj3[16] = 182;
				obj3[16] = 110;
				obj3[16] = 98;
				obj3[16] = 240;
				obj3[17] = 183;
				obj3[17] = 141;
				obj3[17] = 74;
				obj3[18] = 159;
				obj3[18] = 143;
				obj3[18] = 91;
				obj3[19] = 88;
				obj3[19] = 129;
				obj3[19] = 116;
				obj3[19] = 146;
				obj3[19] = 195;
				obj3[20] = 20;
				obj3[20] = 164;
				obj3[20] = 124;
				obj3[21] = 151;
				obj3[21] = 132;
				obj3[21] = 213;
				obj3[22] = 95;
				obj3[22] = 160;
				obj3[22] = 93;
				obj3[22] = 139;
				obj3[22] = 79;
				obj3[22] = 8;
				obj3[23] = 122;
				obj3[23] = 137;
				obj3[23] = 185;
				obj3[23] = 238;
				obj3[24] = 156;
				obj3[24] = 138;
				obj3[24] = 142;
				obj3[24] = 134;
				obj3[24] = 139;
				obj3[25] = 86;
				obj3[25] = 160;
				obj3[25] = 153;
				obj3[25] = 31;
				obj3[25] = 124;
				obj3[25] = 188;
				obj3[26] = 134;
				obj3[26] = 97;
				obj3[26] = 129;
				obj3[26] = 202;
				obj3[27] = 88;
				obj3[27] = 142;
				obj3[27] = 144;
				obj3[27] = 133;
				obj3[27] = 229;
				obj3[28] = 86;
				obj3[28] = 76;
				obj3[28] = 71;
				obj3[29] = 162;
				obj3[29] = 191;
				obj3[29] = 108;
				obj3[29] = 117;
				obj3[29] = 229;
				obj3[30] = 120;
				obj3[30] = 191;
				obj3[30] = 98;
				obj3[30] = 129;
				obj3[31] = 103;
				obj3[31] = 137;
				obj3[31] = 109;
				obj3[31] = 87;
				obj3[31] = 161;
				byte[] rgbKey = obj3;
				byte[] obj4 = new byte[16];
				obj4[0] = 145;
				obj4[0] = 138;
				obj4[0] = 161;
				obj4[0] = 70;
				obj4[1] = 170;
				obj4[1] = 121;
				obj4[1] = 114;
				obj4[1] = 111;
				obj4[1] = 114;
				obj4[1] = 147;
				obj4[2] = 134;
				obj4[2] = 155;
				obj4[2] = 7;
				obj4[3] = 162;
				obj4[3] = 159;
				obj4[3] = 156;
				obj4[3] = 132;
				obj4[3] = 31;
				obj4[4] = 93;
				obj4[4] = 112;
				obj4[4] = 237;
				obj4[5] = 127;
				obj4[5] = 124;
				obj4[5] = 89;
				obj4[6] = 59;
				obj4[6] = 159;
				obj4[6] = 175;
				obj4[7] = 163;
				obj4[7] = 152;
				obj4[7] = 90;
				obj4[7] = 112;
				obj4[7] = 108;
				obj4[7] = 87;
				obj4[8] = 96;
				obj4[8] = 67;
				obj4[8] = 143;
				obj4[8] = 94;
				obj4[8] = 203;
				obj4[9] = 128;
				obj4[9] = 137;
				obj4[9] = 136;
				obj4[9] = 138;
				obj4[9] = 15;
				obj4[10] = 106;
				obj4[10] = 115;
				obj4[10] = 96;
				obj4[10] = 89;
				obj4[10] = 131;
				obj4[10] = 250;
				obj4[11] = 128;
				obj4[11] = 160;
				obj4[11] = 143;
				obj4[11] = 164;
				obj4[12] = 95;
				obj4[12] = 164;
				obj4[12] = 154;
				obj4[12] = 57;
				obj4[13] = 104;
				obj4[13] = 169;
				obj4[13] = 99;
				obj4[13] = 94;
				obj4[14] = 112;
				obj4[14] = 133;
				obj4[14] = 140;
				obj4[14] = 107;
				obj4[15] = 139;
				obj4[15] = 128;
				obj4[15] = 93;
				obj4[15] = 166;
				obj4[15] = 170;
				byte[] rgbIV = obj4;
				SymmetricAlgorithm symmetricAlgorithm = Class12.smethod_6();
				symmetricAlgorithm.Mode = CipherMode.CBC;
				ICryptoTransform transform = symmetricAlgorithm.CreateDecryptor(rgbKey, rgbIV);
				MemoryStream obj5 = (MemoryStream)Class12.smethod_28();
				CryptoStream cryptoStream = new CryptoStream(obj5, transform, CryptoStreamMode.Write);
				cryptoStream.Write(obj2, 0, obj2.Length);
				cryptoStream.FlushFinalBlock();
				Class12.rsacryptoServiceProvider_0.FromXmlString(Encoding.UTF8.GetString(Class12.smethod_29(obj5)));
				obj5.Close();
				cryptoStream.Close();
				@class.method_4();
			}
			catch
			{
				flag = true;
			}
			if (!flag)
			{
				BinaryReader obj6 = null;
				try
				{
					FileStream obj7 = new FileStream(location, FileMode.Open, FileAccess.Read, FileShare.Read);
					obj6 = new BinaryReader(obj7);
					byte[] obj8 = new byte[65536];
					Class12.smethod_9(obj, obj7, 152U, obj8);
					bool flag2 = obj6.ReadUInt16() != 523;
					int num = flag2 ? 96 : 112;
					obj7.Position = 152L;
					obj7.Read(obj8, 0, num);
					obj8[64] = 0;
					obj8[65] = 0;
					obj8[66] = 0;
					obj8[67] = 0;
					Class12.smethod_10(obj, obj8, 0, num);
					obj7.Read(obj8, 0, 128);
					obj8[32] = 0;
					obj8[33] = 0;
					obj8[34] = 0;
					obj8[35] = 0;
					obj8[36] = 0;
					obj8[37] = 0;
					obj8[38] = 0;
					obj8[39] = 0;
					Class12.smethod_10(obj, obj8, 0, 128);
					long position = obj7.Position;
					obj7.Position = 134L;
					int num2 = (int)obj6.ReadUInt16();
					obj7.Position = position;
					Class12.smethod_9(obj, obj7, (uint)(num2 * 40), obj8);
					long position2 = obj7.Position;
					if (flag2)
					{
						obj7.Position = 360L;
					}
					else
					{
						obj7.Position = 376L;
					}
					uint num3 = Class12.smethod_11(obj6.ReadUInt32(), num2, position, obj6);
					obj7.Position = (long)((ulong)(num3 + 32U));
					uint uint_ = obj6.ReadUInt32();
					uint num4 = obj6.ReadUInt32();
					long num5 = (long)((ulong)Class12.smethod_11(uint_, num2, position, obj6));
					long num6 = num5 + (long)((ulong)num4);
					obj7.Position = position2;
					for (int i = 0; i < num2; i++)
					{
						obj7.Position = position + (long)(i * 40) + 16L;
						uint num7 = obj6.ReadUInt32();
						uint num8 = obj6.ReadUInt32();
						obj7.Position = (long)((ulong)num8);
						while (num7 > 0U)
						{
							long position3 = obj7.Position;
							if (num5 > position3 || position3 >= num6)
							{
								if (position3 >= num6)
								{
									Class12.smethod_9(obj, obj7, num7, obj8);
									break;
								}
								uint num9 = (uint)Math.Min(num5 - position3, (long)((ulong)num7));
								Class12.smethod_9(obj, obj7, num9, obj8);
								num7 -= num9;
							}
							else
							{
								uint num10 = (uint)(num6 - position3);
								if (num10 >= num7)
								{
									break;
								}
								num7 -= num10;
								obj7.Position += (long)((ulong)num10);
							}
						}
					}
					obj.TransformFinalBlock(new byte[0], 0, 0);
					obj7.Position = num5;
					byte[] obj9 = obj6.ReadBytes((int)num4);
					Array.Reverse(obj9);
					flag = !Class12.rsacryptoServiceProvider_0.VerifyHash(obj.Hash, str, obj9);
				}
				catch
				{
					flag = true;
				}
				try
				{
					if (obj6 != null)
					{
						obj6.Close();
					}
				}
				catch
				{
				}
			}
			if (flag)
			{
				throw new Exception(typeof(Class12).Assembly.GetName().Name + " ");
			}
			flag = false;
		}
		IL_37:
		Class12.Class15 class2 = new Class12.Class15(stream_0);
		class2.method_0().Position = 0L;
		byte[] obj10 = class2.method_1((int)class2.method_0().Length);
		class2.method_4();
		byte[] obj11 = new byte[32];
		obj11[0] = 135;
		obj11[0] = 19;
		obj11[0] = 79;
		obj11[0] = 50;
		obj11[1] = 116;
		obj11[1] = 94;
		obj11[1] = 102;
		obj11[1] = 231;
		obj11[2] = 127;
		obj11[2] = 136;
		obj11[2] = 108;
		obj11[2] = 129;
		obj11[2] = 120;
		obj11[2] = 168;
		obj11[3] = 167;
		obj11[3] = 107;
		obj11[3] = 159;
		obj11[3] = 121;
		obj11[3] = 144;
		obj11[4] = 88;
		obj11[4] = 87;
		obj11[4] = 241;
		obj11[5] = 49;
		obj11[5] = 91;
		obj11[5] = 12;
		obj11[6] = 161;
		obj11[6] = 86;
		obj11[6] = 116;
		obj11[6] = 45;
		obj11[6] = 136;
		obj11[7] = 160;
		obj11[7] = 135;
		obj11[7] = 147;
		obj11[7] = 151;
		obj11[7] = 118;
		obj11[8] = 77;
		obj11[8] = 151;
		obj11[8] = 167;
		obj11[9] = 112;
		obj11[9] = 106;
		obj11[9] = 88;
		obj11[9] = 105;
		obj11[9] = 192;
		obj11[10] = 150;
		obj11[10] = 129;
		obj11[10] = 52;
		obj11[10] = 182;
		obj11[11] = 33;
		obj11[11] = 157;
		obj11[11] = 119;
		obj11[11] = 185;
		obj11[11] = 94;
		obj11[11] = 104;
		obj11[12] = 124;
		obj11[12] = 103;
		obj11[12] = 115;
		obj11[13] = 108;
		obj11[13] = 122;
		obj11[13] = 122;
		obj11[13] = 128;
		obj11[13] = 97;
		obj11[13] = 0;
		obj11[14] = 86;
		obj11[14] = 110;
		obj11[14] = 109;
		obj11[14] = 116;
		obj11[14] = 103;
		obj11[14] = 21;
		obj11[15] = 85;
		obj11[15] = 131;
		obj11[15] = 112;
		obj11[15] = 86;
		obj11[15] = 158;
		obj11[15] = 44;
		obj11[16] = 110;
		obj11[16] = 122;
		obj11[16] = 96;
		obj11[16] = 174;
		obj11[17] = 118;
		obj11[17] = 153;
		obj11[17] = 212;
		obj11[17] = 131;
		obj11[18] = 95;
		obj11[18] = 81;
		obj11[18] = 13;
		obj11[19] = 128;
		obj11[19] = 101;
		obj11[19] = 169;
		obj11[19] = 210;
		obj11[20] = 141;
		obj11[20] = 135;
		obj11[20] = 205;
		obj11[21] = 118;
		obj11[21] = 202;
		obj11[21] = 80;
		obj11[21] = 144;
		obj11[21] = 156;
		obj11[21] = 101;
		obj11[22] = 148;
		obj11[22] = 108;
		obj11[22] = 155;
		obj11[22] = 108;
		obj11[22] = 48;
		obj11[23] = 133;
		obj11[23] = 160;
		obj11[23] = 1;
		obj11[24] = 130;
		obj11[24] = 102;
		obj11[24] = 103;
		obj11[24] = 94;
		obj11[24] = 166;
		obj11[25] = 135;
		obj11[25] = 203;
		obj11[25] = 149;
		obj11[26] = 126;
		obj11[26] = 104;
		obj11[26] = 155;
		obj11[26] = 158;
		obj11[26] = 79;
		obj11[26] = 11;
		obj11[27] = 66;
		obj11[27] = 94;
		obj11[27] = 57;
		obj11[28] = 78;
		obj11[28] = 144;
		obj11[28] = 78;
		obj11[29] = 166;
		obj11[29] = 16;
		obj11[29] = 197;
		obj11[30] = 180;
		obj11[30] = 194;
		obj11[30] = 167;
		obj11[30] = 149;
		obj11[30] = 222;
		obj11[31] = 130;
		obj11[31] = 138;
		obj11[31] = 190;
		byte[] obj12 = obj11;
		byte[] obj13 = new byte[16];
		obj13[0] = 117;
		obj13[0] = 166;
		obj13[0] = 114;
		obj13[0] = 238;
		obj13[1] = 105;
		obj13[1] = 133;
		obj13[1] = 169;
		obj13[1] = 103;
		obj13[1] = 202;
		obj13[2] = 91;
		obj13[2] = 115;
		obj13[2] = 138;
		obj13[2] = 108;
		obj13[3] = 107;
		obj13[3] = 128;
		obj13[3] = 116;
		obj13[3] = 92;
		obj13[3] = 211;
		obj13[4] = 90;
		obj13[4] = 39;
		obj13[4] = 157;
		obj13[4] = 96;
		obj13[4] = 84;
		obj13[4] = 10;
		obj13[5] = 186;
		obj13[5] = 73;
		obj13[5] = 168;
		obj13[5] = 147;
		obj13[5] = 170;
		obj13[5] = 43;
		obj13[6] = 155;
		obj13[6] = 145;
		obj13[6] = 225;
		obj13[7] = 62;
		obj13[7] = 103;
		obj13[7] = 234;
		obj13[8] = 166;
		obj13[8] = 164;
		obj13[8] = 233;
		obj13[9] = 140;
		obj13[9] = 140;
		obj13[9] = 149;
		obj13[9] = 163;
		obj13[9] = 111;
		obj13[9] = 81;
		obj13[10] = 147;
		obj13[10] = 61;
		obj13[10] = 158;
		obj13[10] = 123;
		obj13[10] = 213;
		obj13[11] = 58;
		obj13[11] = 69;
		obj13[11] = 84;
		obj13[11] = 69;
		obj13[11] = 192;
		obj13[11] = 228;
		obj13[12] = 147;
		obj13[12] = 148;
		obj13[12] = 176;
		obj13[13] = 24;
		obj13[13] = 160;
		obj13[13] = 160;
		obj13[13] = 47;
		obj13[13] = 234;
		obj13[14] = 133;
		obj13[14] = 184;
		obj13[14] = 127;
		obj13[14] = 153;
		obj13[14] = 136;
		obj13[14] = 178;
		obj13[15] = 125;
		obj13[15] = 87;
		obj13[15] = 138;
		obj13[15] = 137;
		obj13[15] = 65;
		byte[] obj14 = obj13;
		Array.Reverse(obj14);
		byte[] publicKeyToken = Class12.assembly_0.GetName().GetPublicKeyToken();
		if (publicKeyToken != null && publicKeyToken.Length != 0)
		{
			obj14[1] = publicKeyToken[0];
			obj14[3] = publicKeyToken[1];
			obj14[5] = publicKeyToken[2];
			obj14[7] = publicKeyToken[3];
			obj14[9] = publicKeyToken[4];
			obj14[11] = publicKeyToken[5];
			obj14[13] = publicKeyToken[6];
			obj14[15] = publicKeyToken[7];
		}
		for (int j = 0; j < obj14.Length; j++)
		{
			obj12[j] ^= obj14[j];
		}
		if (int_6 == -1)
		{
			SymmetricAlgorithm symmetricAlgorithm2 = Class12.smethod_6();
			symmetricAlgorithm2.Mode = CipherMode.CBC;
			ICryptoTransform transform2 = symmetricAlgorithm2.CreateDecryptor(obj12, obj14);
			MemoryStream obj15 = (MemoryStream)Class12.smethod_28();
			CryptoStream cryptoStream2 = new CryptoStream(obj15, transform2, CryptoStreamMode.Write);
			cryptoStream2.Write(obj10, 0, obj10.Length);
			cryptoStream2.FlushFinalBlock();
			Class12.byte_1 = Class12.smethod_29(obj15);
			obj15.Close();
			cryptoStream2.Close();
			obj10 = Class12.byte_1;
		}
		if (Class12.assembly_0.EntryPoint == null)
		{
			Class12.int_2 = 80;
		}
		new Class12().method_1(obj12, obj14, obj10);
	}

	// Token: 0x06000785 RID: 1925 RVA: 0x00022F14 File Offset: 0x00021114
	internal static string smethod_15(int int_6)
	{
		if (Class12.byte_1.Length == 0)
		{
			Class12.list_1 = new List<string>();
			Class12.list_0 = new List<int>();
			Class12.smethod_14(Class12.assembly_0.GetManifestResourceStream("cF1CKXVABfDQdYtCqN.pBbXk1dNJ3P1w5SAwQ"), int_6);
		}
		if (Class12.int_2 < 75)
		{
			MethodBase method = new StackFrame(1).GetMethod();
			if (Class12.assembly_0 != method.DeclaringType.Assembly)
			{
				bool flag = false;
				string name = method.DeclaringType.Assembly.GetName().Name;
				foreach (AssemblyName assemblyName in Class12.assembly_0.GetReferencedAssemblies())
				{
					if (name == assemblyName.Name)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					throw new Exception();
				}
			}
			Class12.int_2++;
		}
		object obj = Class12.object_2;
		string result;
		lock (obj)
		{
			int num = BitConverter.ToInt32(Class12.byte_1, int_6);
			if (num >= Class12.list_0.Count || Class12.list_0[num] != int_6)
			{
				try
				{
					byte[] array = new byte[num];
					Array.Copy(Class12.byte_1, int_6 + 4, array, 0, num);
					string @string = Encoding.Unicode.GetString(array, 0, array.Length);
					Class12.list_1.Add(@string);
					Class12.list_0.Add(int_6);
					Array.Copy(BitConverter.GetBytes(Class12.list_1.Count - 1), 0, Class12.byte_1, int_6, 4);
					return @string;
				}
				catch
				{
					goto IL_192;
				}
			}
			result = Class12.list_1[num];
		}
		return result;
		IL_192:
		return "";
	}

	// Token: 0x06000786 RID: 1926 RVA: 0x000230D4 File Offset: 0x000212D4
	internal static string smethod_16(string string_1)
	{
		"Nc3moHiXdmi63CytJ".Trim();
		byte[] array = Convert.FromBase64String(string_1);
		return Encoding.Unicode.GetString(array, 0, array.Length);
	}

	// Token: 0x06000787 RID: 1927 RVA: 0x00023104 File Offset: 0x00021304
	private static void smethod_17()
	{
		try
		{
			RSACryptoServiceProvider.UseMachineKeyStore = true;
		}
		catch
		{
		}
	}

	// Token: 0x06000788 RID: 1928 RVA: 0x0002312C File Offset: 0x0002132C
	private static Delegate smethod_18(IntPtr intptr_4, Type type_0)
	{
		return (Delegate)typeof(Marshal).GetMethod("GetDelegateForFunctionPointer", new Type[]
		{
			typeof(IntPtr),
			typeof(Type)
		}).Invoke(null, new object[]
		{
			intptr_4,
			type_0
		});
	}

	// Token: 0x06000789 RID: 1929 RVA: 0x00023190 File Offset: 0x00021390
	internal static object smethod_19(Assembly assembly_1)
	{
		object location;
		try
		{
			if (!File.Exists(((Assembly)assembly_1).Location))
			{
				goto IL_27;
			}
			location = ((Assembly)assembly_1).Location;
		}
		catch
		{
			goto IL_27;
		}
		return location;
		IL_27:
		try
		{
			if (File.Exists(((Assembly)assembly_1).GetName().CodeBase.ToString().Replace("file:///", "")))
			{
				return ((Assembly)assembly_1).GetName().CodeBase.ToString().Replace("file:///", "");
			}
		}
		catch
		{
		}
		try
		{
			if (File.Exists(assembly_1.GetType().GetProperty("Location").GetValue(assembly_1, new object[0]).ToString()))
			{
				return assembly_1.GetType().GetProperty("Location").GetValue(assembly_1, new object[0]).ToString();
			}
		}
		catch
		{
		}
		return "";
	}

	// Token: 0x0600078A RID: 1930
	[DllImport("kernel32")]
	public static extern IntPtr LoadLibrary(string string_1);

	// Token: 0x0600078B RID: 1931
	[DllImport("kernel32", CharSet = CharSet.Ansi)]
	public static extern IntPtr GetProcAddress(IntPtr intptr_4, string string_1);

	// Token: 0x0600078C RID: 1932 RVA: 0x000232A0 File Offset: 0x000214A0
	private static IntPtr smethod_20(IntPtr intptr_4, string string_1, uint uint_1)
	{
		if (Class12.delegate4_0 == null)
		{
			Class12.delegate4_0 = (Class12.Delegate4)Marshal.GetDelegateForFunctionPointer(Class12.GetProcAddress(Class12.smethod_26(), "Find ".Trim() + "ResourceA"), typeof(Class12.Delegate4));
		}
		return Class12.delegate4_0(intptr_4, string_1, uint_1);
	}

	// Token: 0x0600078D RID: 1933 RVA: 0x000232FC File Offset: 0x000214FC
	private static IntPtr smethod_21(IntPtr intptr_4, uint uint_1, uint uint_2, uint uint_3)
	{
		if (Class12.delegate5_0 == null)
		{
			Class12.delegate5_0 = (Class12.Delegate5)Marshal.GetDelegateForFunctionPointer(Class12.GetProcAddress(Class12.smethod_26(), "Virtual ".Trim() + "Alloc"), typeof(Class12.Delegate5));
		}
		return Class12.delegate5_0(intptr_4, uint_1, uint_2, uint_3);
	}

	// Token: 0x0600078E RID: 1934 RVA: 0x00023358 File Offset: 0x00021558
	private static int smethod_22(IntPtr intptr_4, IntPtr intptr_5, [In] [Out] byte[] byte_2, uint uint_1, out IntPtr intptr_6)
	{
		if (Class12.delegate6_0 == null)
		{
			Class12.delegate6_0 = (Class12.Delegate6)Marshal.GetDelegateForFunctionPointer(Class12.GetProcAddress(Class12.smethod_26(), "Write ".Trim() + "Process ".Trim() + "Memory"), typeof(Class12.Delegate6));
		}
		return Class12.delegate6_0(intptr_4, intptr_5, byte_2, uint_1, out intptr_6);
	}

	// Token: 0x0600078F RID: 1935 RVA: 0x000233C0 File Offset: 0x000215C0
	private static int smethod_23(IntPtr intptr_4, int int_6, int int_7, ref int int_8)
	{
		if (Class12.delegate7_0 == null)
		{
			Class12.delegate7_0 = (Class12.Delegate7)Marshal.GetDelegateForFunctionPointer(Class12.GetProcAddress(Class12.smethod_26(), "Virtual ".Trim() + "Protect"), typeof(Class12.Delegate7));
		}
		return Class12.delegate7_0(intptr_4, int_6, int_7, ref int_8);
	}

	// Token: 0x06000790 RID: 1936 RVA: 0x0002341C File Offset: 0x0002161C
	private static IntPtr smethod_24(uint uint_1, int int_6, uint uint_2)
	{
		if (Class12.delegate8_0 == null)
		{
			Class12.delegate8_0 = (Class12.Delegate8)Marshal.GetDelegateForFunctionPointer(Class12.GetProcAddress(Class12.smethod_26(), "Open ".Trim() + "Process"), typeof(Class12.Delegate8));
		}
		return Class12.delegate8_0(uint_1, int_6, uint_2);
	}

	// Token: 0x06000791 RID: 1937 RVA: 0x00023478 File Offset: 0x00021678
	private static int smethod_25(IntPtr intptr_4)
	{
		if (Class12.delegate9_0 == null)
		{
			Class12.delegate9_0 = (Class12.Delegate9)Marshal.GetDelegateForFunctionPointer(Class12.GetProcAddress(Class12.smethod_26(), "Close ".Trim() + "Handle"), typeof(Class12.Delegate9));
		}
		return Class12.delegate9_0(intptr_4);
	}

	// Token: 0x06000792 RID: 1938 RVA: 0x000069EC File Offset: 0x00004BEC
	private static IntPtr smethod_26()
	{
		if (Class12.intptr_0 == IntPtr.Zero)
		{
			Class12.intptr_0 = Class12.LoadLibrary("kernel ".Trim() + "32.dll");
		}
		return Class12.intptr_0;
	}

	// Token: 0x06000793 RID: 1939 RVA: 0x000234D0 File Offset: 0x000216D0
	private static byte[] smethod_27(string string_1)
	{
		byte[] array;
		using (FileStream fileStream = new FileStream(string_1, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			int num = 0;
			int i = (int)fileStream.Length;
			array = new byte[i];
			while (i > 0)
			{
				int num2 = fileStream.Read(array, num, i);
				num += num2;
				i -= num2;
			}
		}
		return array;
	}

	// Token: 0x06000794 RID: 1940 RVA: 0x00002A52 File Offset: 0x00000C52
	internal static Stream smethod_28()
	{
		return new MemoryStream();
	}

	// Token: 0x06000795 RID: 1941 RVA: 0x00006A22 File Offset: 0x00004C22
	internal static byte[] smethod_29(MemoryStream memoryStream_0)
	{
		return ((MemoryStream)memoryStream_0).ToArray();
	}

	// Token: 0x06000796 RID: 1942 RVA: 0x00023530 File Offset: 0x00021730
	private static byte[] smethod_30(byte[] byte_2)
	{
		Stream stream = Class12.smethod_28();
		SymmetricAlgorithm symmetricAlgorithm = Class12.smethod_6();
		symmetricAlgorithm.Key = new byte[]
		{
			115,
			253,
			238,
			247,
			59,
			201,
			84,
			132,
			125,
			139,
			169,
			228,
			18,
			140,
			51,
			46,
			108,
			194,
			133,
			228,
			243,
			110,
			123,
			147,
			96,
			244,
			29,
			118,
			147,
			140,
			153,
			159
		};
		symmetricAlgorithm.IV = new byte[]
		{
			29,
			149,
			96,
			240,
			130,
			80,
			126,
			97,
			146,
			93,
			96,
			30,
			203,
			100,
			3,
			46
		};
		CryptoStream cryptoStream = new CryptoStream(stream, symmetricAlgorithm.CreateDecryptor(), CryptoStreamMode.Write);
		cryptoStream.Write(byte_2, 0, byte_2.Length);
		cryptoStream.Close();
		return Class12.smethod_29((MemoryStream)stream);
	}

	// Token: 0x06000797 RID: 1943 RVA: 0x00005A47 File Offset: 0x00003C47
	private byte[] method_2()
	{
		return null;
	}

	// Token: 0x06000798 RID: 1944 RVA: 0x00005A47 File Offset: 0x00003C47
	private byte[] method_3()
	{
		return null;
	}

	// Token: 0x06000799 RID: 1945 RVA: 0x00005A47 File Offset: 0x00003C47
	private byte[] method_4()
	{
		return null;
	}

	// Token: 0x0600079A RID: 1946 RVA: 0x00005A47 File Offset: 0x00003C47
	private byte[] method_5()
	{
		return null;
	}

	// Token: 0x0600079B RID: 1947 RVA: 0x00006A2F File Offset: 0x00004C2F
	private byte[] method_6()
	{
		int length = "LPdkgRgUfpysKUP".Length;
		return new byte[]
		{
			1,
			2
		};
	}

	// Token: 0x0600079C RID: 1948 RVA: 0x00006A4A File Offset: 0x00004C4A
	private byte[] method_7()
	{
		int length = "3QabmT60gjn4DmfTf".Length;
		return new byte[]
		{
			1,
			2
		};
	}

	// Token: 0x0600079D RID: 1949 RVA: 0x00006A65 File Offset: 0x00004C65
	internal byte[] method_8()
	{
		int length = "C2kks3heUw0hSNw".Length;
		return new byte[]
		{
			1,
			2
		};
	}

	// Token: 0x0600079E RID: 1950 RVA: 0x00006A80 File Offset: 0x00004C80
	internal byte[] method_9()
	{
		int length = "1u7PmTcanmOAbJby7".Length;
		return new byte[]
		{
			1,
			2
		};
	}

	// Token: 0x0600079F RID: 1951 RVA: 0x00005A47 File Offset: 0x00003C47
	internal byte[] method_10()
	{
		return null;
	}

	// Token: 0x060007A0 RID: 1952 RVA: 0x00005A47 File Offset: 0x00003C47
	internal byte[] method_11()
	{
		return null;
	}

	// Token: 0x060007A1 RID: 1953 RVA: 0x00006A9B File Offset: 0x00004C9B
	internal static bool smethod_31()
	{
		return null == null;
	}

	// Token: 0x060007A2 RID: 1954 RVA: 0x000022D0 File Offset: 0x000004D0
	internal static void smethod_32()
	{
	}

	// Token: 0x060007A3 RID: 1955 RVA: 0x00006A9B File Offset: 0x00004C9B
	internal static bool smethod_33()
	{
		return null == null;
	}

	// Token: 0x040002FF RID: 767
	private static byte[] byte_0;

	// Token: 0x04000300 RID: 768
	private static SortedList sortedList_0;

	// Token: 0x04000301 RID: 769
	internal static object object_0;

	// Token: 0x04000302 RID: 770
	[Class12.Attribute1(typeof(Class12.Attribute1.Class13<object>[]))]
	private static bool bool_0;

	// Token: 0x04000303 RID: 771
	private static long long_0;

	// Token: 0x04000304 RID: 772
	private static bool bool_1;

	// Token: 0x04000305 RID: 773
	private static Class12.Delegate6 delegate6_0;

	// Token: 0x04000306 RID: 774
	private static long long_1;

	// Token: 0x04000307 RID: 775
	private static IntPtr intptr_0;

	// Token: 0x04000308 RID: 776
	private static bool fQgAnroQoI;

	// Token: 0x04000309 RID: 777
	internal static RSACryptoServiceProvider rsacryptoServiceProvider_0;

	// Token: 0x0400030A RID: 778
	private static bool bool_2;

	// Token: 0x0400030B RID: 779
	private static Class12.Delegate8 delegate8_0;

	// Token: 0x0400030C RID: 780
	internal static object object_1;

	// Token: 0x0400030D RID: 781
	private static int int_0;

	// Token: 0x0400030E RID: 782
	private static Class12.Delegate7 delegate7_0;

	// Token: 0x0400030F RID: 783
	private static IntPtr intptr_1;

	// Token: 0x04000310 RID: 784
	private static object object_2;

	// Token: 0x04000311 RID: 785
	private static uint[] uint_0;

	// Token: 0x04000312 RID: 786
	private static IntPtr intptr_2;

	// Token: 0x04000313 RID: 787
	private static string[] string_0;

	// Token: 0x04000314 RID: 788
	internal static Hashtable hashtable_0;

	// Token: 0x04000315 RID: 789
	private static Class12.Delegate9 delegate9_0;

	// Token: 0x04000316 RID: 790
	private static bool bool_3;

	// Token: 0x04000317 RID: 791
	private static List<int> list_0;

	// Token: 0x04000318 RID: 792
	private static int int_1;

	// Token: 0x04000319 RID: 793
	private static object object_3;

	// Token: 0x0400031A RID: 794
	private static IntPtr intptr_3;

	// Token: 0x0400031B RID: 795
	private static byte[] byte_1;

	// Token: 0x0400031C RID: 796
	private static Dictionary<int, int> dictionary_0;

	// Token: 0x0400031D RID: 797
	internal static Assembly assembly_0;

	// Token: 0x0400031E RID: 798
	private static int int_2;

	// Token: 0x0400031F RID: 799
	private static List<string> list_1;

	// Token: 0x04000320 RID: 800
	private static Class12.Delegate5 delegate5_0;

	// Token: 0x04000321 RID: 801
	private static Class12.Delegate4 delegate4_0;

	// Token: 0x04000322 RID: 802
	private static int int_3;

	// Token: 0x04000323 RID: 803
	private static bool bool_4;

	// Token: 0x04000324 RID: 804
	private static int[] int_4;

	// Token: 0x04000325 RID: 805
	private static bool bool_5;

	// Token: 0x04000326 RID: 806
	private static int int_5;

	// Token: 0x020000C0 RID: 192
	// (Invoke) Token: 0x060007A5 RID: 1957
	private delegate void Delegate1(object o);

	// Token: 0x020000C1 RID: 193
	internal class Attribute1 : Attribute
	{
		// Token: 0x060007A8 RID: 1960 RVA: 0x00002977 File Offset: 0x00000B77
		public Attribute1(object object_0)
		{
		}

		// Token: 0x060007A9 RID: 1961 RVA: 0x00002308 File Offset: 0x00000508
		// Note: this type is marked as 'beforefieldinit'.
		static Attribute1()
		{
			Class16.kLjw4iIsCLsZtxc4lksN0j();
		}

		// Token: 0x020000C2 RID: 194
		internal class Class13<T>
		{
			// Token: 0x060007AB RID: 1963 RVA: 0x00002308 File Offset: 0x00000508
			// Note: this type is marked as 'beforefieldinit'.
			static Class13()
			{
				Class16.kLjw4iIsCLsZtxc4lksN0j();
			}

			// Token: 0x060007AC RID: 1964 RVA: 0x00006AA1 File Offset: 0x00004CA1
			internal static bool smethod_0()
			{
				return Class12.Attribute1.Class13<T>.object_0 == null;
			}

			// Token: 0x04000327 RID: 807
			internal static object object_0;
		}
	}

	// Token: 0x020000C3 RID: 195
	internal class Class14
	{
		// Token: 0x060007AD RID: 1965 RVA: 0x0002359C File Offset: 0x0002179C
		internal static string smethod_0(string string_0, string string_1)
		{
			byte[] bytes = Encoding.Unicode.GetBytes(string_0);
			byte[] key = new byte[]
			{
				82,
				102,
				104,
				110,
				32,
				77,
				24,
				34,
				118,
				181,
				51,
				17,
				18,
				51,
				12,
				109,
				10,
				32,
				77,
				24,
				34,
				158,
				161,
				41,
				97,
				28,
				118,
				181,
				5,
				25,
				1,
				88
			};
			byte[] iv = Class12.smethod_8(Encoding.Unicode.GetBytes(string_1));
			MemoryStream memoryStream = new MemoryStream();
			SymmetricAlgorithm symmetricAlgorithm = Class12.smethod_6();
			symmetricAlgorithm.Key = key;
			symmetricAlgorithm.IV = iv;
			CryptoStream cryptoStream = new CryptoStream(memoryStream, symmetricAlgorithm.CreateEncryptor(), CryptoStreamMode.Write);
			cryptoStream.Write(bytes, 0, bytes.Length);
			cryptoStream.Close();
			return Convert.ToBase64String(memoryStream.ToArray());
		}

		// Token: 0x060007AF RID: 1967 RVA: 0x00002308 File Offset: 0x00000508
		// Note: this type is marked as 'beforefieldinit'.
		static Class14()
		{
			Class16.kLjw4iIsCLsZtxc4lksN0j();
		}
	}

	// Token: 0x020000C4 RID: 196
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate uint Delegate2(IntPtr classthis, IntPtr comp, IntPtr info, uint flags, IntPtr nativeEntry, ref uint nativeSizeOfCode);

	// Token: 0x020000C5 RID: 197
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate IntPtr Delegate3();

	// Token: 0x020000C6 RID: 198
	internal struct Struct1
	{
		// Token: 0x04000328 RID: 808
		internal bool bool_0;

		// Token: 0x04000329 RID: 809
		internal byte[] byte_0;
	}

	// Token: 0x020000C7 RID: 199
	internal class Class15
	{
		// Token: 0x060007BA RID: 1978 RVA: 0x00006AAB File Offset: 0x00004CAB
		public Class15(Stream stream_0)
		{
			this.binaryReader_0 = new BinaryReader(stream_0);
		}

		// Token: 0x060007BB RID: 1979 RVA: 0x00006ABF File Offset: 0x00004CBF
		internal Stream method_0()
		{
			return this.binaryReader_0.BaseStream;
		}

		// Token: 0x060007BC RID: 1980 RVA: 0x00006ACC File Offset: 0x00004CCC
		internal byte[] method_1(int int_0)
		{
			return this.binaryReader_0.ReadBytes(int_0);
		}

		// Token: 0x060007BD RID: 1981 RVA: 0x00006ADA File Offset: 0x00004CDA
		internal int method_2(byte[] byte_0, int int_0, int int_1)
		{
			return this.binaryReader_0.Read(byte_0, int_0, int_1);
		}

		// Token: 0x060007BE RID: 1982 RVA: 0x00006AEA File Offset: 0x00004CEA
		internal int method_3()
		{
			return this.binaryReader_0.ReadInt32();
		}

		// Token: 0x060007BF RID: 1983 RVA: 0x00006AF7 File Offset: 0x00004CF7
		internal void method_4()
		{
			this.binaryReader_0.Close();
		}

		// Token: 0x060007C0 RID: 1984 RVA: 0x00002308 File Offset: 0x00000508
		// Note: this type is marked as 'beforefieldinit'.
		static Class15()
		{
			Class16.kLjw4iIsCLsZtxc4lksN0j();
		}

		// Token: 0x0400032A RID: 810
		private BinaryReader binaryReader_0;
	}

	// Token: 0x020000C8 RID: 200
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	private delegate IntPtr Delegate4(IntPtr hModule, string lpName, uint lpType);

	// Token: 0x020000C9 RID: 201
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate IntPtr Delegate5(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

	// Token: 0x020000CA RID: 202
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int Delegate6(IntPtr hProcess, IntPtr lpBaseAddress, [In] [Out] byte[] buffer, uint size, out IntPtr lpNumberOfBytesWritten);

	// Token: 0x020000CB RID: 203
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int Delegate7(IntPtr lpAddress, int dwSize, int flNewProtect, ref int lpflOldProtect);

	// Token: 0x020000CC RID: 204
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate IntPtr Delegate8(uint dwDesiredAccess, int bInheritHandle, uint dwProcessId);

	// Token: 0x020000CD RID: 205
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int Delegate9(IntPtr ptr);

	// Token: 0x020000CE RID: 206
	[Flags]
	private enum Enum1
	{

	}
}
