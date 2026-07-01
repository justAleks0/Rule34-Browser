using System.Net;
using System.Text.Json;
using Rule34Gallery.Core.Remote;

namespace Rule34GalleryApp.Services;

public sealed class RemoteControlServer : IDisposable
{
    private readonly Func<IRemoteControlBridge> _bridgeFactory;
    private readonly object _gate = new();
    private LanHttpServer? _server;
    private string _token = string.Empty;
    private bool _enabled;
    private int _port = RemoteProtocol.DefaultPort;
    private string _pairingPin = string.Empty;
    private DateTime _pairingPinExpiresUtc = DateTime.MinValue;

    public RemoteControlServer(Func<IRemoteControlBridge> bridgeFactory) => _bridgeFactory = bridgeFactory;

    public bool IsRunning { get; private set; }

    public IReadOnlyList<string> ActivePrefixes { get; private set; } = [];

    public string? StartError { get; private set; }

    public string PairingPin
    {
        get
        {
            lock (_gate)
            {
                return _pairingPin;
            }
        }
    }

    public TimeSpan PairingPinTimeRemaining
    {
        get
        {
            lock (_gate)
            {
                var remaining = _pairingPinExpiresUtc - DateTime.UtcNow;
                return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
            }
        }
    }

    public void RefreshPairingPin()
    {
        lock (_gate)
        {
            _pairingPin = RemotePairingPin.Generate();
            _pairingPinExpiresUtc = DateTime.UtcNow.AddMinutes(15);
        }
    }

    public void ApplySettings(UserSettings settings)
    {
        lock (_gate)
        {
            _token = settings.RemoteControlToken ?? string.Empty;
            _port = settings.RemoteControlPort > 0 ? settings.RemoteControlPort : RemoteProtocol.DefaultPort;
            var wasEnabled = _enabled;
            _enabled = settings.RemoteControlEnabled && !string.IsNullOrWhiteSpace(_token);
            if (_enabled)
            {
                if (!wasEnabled || string.IsNullOrWhiteSpace(_pairingPin))
                {
                    RefreshPairingPin();
                }

                StartListener(_port);
            }
            else
            {
                StopListener();
                _pairingPin = string.Empty;
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            StopListener();
        }
    }

    private void StartListener(int port)
    {
        StopListener();
        StartError = null;

        var server = new LanHttpServer();
        try
        {
            server.Start(port, HandleRequestAsync);
        }
        catch (Exception ex)
        {
            StartError = $"Could not start remote server (port {port}). Try another port: {ex.Message}";
            server.Dispose();
            return;
        }

        _server = server;
        IsRunning = true;
        var prefixes = new List<string> { $"http://127.0.0.1:{port}/" };
        foreach (var ip in RemoteNetworkHelper.GetLocalIPv4Addresses())
        {
            prefixes.Add($"http://{ip}:{port}/");
        }

        ActivePrefixes = prefixes;
    }

    private void StopListener()
    {
        IsRunning = false;
        ActivePrefixes = [];
        if (_server is not null)
        {
            try
            {
                _server.Dispose();
            }
            catch
            {
                // ignore
            }

            _server = null;
        }
    }

    private async Task HandleRequestAsync(SimpleHttpRequest request)
    {
        try
        {
            var path = request.Path;
            var method = request.Method;

            if (path.Equals("/api/ping", StringComparison.OrdinalIgnoreCase) && method == "GET")
            {
                await request.WriteJsonAsync(Serialize(new RemotePingResponse
                {
                    RemoteEnabled = _enabled,
                    RequiresToken = _enabled,
                })).ConfigureAwait(false);
                return;
            }

            if (path.Equals("/api/pair", StringComparison.OrdinalIgnoreCase) && method == "POST")
            {
                await HandlePairAsync(request).ConfigureAwait(false);
                return;
            }

            if (!_enabled)
            {
                await request.WriteJsonAsync(Serialize(RemoteCommandResponse.Fail("Remote control is disabled on this PC.")),
                    HttpStatusCode.Forbidden).ConfigureAwait(false);
                return;
            }

            if (!IsAuthorized(request))
            {
                await request.WriteJsonAsync(Serialize(RemoteCommandResponse.Fail("Invalid remote token.")),
                    HttpStatusCode.Unauthorized).ConfigureAwait(false);
                return;
            }

            RemoteClientSession.RecordActivity();

            if (path.Equals("/api/state", StringComparison.OrdinalIgnoreCase) && method == "GET")
            {
                var state = _bridgeFactory().CaptureState();
                await request.WriteJsonAsync(Serialize(RemoteCommandResponse.Success(state))).ConfigureAwait(false);
                return;
            }

            if (path.Equals("/api/command", StringComparison.OrdinalIgnoreCase) && method == "POST")
            {
                RemoteCommandRequest? command;
                try
                {
                    command = JsonSerializer.Deserialize<RemoteCommandRequest>(request.Body, RemoteProtocol.JsonOptions);
                }
                catch
                {
                    await request.WriteJsonAsync(Serialize(RemoteCommandResponse.Fail("Invalid JSON body.")),
                        HttpStatusCode.BadRequest).ConfigureAwait(false);
                    return;
                }

                if (command is null || string.IsNullOrWhiteSpace(command.Type))
                {
                    await request.WriteJsonAsync(Serialize(RemoteCommandResponse.Fail("Missing command type.")),
                        HttpStatusCode.BadRequest).ConfigureAwait(false);
                    return;
                }

                var result = await _bridgeFactory().ExecuteAsync(command, CancellationToken.None).ConfigureAwait(false);
                await request.WriteJsonAsync(Serialize(result),
                    result.Ok ? HttpStatusCode.OK : HttpStatusCode.BadRequest).ConfigureAwait(false);
                return;
            }

            await request.WriteJsonAsync(Serialize(RemoteCommandResponse.Fail("Not found.")), HttpStatusCode.NotFound)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            try
            {
                await request.WriteJsonAsync(Serialize(RemoteCommandResponse.Fail(ex.Message)),
                    HttpStatusCode.InternalServerError).ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        }
    }

    private async Task HandlePairAsync(SimpleHttpRequest request)
    {
        if (!_enabled)
        {
            await request.WriteJsonAsync(Serialize(RemotePairResponse.Fail("Remote control is disabled on this PC.")),
                HttpStatusCode.Forbidden).ConfigureAwait(false);
            return;
        }

        RemotePairRequest? body;
        try
        {
            body = JsonSerializer.Deserialize<RemotePairRequest>(request.Body, RemoteProtocol.JsonOptions);
        }
        catch
        {
            await request.WriteJsonAsync(Serialize(RemotePairResponse.Fail("Invalid JSON body.")), HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
            return;
        }

        var pin = body?.Pin?.Trim() ?? string.Empty;
        if (!RemotePairingPin.IsValidFormat(pin))
        {
            await request.WriteJsonAsync(
                    Serialize(RemotePairResponse.Fail("Enter the 6-digit code shown on the PC.")),
                    HttpStatusCode.BadRequest)
                .ConfigureAwait(false);
            return;
        }

        var token = string.Empty;
        var port = RemoteProtocol.DefaultPort;
        string? pairError = null;
        HttpStatusCode pairCode = HttpStatusCode.OK;
        lock (_gate)
        {
            if (DateTime.UtcNow > _pairingPinExpiresUtc)
            {
                pairError = "Pairing code expired. Tap New code on the PC.";
                pairCode = HttpStatusCode.BadRequest;
            }
            else if (!string.Equals(pin, _pairingPin, StringComparison.Ordinal))
            {
                pairError = "Wrong pairing code.";
                pairCode = HttpStatusCode.Unauthorized;
            }
            else
            {
                token = _token;
                port = _port;
            }
        }

        if (pairError is not null)
        {
            await request.WriteJsonAsync(Serialize(RemotePairResponse.Fail(pairError)), pairCode).ConfigureAwait(false);
            return;
        }

        var host = request.GetRequestHost();
        if (string.IsNullOrWhiteSpace(host) || host == "127.0.0.1" || host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            host = RemoteNetworkHelper.GetLocalIPv4Addresses().FirstOrDefault() ?? "127.0.0.1";
        }

        await request.WriteJsonAsync(Serialize(RemotePairResponse.Success(host, port, token))).ConfigureAwait(false);
    }

    private bool IsAuthorized(SimpleHttpRequest request)
    {
        if (request.Headers.TryGetValue(RemoteProtocol.TokenHeader, out var header) &&
            !string.IsNullOrWhiteSpace(header) &&
            string.Equals(header.Trim(), _token, StringComparison.Ordinal))
        {
            return true;
        }

        var query = request.GetQueryValue("token");
        return !string.IsNullOrWhiteSpace(query) &&
               string.Equals(query.Trim(), _token, StringComparison.Ordinal);
    }

    private static string Serialize(object payload) =>
        JsonSerializer.Serialize(payload, RemoteProtocol.JsonOptions);
}
