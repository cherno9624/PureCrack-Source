using System;
using System.Linq;
using System.Threading;

// Token: 0x02000007 RID: 7
internal class Class2
{
	// Token: 0x06000018 RID: 24 RVA: 0x00006E18 File Offset: 0x00005018
	internal void method_0(GClass2 gclass2_0)
	{
		try
		{
			if (gclass2_0 != null)
			{
				if (gclass2_0 is GClass8)
				{
					int int32_ = Interlocked.CompareExchange(ref Class9.int_1, 0, 0);
					try
					{
						Timer timer_ = Class9.timer_1;
						if (timer_ != null)
						{
							timer_.Dispose();
						}
					}
					catch
					{
					}
					Class9.h(new GClass8
					{
						HasValue = true,
						Int32_0 = int32_,
						String_1 = Class3.smethod_2(),
						String_0 = Class3.d(),
						Byte_0 = Class4.i(60)
					});
					try
					{
						Interlocked.Exchange(ref Class9.int_1, 0);
					}
					catch
					{
					}
				}
				else
				{
					GClass3 gclass = gclass2_0 as GClass3;
					if (gclass != null)
					{
						byte[] array = GClass1.smethod_0(gclass);
						if (array != null)
						{
							Class7.smethod_1(Class4.smethod_0(), array);
							Class9.smethod_9();
						}
					}
					else
					{
						GClass11 gclass2 = gclass2_0 as GClass11;
						if (gclass2 != null)
						{
							byte[] array2;
							if (gclass2.Byte_0 != null)
							{
								Class7.smethod_1(gclass2.String_0, gclass2.Byte_0);
								array2 = gclass2.Byte_0;
							}
							else
							{
								array2 = Class7.smethod_0(gclass2.String_0);
								if (array2 == null)
								{
									Class9.h(gclass2);
									return;
								}
							}
							new Class5().method_0(GClass1.smethod_0(new GClass7
							{
								GClass4_0 = Class9.smethod_1(),
								zPjUxLdehl = gclass2.String_1
							}), GClass14.smethod_1(array2.Reverse<byte>().ToArray<byte>()));
							GC.Collect();
						}
						else
						{
							GClass10 gclass3 = gclass2_0 as GClass10;
							if (gclass3 != null)
							{
								Class1.smethod_0(gclass3);
							}
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Class9.i(ex.Message);
		}
	}

	// Token: 0x0600001A RID: 26 RVA: 0x00002308 File Offset: 0x00000508
	// Note: this type is marked as 'beforefieldinit'.
	static Class2()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
	}

	// Token: 0x0600001B RID: 27 RVA: 0x00002353 File Offset: 0x00000553
	internal static bool smethod_0()
	{
		return Class2.object_0 == null;
	}

	// Token: 0x0400000E RID: 14
	internal static object object_0;
}
