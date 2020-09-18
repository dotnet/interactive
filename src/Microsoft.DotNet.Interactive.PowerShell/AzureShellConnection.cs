// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.PowerShell.Host;
using Timer = System.Timers.Timer;

namespace Microsoft.DotNet.Interactive.PowerShell
{
    #region JsonTypes

    internal class IntToStringConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                ReadOnlySpan<byte> span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
                if (Utf8Parser.TryParse(span, out int number, out int bytesConsumed) && span.Length == bytesConsumed)
                {
                    return number;
                }

                if (int.TryParse(reader.GetString(), out number))
                {
                    return number;
                }
            }

            return reader.GetInt32();
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    internal class CloudShellTerminal
    {
        public string id { get; set; }
        public string socketUri { get; set; }
        [JsonConverter(typeof(IntToStringConverter))]
        public int idleTimeout { get; set; }
        public bool tokenUpdated { get; set; }
        public string rootDirectory { get; set; }
    }

    internal class AuthResponse
    {
        public string token_type { get; set; }
        public string scope { get; set; }
        [JsonConverter(typeof(IntToStringConverter))]
        public int expires_in { get; set; }
        [JsonConverter(typeof(IntToStringConverter))]
        public int ext_expires_in { get; set; }
        [JsonConverter(typeof(IntToStringConverter))]
        public int not_before { get; set; }
        public string resource { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string id_token { get; set; }
    }

    internal class AuthResponsePending
    {
        public string error { get; set; }
        public string error_description { get; set; }
        public int[] error_codes { get; set; }
        public string timestamp { get; set; }
        public string trace_id { get; set; }
        public string correlation_id { get; set; }
        public string error_uri { get; set; }
    }

    internal class CloudShellResponse
    {
        public Dictionary<string,string> properties { get; set; }
    }

    internal class DeviceCodeResponse
    {
        public string user_code { get; set; }
        public string device_code { get; set; }
        public string verification_url { get; set; }
        [JsonConverter(typeof(IntToStringConverter))]
        public int expires_in { get; set; }
        [JsonConverter(typeof(IntToStringConverter))]
        public int interval { get; set; }
        public string message { get; set; }
    }

    internal class AzureTenant
    {
        public string id { get; set; }
        public string tenantId { get; set; }
        public string countryCode { get; set; }
        public string displayName { get; set; }
        public string[] domains { get; set; }
    }

    internal class AzureTenantResponse
    {
        public AzureTenant[] value { get; set; }
    }

    internal class CloudShellSettings
    {
        public CloudShellSettingProperties properties { get; set; }
    }

    internal class CloudShellSettingProperties
    {
        public string preferredLocation { get; set; }
        public StorageProfile storageProfile { get; set; }
        public string preferredShellType { get; set; }
    }

    internal class StorageProfile
    {
        public string storageAccountResourceId { get; set; }
        public string fileShareName { get; set; }
        public int diskSizeInGB { get; set; }
    }

    internal class FailedRequest
    {
        public FailedRequestError error { get; set; }
    }

    internal class FailedRequestError
    {
        public string code { get; set; }
        public string message { get; set; }
    }

    #endregion

    internal class AzShellConnectionUtils : IDisposable
    {
        private const string ClientId = "245e1dee-74ef-4257-a8c8-8208296e1dfd";
        private const string UserAgent = "PowerShell.Enter-AzShell";
        private const string CommandToStartPwsh = "stty -echo && cat | pwsh -noninteractive -f - && exit";
        private const string CommandToSetPrompt = @"Remove-Item Function:\Prompt -Force; New-Item -Path Function:\Prompt -Value { ""8f0a62bb-598d-4355-a14e-e33867c62891`n"" } -Options constant -Force > $null; New-Alias -Name help -Value Get-Help -Force";
        private const string PwshPrompt = "8f0a62bb-598d-4355-a14e-e33867c62891\r\n";

        // Mimic 'ctrl+d': end of transmission.
        private static readonly byte[] _exitSessionCommand = new byte[] { 4 };

        private static string _cachedAccessToken;
        private static string _cachedRefreshToken;
        private static string _cachedTenantId;

        private readonly HttpClient _httpClient;
        private readonly ClientWebSocket _socket;
        private readonly Pipe _pipe;
        private readonly Timer _tokenRenewTimer;
        private readonly string _requestedTenantId;

        private bool _sessionInitialized;
        private TaskCompletionSource<object> _codeExecutedTaskSource;

        internal AzShellConnectionUtils(string tenantId)
            : this(tenantId != null && tenantId != _cachedTenantId)
        {
            _requestedTenantId = tenantId;
        }

        internal AzShellConnectionUtils(bool reset)
        {
            _httpClient = new HttpClient();
            _socket = new ClientWebSocket();
            _pipe = new Pipe();
            _tokenRenewTimer = new Timer() { AutoReset = false };
            _tokenRenewTimer.Elapsed += OnTimedEvent;

            if (reset)
            {
                ResetState();
            }
        }

        public void Dispose()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
            }

            if (_socket != null)
            {
                _socket.Dispose();
            }

            if (_tokenRenewTimer != null)
            {
                _tokenRenewTimer.Stop();
                _tokenRenewTimer.Elapsed -= OnTimedEvent;
                _tokenRenewTimer.Dispose();
            }
        }

        internal async Task<bool> ConnectAndInitializeAzShell(int terminalWidth, int terminalHeight)
        {
            if (_sessionInitialized)
            {
                throw new InvalidOperationException("Session has already been initialized.");
            }

            Console.WriteLine("Authenticating with Azure...");
            if (_cachedAccessToken != null)
            {
                try
                {
                    Console.Write("Renew the previous access token...");
                    await RefreshToken().ConfigureAwait(false);
                    Console.WriteLine("Succeeded.");
                }
                catch
                {
                    Console.WriteLine("Failed.\nStarting a new authentication...");
                    ResetState();
                }
            }

            if (_cachedAccessToken == null)
            {
                await GetDeviceCode().ConfigureAwait(false);
                _cachedTenantId = _requestedTenantId ?? await GetTenantId().ConfigureAwait(false);
                await RefreshToken().ConfigureAwait(false);
            }

            var userSettings = await ReadCloudShellUserSettings().ConfigureAwait(false);
            if (userSettings?.properties == null || userSettings.properties.storageProfile == null)
            {
                Console.WriteLine("It seems you haven't setup your cloud shell account yet. Navigate to https://shell.azure.com to complete account setup.");
                return false;
            }

            Console.Write("Requesting Cloud Shell...");
            string cloudShellUri = await RequestCloudShell().ConfigureAwait(false);
            Console.WriteLine("Succeeded.");

            Console.WriteLine("Connecting terminal...");
            string socketUri = await RequestTerminal(cloudShellUri, terminalWidth, terminalHeight).ConfigureAwait(false);

            await _socket.ConnectAsync(new Uri(socketUri), CancellationToken.None).ConfigureAwait(false);
            Task fillPipe = FillPipeAsync(_socket, _pipe.Writer);
            Task readPipe = ReadPipeAsync(_pipe.Reader);

            // Connection has established. Start pwsh.
            await SendCommand(CommandToStartPwsh).ConfigureAwait(false);

            // Wait for 2 seconds for the initialization to finish, e.g. the profile.
            await Task.Delay(2000).ConfigureAwait(false);
            await SendCommand(CommandToSetPrompt).ConfigureAwait(false);

            return true;
        }

        internal async Task ExitSession()
        {
            await SendCommand(_exitSessionCommand, waitForExecutionCompletion: false);
            _tokenRenewTimer.Stop();

            string color = VTColorUtils.CombineColorSequences(ConsoleColor.Green, VTColorUtils.DefaultConsoleColor);
            Console.Write($"{color}Azure Cloud Shell session ended.{VTColorUtils.ResetColor}\n");
            Console.Write($"{color}Submitted code will run in the local PowerShell sub kernel.{VTColorUtils.ResetColor}\n");
        }

        internal async Task SendCommand(string command, bool waitForExecutionCompletion = true)
        {
            if (waitForExecutionCompletion)
            {
                _codeExecutedTaskSource = new TaskCompletionSource<object>();
            }

            // The command could contain multi-line statement, which would require double line endings
            // for pwsh to accept the input.
            var buffer = Encoding.UTF8.GetBytes(command + "\n\n");
            await SendCommand(buffer, waitForExecutionCompletion);
        }

        private async Task SendCommand(byte[] command, bool waitForExecutionCompletion)
        {
            if (_socket.State != WebSocketState.Open)
            {
                string message = _socket.State switch
                {
                    WebSocketState.Closed => string.IsNullOrEmpty(_socket.CloseStatusDescription)
                        ? $"Session closed: {_socket.CloseStatus}"
                        : $"Session closed. {_socket.CloseStatus} => {_socket.CloseStatusDescription}",
                    WebSocketState.Aborted => "Session aborted!",
                    _ => $"Session is about to close. State: {_socket.State}"
                };

                throw new IOException(message);
            }

            await _socket.SendAsync(
                command.AsMemory(),
                WebSocketMessageType.Text,
                endOfMessage: true,
                CancellationToken.None);

            if (waitForExecutionCompletion)
            {
                await _codeExecutedTaskSource.Task;
                _codeExecutedTaskSource = null;
            }
        }

        private async void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            const int fiveMinInMSec = 300000;
            const int oneMinInMSec = 60000;

            try
            {
                await RefreshToken();
            }
            catch
            {
                // Give it another try.
                Timer timer = (Timer)source;
                double oldInterval = timer.Interval;
                double newInterval = oldInterval >= fiveMinInMSec 
                    ? fiveMinInMSec / 2
                    : oldInterval /  2;

                if (newInterval > oneMinInMSec)
                {
                    timer.Interval = newInterval;
                    timer.Start();
                }
            }
        }

        private void ResetState()
        {
            _cachedAccessToken = null;
            _cachedRefreshToken = null;
            _cachedTenantId = null;
        }

        private async Task FillPipeAsync(ClientWebSocket socket, PipeWriter writer)
        {
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    // Allocate at least 512 bytes from the PipeWriter
                    Memory<byte> memory = writer.GetMemory(512);
                    var receiveResult = await socket.ReceiveAsync(memory, CancellationToken.None);

                    // Tell the PipeWriter how much was read from the Socket
                    writer.Advance(receiveResult.Count);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Close message received",
                            CancellationToken.None);
                    }

                    // Make the data available to the PipeReader
                    FlushResult result = await writer.FlushAsync();
                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (Exception)
            {
                // report error
            }

            // Tell the PipeReader that there's no more data coming
            writer.Complete();
        }

        private async Task ReadPipeAsync(PipeReader reader)
        {
            // We depend on the echoed prompt string to determine if the code execution is
            // done. Note that the submitted code may contain multiple statements which will
            // result in multiple prompt strings (one after each statement) echoed back.
            // If we receive a prompt string and there is no more incoming bytes after that,
            // then we know the whole submitted code has done execution.
            bool potentialEndOfExecution = false;

            while (true)
            {
                if (!reader.TryRead(out ReadResult result))
                {
                    if (potentialEndOfExecution)
                    {
                        _codeExecutedTaskSource?.SetResult(null);
                    }
                    result = await reader.ReadAsync();
                }

                potentialEndOfExecution = false;
                ReadOnlySequence<byte> buffer = result.Buffer;

                while (true)
                {
                    // Look for a EOL in the buffer
                    var position = buffer.PositionOf((byte)'\n');
                    if (position == null)
                    {
                        break;
                    }

                    // Read the whole line.
                    position = buffer.GetPosition(1, position.Value);
                    string line = GetUtf8String(buffer.Slice(0, position.Value));

                    // Skip the bytes for that line.
                    buffer = buffer.Slice(position.Value);

                    if (_sessionInitialized)
                    {
                        potentialEndOfExecution = ProcessMessageDuringSession(
                            line,
                            buffer.Length == 0);
                    }
                    else
                    {
                        ProcessMessageAtSessionInitialization(line);
                    }
                }

                // Tell the PipeReader how much of the buffer we have consumed
                reader.AdvanceTo(buffer.Start, buffer.End);

                // Stop reading if there's no more data coming
                if (result.IsCompleted)
                {
                    break;
                }
            }

            // Mark the PipeReader as complete
            reader.Complete();
        }

        private bool ProcessMessageDuringSession(string line, bool noRemainingBytes)
        {
            if (PwshPrompt.Equals(line, StringComparison.Ordinal))
            {
                // The line is exactly the prompt string.
                return noRemainingBytes;
            }

            if (line.EndsWith(PwshPrompt, StringComparison.Ordinal))
            {
                if (line.StartsWith(VTColorUtils.EscapeCharacters))
                {
                    // The line is the prompt string with some escape sequences prepended.
                    return noRemainingBytes;
                }

                // This can happen when executing a native executable that writes some
                // output without an ending new line.
                int index = line.LastIndexOf(PwshPrompt);
                Console.Write(line.Remove(index));
            }
            else
            {
                Console.Write(line);
            }

            return false;
        }

        private void ProcessMessageAtSessionInitialization(string line)
        {
            // Handle incoming messages at session startup.
            if (line.Contains("MOTD:"))
            {
                // Let's display the message-of-the-day from PowerShell Azure Shell.
                Console.WriteLine("\n" + line);
                // Also, seeing this message means we are now in pwsh.
                _codeExecutedTaskSource?.SetResult(null);
            }
            else if (line.Contains("VERBOSE: "))
            {
                // Let's show the verbose message generated from the profile.
                Console.Write(line);
            }
            else if (line.Contains(CommandToSetPrompt))
            {
                // pwsh will echo the command passed to it.
                // It's okay to show incoming messages after this very first command is echoed back.
                _sessionInitialized = true;

                string color = VTColorUtils.CombineColorSequences(ConsoleColor.Green, VTColorUtils.DefaultConsoleColor);
                Console.WriteLine($"\n{color}Welcome to Azure Cloud Shell!{VTColorUtils.ResetColor}");
                Console.WriteLine($"{color}Submitted code will run in the Azure Cloud Shell, type 'exit' to quit.{VTColorUtils.ResetColor}");
            }
        }

        private string GetUtf8String(ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsSingleSegment)
            {
                return Encoding.UTF8.GetString(buffer.First.Span);
            }

            return Encoding.UTF8.GetString(buffer.ToArray());
        }

        private async Task RefreshToken()
        {
            const int fiveMinInSec = 300;
            const string resource = "https://management.core.windows.net/";
            string resourceUri = $"https://login.microsoftonline.com/{_cachedTenantId}/oauth2/token";
            string encodedResource = Uri.EscapeDataString(resource);
            string body = $"client_id={ClientId}&resource={encodedResource}&grant_type=refresh_token&refresh_token={_cachedRefreshToken}";

            HttpResponseMessage response = await SendWebRequest(
                resourceUri: resourceUri,
                body: body,
                contentType: "application/x-www-form-urlencoded",
                token: null,
                method: HttpMethod.Post
            );

            if (!response.IsSuccessStatusCode)
            {
                await ThrowForFailedRequest(response, "Failed to refresh the access token. {0}.");
            }

            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            var authResponse = JsonSerializer.Deserialize<AuthResponse>(bytes);
            _cachedAccessToken = authResponse.access_token;
            _cachedRefreshToken = authResponse.refresh_token;

            int expires_in = authResponse.expires_in;
            int interval = expires_in > 2 * fiveMinInSec ? expires_in - fiveMinInSec : expires_in / 2;
            _tokenRenewTimer.Interval = interval * 1000;
            _tokenRenewTimer.Start();
        }

        private async Task<string> RequestTerminal(string uri, int width, int height)
        {
            string resourceUri = $"{uri}/terminals?cols={width}&rows={height}&version=2019-01-01&shell=bash";
            HttpResponseMessage response = await SendWebRequest(
                resourceUri: resourceUri,
                body: string.Empty,
                contentType: "application/json",
                token: _cachedAccessToken,
                method: HttpMethod.Post
            );

            if (!response.IsSuccessStatusCode)
            {
                await ThrowForFailedRequest(response, "Failed to request a cloud shell terminal. {0}.");
            }

            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            var terminal = JsonSerializer.Deserialize<CloudShellTerminal>(bytes);
            return terminal.socketUri;
        }

        private async Task<string> GetTenantId()
        {
            const string resourceUri = "https://management.azure.com/tenants?api-version=2018-01-01";
            HttpResponseMessage response = await SendWebRequest(
                resourceUri: resourceUri,
                body: null,
                contentType: null,
                token: _cachedAccessToken,
                method: HttpMethod.Get
            );

            if (!response.IsSuccessStatusCode)
            {
                await ThrowForFailedRequest(response, "Failed to get the tenant Id. {0}.");
            }

            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            var tenant = JsonSerializer.Deserialize<AzureTenantResponse>(bytes);
            if (tenant.value.Length == 0)
            {
                throw new Exception("No tenants found!");
            }

            return tenant.value[0].tenantId;
        }

        private async Task<CloudShellSettings> ReadCloudShellUserSettings()
        {
            const string settingsUri = "https://management.azure.com/providers/Microsoft.Portal/userSettings/cloudconsole?api-version=2018-10-01";

            HttpResponseMessage response = await SendWebRequest(
                resourceUri: settingsUri,
                body: null,
                contentType: null,
                token: _cachedAccessToken,
                method: HttpMethod.Get
            );

            if (response.IsSuccessStatusCode)
            {
                byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                return JsonSerializer.Deserialize<CloudShellSettings>(bytes);
            }

            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                await ThrowForFailedRequest(response, "Failed to get the cloud shell user settings. {0}.");
            }

            // The user setting cannot be found.
            return null;
        }

        private async Task<string> RequestCloudShell()
        {
            const string resourceUri = "https://management.azure.com/providers/Microsoft.Portal/consoles/default?api-version=2018-10-01";
            const string body = @"
                {
                    ""Properties"": {
                        ""consoleRequestProperties"": {
                        ""osType"": ""linux""
                        }
                    }
                }
                ";

            HttpResponseMessage response = await SendWebRequest(
                resourceUri: resourceUri,
                body: body,
                contentType: "application/json",
                token: _cachedAccessToken,
                method: HttpMethod.Put
            );

            if (!response.IsSuccessStatusCode)
            {
                await ThrowForFailedRequest(response, "Failed to request for the cloud shell URI. {0}.");
            }

            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            var cloudShellResponse = JsonSerializer.Deserialize<CloudShellResponse>(bytes);
            return cloudShellResponse.properties["uri"];
        }

        private async Task GetDeviceCode()
        {
            const string resource = "https://management.core.windows.net/";
            string resourceUri = "https://login.microsoftonline.com/common/oauth2/devicecode";
            string encodedResource = Uri.EscapeDataString(resource);
            string body = $"client_id={ClientId}&resource={encodedResource}";

            HttpResponseMessage response = await SendWebRequest(
                resourceUri: resourceUri,
                body: body,
                contentType: "application/x-www-form-urlencoded",
                token: null,
                method: HttpMethod.Post
            );

            if (!response.IsSuccessStatusCode)
            {
                await ThrowForFailedRequest(response, "Failed to get the device code for authentication. {0}.");
            }

            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            var deviceCode = JsonSerializer.Deserialize<DeviceCodeResponse>(bytes);
            Console.WriteLine(deviceCode.message);

            resourceUri = "https://login.microsoftonline.com/common/oauth2/token";
            body = $"grant_type=device_code&resource={encodedResource}&client_id={ClientId}&code={deviceCode.device_code}";

            // poll until user authenticates
            for (int count = 0; count < deviceCode.expires_in / deviceCode.interval; count++)
            {
                response = await SendWebRequest(
                    resourceUri: resourceUri,
                    body: body,
                    contentType: "application/x-www-form-urlencoded",
                    token: null,
                    method: HttpMethod.Post
                );

                if (response.IsSuccessStatusCode)
                {
                    // Authentication was successful.
                    bytes = await response.Content.ReadAsByteArrayAsync();
                    var authResponse = JsonSerializer.Deserialize<AuthResponse>(bytes);

                    _cachedAccessToken = authResponse.access_token;
                    _cachedRefreshToken = authResponse.refresh_token;

                    break;
                }

                if (response.StatusCode != HttpStatusCode.BadRequest)
                {
                    // Unexpected request failure.
                    await ThrowForFailedRequest(response, "Failed to poll for authentication. {0}.");
                }

                // Status code was 'Bad Request'. It's possible we are still pending on authentication.
                bytes = await response.Content.ReadAsByteArrayAsync();
                var authResponsePending = JsonSerializer.Deserialize<AuthResponsePending>(bytes);

                if (!authResponsePending.error.Equals("authorization_pending", StringComparison.Ordinal))
                {
                    throw new HttpRequestException($"Authentication failed: {authResponsePending.error_description}");
                }

                Thread.Sleep(deviceCode.interval * 1000);
            }
        }

        private async Task<HttpResponseMessage> SendWebRequest(
            string resourceUri,
            string body,
            string contentType,
            string token,
            HttpMethod method)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

            if (token != null)
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(resourceUri),
                Method = method
            };

            if (body != null)
            {
                request.Content = new StringContent(body, Encoding.UTF8, contentType);
            }

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return await _httpClient.SendAsync(request);
        }

        private async Task ThrowForFailedRequest(HttpResponseMessage response, string parentErrorTemplate)
        {
            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            var badRequest = JsonSerializer.Deserialize<FailedRequest>(bytes);

            string detailedError = badRequest?.error == null
                ? response.ReasonPhrase
                : $"{response.ReasonPhrase}: {badRequest.error.code}. {badRequest.error.message}.";

            throw new HttpRequestException(string.Format(parentErrorTemplate, detailedError));
        }
    }
}
