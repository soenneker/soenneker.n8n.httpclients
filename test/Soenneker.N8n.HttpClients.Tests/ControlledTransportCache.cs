using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using Soenneker.Dtos.HttpClientOptions;

namespace Soenneker.N8n.HttpClients.Tests;

// Intercepts only the underlying transport factory. The real SSRF cache and its
// socket callback remain under test; no public or private network is contacted.
public class ControlledTransportCache : DispatchProxy
{
    public HttpClient? Client { get; private set; }
    public int PublicPort { get; set; }
    public bool Rebind { get; set; }
    public int Connections { get; private set; }

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method!.Name == "Get" && args!.Length == 4)
        {
            var options = (HttpClientOptions)((Delegate)args[2]!).DynamicInvoke(args[1])!;
            var handler = new SocketsHttpHandler { AllowAutoRedirect = options.AllowAutoRedirect ?? true };
            options.ModifyPrimaryHandler!(handler);
            var guardedConnect = handler.ConnectCallback!;
            handler.ConnectCallback = async (context, token) =>
            {
                Connections++;
                if (Rebind)
                {
                    // The URL was public when selected. The connection-time DNS
                    // endpoint now resolves locally, as with a changed DNS answer.
                    return await guardedConnect((SocketsHttpConnectionContext)Activator.CreateInstance(typeof(SocketsHttpConnectionContext),
                        BindingFlags.Instance | BindingFlags.NonPublic, null,
                        new object[] { new DnsEndPoint("localhost", PublicPort), context.InitialRequestMessage }, null)!, token);
                }
                if (context.DnsEndPoint.Host == "public.example")
                {
                    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    try { await socket.ConnectAsync(IPAddress.Loopback, PublicPort, token); }
                    catch { socket.Dispose(); throw; }
                    return new NetworkStream(socket, ownsSocket: true);
                }
                return await guardedConnect(context, token);
            };
            Client = new HttpClient(handler) { BaseAddress = options.BaseAddress, Timeout = TimeSpan.FromSeconds(5) };
            foreach (var header in options.DefaultRequestHeaders!)
                Client.DefaultRequestHeaders.Add(header.Key, header.Value);
            return ValueTask.FromResult(Client);
        }
        if (method.Name == "Remove") { Client?.Dispose(); return ValueTask.CompletedTask; }
        if (method.Name is "RemoveSync" or "Dispose") { Client?.Dispose(); return null; }
        throw new NotSupportedException(method.Name);
    }
}
