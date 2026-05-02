using System;
using System.Reflection;

// Token: 0x020000BD RID: 189
internal class Class11
{
	// Token: 0x06000768 RID: 1896 RVA: 0x0002018C File Offset: 0x0001E38C
	internal static void smethod_0(int typemdt)
	{
		Type type = Class11.module_0.ResolveType(33554432 + typemdt);
		foreach (FieldInfo fieldInfo in type.GetFields())
		{
			MethodInfo method = (MethodInfo)Class11.module_0.ResolveMethod(fieldInfo.MetadataToken + 100663296);
			fieldInfo.SetValue(null, (MulticastDelegate)Delegate.CreateDelegate(type, method));
		}
	}

	// Token: 0x0600076A RID: 1898 RVA: 0x000068C3 File Offset: 0x00004AC3
	// Note: this type is marked as 'beforefieldinit'.
	static Class11()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
		Class11.module_0 = typeof(Class11).Assembly.ManifestModule;
	}

	// Token: 0x0600076B RID: 1899 RVA: 0x000068E3 File Offset: 0x00004AE3
	internal static bool smethod_1()
	{
		return Class11.object_0 == null;
	}

	// Token: 0x040002FD RID: 765
	internal static Module module_0;

	// Token: 0x040002FE RID: 766
	private static object object_0;

	// Token: 0x020000BE RID: 190
	internal delegate void Delegate0(object o);
}
