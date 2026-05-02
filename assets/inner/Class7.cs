using System;
using Microsoft.Win32;

// Token: 0x02000011 RID: 17
internal static class Class7
{
	// Token: 0x0600004E RID: 78 RVA: 0x00008968 File Offset: 0x00006B68
	internal static byte[] smethod_0(string string_0)
	{
		byte[] result;
		try
		{
			using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\" + Class4.smethod_0(), RegistryKeyPermissionCheck.ReadWriteSubTree))
			{
				if (registryKey == null)
				{
					result = null;
				}
				else
				{
					byte[] array = (byte[])registryKey.GetValue(string_0);
					if (array == null)
					{
						result = null;
					}
					else
					{
						result = array;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Class9.i(ex.Message);
			goto IL_53;
		}
		return result;
		IL_53:
		return null;
	}

	// Token: 0x0600004F RID: 79 RVA: 0x000089E8 File Offset: 0x00006BE8
	internal static void smethod_1(string string_0, object object_1)
	{
		try
		{
			using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\" + Class4.smethod_0(), RegistryKeyPermissionCheck.ReadWriteSubTree))
			{
				registryKey.SetValue(string_0, object_1, RegistryValueKind.Binary);
			}
		}
		catch (Exception ex)
		{
			Class9.i(ex.Message);
		}
	}

	// Token: 0x06000050 RID: 80 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static Class7()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x06000051 RID: 81 RVA: 0x00002424 File Offset: 0x00000624
	internal static bool smethod_2()
	{
		return Class7.object_0 == null;
	}

	// Token: 0x0400002B RID: 43
	internal static object object_0;
}
