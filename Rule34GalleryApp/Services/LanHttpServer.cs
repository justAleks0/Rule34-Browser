using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Rule34GalleryApp.Services;

/// <summary>
/// Minimal HTTP/1.1 server over TCP. Avoids Windows HttpListener URL ACL requirements for LAN IPs.
/// </summary>
public sealed class LanHttpServer : IDisposable
{
    private const int MaxHeaderBytes = 32 * 1024;
    private const int MaxBodyBytes = 1024 * 1024;

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Func<SimpleHttpRequest, Task>? _handler;

    public bool IsRunning { get; private set; }

    public void Start(int port, Func<SimpleHttpRequest, Task> handler)
    {
        Stop();
        _handler = handler;
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        _cts = new CancellationTokenSource();
        IsRunning = true;
        _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        IsRunning = false;
        try
        {
            _cts?.Cancel();
        }
        catch
        {
            // ignore
        }

        _cts = null;
        if (_listener is not null)
        {
            try
            {
                _listener.Stop();
            }
            catch
            {
                // ignore
            }

            _listener = null;
        }
    }

    public void Dispose() => Stop();

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener is not null)
        {
            TcpClient? client = null;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                continue;
            }

            if (client is not null)
            {
                _ = Task.Run(() => HandleClientAsync(client), cancellationToken);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            client.ReceiveTimeout = 30_000;
            await using var stream = client.GetStream();
            var request = await ReadRequestAsync(stream).ConfigureAwait(false);
            if (request is null || _handler is null)
            {
                return;
            }

            await _handler(request).ConfigureAwait(false);
        }
        catch
        {
            // ignore per-connection errors
        }
        finally
        {
            try
            {
                client.Close();
            }
            catch
            {
                // ignore
            }
        }
    }

    private static async Task<SimpleHttpRequest?> ReadRequestAsync(NetworkStream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);
        var requestLine = await reader.ReadLineAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(requestLine))
        {
            return null;
        }

        var tokens = requestLine.Split([' '], 3, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2)
        {
            return null;
        }

        var method = tokens[0].ToUpperInvariant();
        var target = tokens[1];
        string path;
        string query;
        if (target.StartsWith('/'))
        {
            var qIndex = target.IndexOf('?');
            if (qIndex >= 0)
            {
                path = target[..qIndex].TrimEnd('/');
                query = target[(qIndex + 1)..];
            }
            else
            {
                path = target.TrimEnd('/');
                query = string.Empty;
            }
        }
        else if (Uri.TryCreate(target, UriKind.Absolute, out var uri))
        {
            path = uri.AbsolutePath.TrimEnd('/');
            query = uri.Query.TrimStart('?');
        }
        else
        {
            return null;
        }

        var headerBytes = 0;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (true)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                return null;
            }

            if (line.Length == 0)
            {
                break;
            }

            headerBytes += line.Length + 2;
            if (headerBytes > MaxHeaderBytes)
            {
                return null;
            }

            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var name = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();
            headers[name] = value;
        }

        var body = string.Empty;
        if (headers.TryGetValue("Content-Length", out var lengthText) &&
            int.TryParse(lengthText, out var contentLength) &&
            contentLength > 0)
        {
            if (contentLength > MaxBodyBytes)
            {
                return null;
            }

            var buffer = new char[contentLength];
            var read = 0;
            while (read < contentLength)
            {
                var n = await reader.ReadBlockAsync(buffer, read, contentLength - read).ConfigureAwait(false);
                if (n <= 0)
                {
                    break;
                }

                read += n;
            }

            body = new string(buffer, 0, read);
        }

        headers.TryGetValue("Host", out var hostHeader);
        return new SimpleHttpRequest(method, path, query, hostHeader ?? string.Empty, headers, body, stream);
    }
}

public sealed class SimpleHttpRequest
{
    private readonly NetworkStream _responseStream;

    public SimpleHttpRequest(
        string method,
        string path,
        string query,
        string hostHeader,
        IReadOnlyDictionary<string, string> headers,
        string body,
        NetworkStream responseStream)
    {
        Method = method;
        Path = path;
        Query = query;
        HostHeader = hostHeader;
        Headers = headers;
        Body = body;
        _responseStream = responseStream;
    }

    public string Method { get; }

    public string Path { get; }

    public string Query { get; }

    public string HostHeader { get; }

    public IReadOnlyDictionary<string, string> Headers { get; }

    public string Body { get; }

    public string? GetQueryValue(string key)
    {
        if (string.IsNullOrEmpty(Query))
        {
            return null;
        }

        foreach (var part in Query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(kv[1]);
            }
        }

        return null;
    }

    public string GetRequestHost()
    {
        if (!string.IsNullOrWhiteSpace(HostHeader))
        {
            var host = HostHeader.Split(':', 2)[0].Trim();
            if (!string.IsNullOrWhiteSpace(host))
            {
                return host;
            }
        }

        return string.Empty;
    }

    public async Task WriteJsonAsync(string json, HttpStatusCode code = HttpStatusCode.OK)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var status = code switch
        {
            HttpStatusCode.OK => "200 OK",
            HttpStatusCode.BadRequest => "400 Bad Request",
            HttpStatusCode.Unauthorized => "401 Unauthorized",
            HttpStatusCode.Forbidden => "403 Forbidden",
            HttpStatusCode.NotFound => "404 Not Found",
            HttpStatusCode.InternalServerError => "500 Internal Server Error",
            _ => $"{(int)code} {(HttpStatusCode)code}",
        };

        var header = new StringBuilder();
        header.Append("HTTP/1.1 ").Append(status).Append("\r\n");
        header.Append("Content-Type: application/json; charset=utf-8\r\n");
        header.Append("Content-Length: ").Append(bytes.Length).Append("\r\n");
        header.Append("Connection: close\r\n");
        header.Append("Access-Control-Allow-Origin: *\r\n");
        header.Append("\r\n");

        var headerBytes = Encoding.UTF8.GetBytes(header.ToString());
        await _responseStream.WriteAsync(headerBytes).ConfigureAwait(false);
        await _responseStream.WriteAsync(bytes).ConfigureAwait(false);
        await _responseStream.FlushAsync().ConfigureAwait(false);
    }
}
