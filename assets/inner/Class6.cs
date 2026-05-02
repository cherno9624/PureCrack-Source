using System;
using System.Threading;

// Token: 0x02000010 RID: 16
internal static class Class6
{
	// Token: 0x0600004A RID: 74 RVA: 0x000088AC File Offset: 0x00006AAC
	internal static bool smethod_0(string string_0)
	{
		bool result;
		try
		{
			Class6.mutex_0 = new Mutex(false, string_0);
			result = Class6.mutex_0.WaitOne(TimeSpan.FromSeconds(15.0), false);
		}
		catch (AbandonedMutexException)
		{
			result = true;
		}
		catch (Exception)
		{
			result = false;
		}
		return result;
	}

	// Token: 0x0600004B RID: 75 RVA: 0x0000890C File Offset: 0x00006B0C
	internal static void smethod_1()
	{
		try
		{
			if (Class6.mutex_0 != null)
			{
				using (Mutex mutex = Class6.mutex_0)
				{
					mutex.ReleaseMutex();
					mutex.Close();
					mutex.Dispose();
				}
			}
		}
		catch
		{
		}
	}

	// Token: 0x0600004C RID: 76 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static Class6()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x0600004D RID: 77 RVA: 0x0000241A File Offset: 0x0000061A
	internal static bool smethod_2()
	{
		return Class6.object_0 == null;
	}

	// Token: 0x04000029 RID: 41
	private static Mutex mutex_0;

	// Token: 0x0400002A RID: 42
	internal static object object_0;
}
