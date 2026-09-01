using Asher.Services.Application;
using Asher.Services.Application.Contracts;
using Asher.Services.Hosting;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Asher.Host.Jsonl
{
    internal sealed class JsonlHostSession
    {
        private readonly IAsherApplication _application;
        private readonly SemaphoreSlim _stdoutLock = new(1, 1);
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _operations = new();
        private volatile bool _shutdownRequested;

        public JsonlHostSession(IAsherApplication application)
        {
            _application = application;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await WriteStdoutAsync(new JsonlEventMessage { Event = "ready" });

            while (!cancellationToken.IsCancellationRequested && !_shutdownRequested)
            {
                string? line;
                try
                {
                    line = await Console.In.ReadLineAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (line == null)
                    break;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                await HandleLineAsync(line);

                if (_shutdownRequested)
                    break;
            }

            await WaitForOperationsAsync();
        }

        private async Task HandleLineAsync(string line)
        {
            JsonlRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<JsonlRequest>(line, JsonlProtocol.SerializerOptions);
            }
            catch (JsonException ex)
            {
                await WriteResponseAsync(null, false, null, new JsonlError
                {
                    Code = JsonlProtocol.ErrorCodes.InvalidRequest,
                    Message = $"Invalid JSON: {ex.Message}"
                });
                return;
            }

            if (request == null)
            {
                await WriteResponseAsync(null, false, null, new JsonlError
                {
                    Code = JsonlProtocol.ErrorCodes.InvalidRequest,
                    Message = "Request payload is empty."
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(request.RequestId))
            {
                await WriteResponseAsync(null, false, null, new JsonlError
                {
                    Code = JsonlProtocol.ErrorCodes.InvalidRequest,
                    Message = "requestId is required."
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(request.Method))
            {
                await WriteResponseAsync(request.RequestId, false, null, new JsonlError
                {
                    Code = JsonlProtocol.ErrorCodes.InvalidRequest,
                    Message = "method is required."
                });
                return;
            }

            try
            {
                await DispatchAsync(request);
            }
            catch (Exception ex)
            {
                LogDiagnostic($"Unhandled error for {request.RequestId}/{request.Method}: {ex.Message}");
                await WriteResponseAsync(request.RequestId, false, null, new JsonlError
                {
                    Code = JsonlProtocol.ErrorCodes.InternalError,
                    Message = ex.Message
                });
            }
        }

        private async Task DispatchAsync(JsonlRequest request)
        {
            switch (request.Method)
            {
                case JsonlProtocol.Methods.Shutdown:
                    _shutdownRequested = true;
                    await WriteResponseAsync(request.RequestId, true, new { shuttingDown = true }, null);
                    return;

                case JsonlProtocol.Methods.Cancel:
                    await HandleCancelAsync(request);
                    return;

                case JsonlProtocol.Methods.GetSettings:
                    await WriteResponseAsync(request.RequestId, true, _application.GetSettings(), null);
                    return;

                case JsonlProtocol.Methods.SaveSettings:
                {
                    if (request.Params.ValueKind != JsonValueKind.Object)
                    {
                        await WriteInvalidParamsAsync(request.RequestId, "saveSettings requires a params object.");
                        return;
                    }

                    var settings = request.Params.Deserialize<ApplicationSettingsDto>(JsonlProtocol.SerializerOptions);
                    if (settings == null)
                    {
                        await WriteInvalidParamsAsync(request.RequestId, "saveSettings requires params.settings object.");
                        return;
                    }

                    _application.SaveSettings(settings);
                    await WriteResponseAsync(request.RequestId, true, new { saved = true }, null);
                    return;
                }

                case JsonlProtocol.Methods.GetApplicationMode:
                    await WriteResponseAsync(request.RequestId, true, new
                    {
                        mode = _application.GetApplicationMode()
                    }, null);
                    return;

                case JsonlProtocol.Methods.DetectGameFolder:
                    await WriteResponseAsync(request.RequestId, true, _application.DetectGameFolder(), null);
                    return;

                case JsonlProtocol.Methods.GetGameFolderInfo:
                {
                    if (!request.Params.TryGetProperty("folderPath", out var folderPathElement))
                    {
                        await WriteInvalidParamsAsync(request.RequestId, "getGameFolderInfo requires params.folderPath.");
                        return;
                    }

                    var folderPath = folderPathElement.GetString();
                    if (string.IsNullOrWhiteSpace(folderPath))
                    {
                        await WriteInvalidParamsAsync(request.RequestId, "getGameFolderInfo requires params.folderPath.");
                        return;
                    }

                    await WriteResponseAsync(request.RequestId, true, _application.GetGameFolderInfo(folderPath), null);
                    return;
                }

                case JsonlProtocol.Methods.ResolveGameFolderPath:
                    await WriteResponseAsync(request.RequestId, true, new
                    {
                        path = _application.ResolveGameFolderPath()
                    }, null);
                    return;

                case JsonlProtocol.Methods.IsGameInstalled:
                {
                    string? path = null;
                    if (request.Params.ValueKind == JsonValueKind.Object
                        && request.Params.TryGetProperty("gameFolderPath", out var pathElement))
                    {
                        path = pathElement.GetString();
                    }

                    await WriteResponseAsync(request.RequestId, true, new
                    {
                        installed = _application.IsGameInstalled(path)
                    }, null);
                    return;
                }

                case JsonlProtocol.Methods.HasRestorableBackup:
                {
                    string? path = null;
                    if (request.Params.ValueKind == JsonValueKind.Object
                        && request.Params.TryGetProperty("gameFolderPath", out var pathElement))
                    {
                        path = pathElement.GetString();
                    }

                    await WriteResponseAsync(request.RequestId, true, new
                    {
                        hasBackup = _application.HasRestorableBackup(path)
                    }, null);
                    return;
                }

                case JsonlProtocol.Methods.GetMods:
                    await RunCancellableAsync(request.RequestId, async ct =>
                    {
                        var mods = await _application.GetModsAsync(ct);
                        return (object)mods;
                    });
                    return;

                case JsonlProtocol.Methods.SetModEnabled:
                {
                    if (!request.Params.TryGetProperty("fileName", out var fileNameElement)
                        || !request.Params.TryGetProperty("enabled", out var enabledElement))
                    {
                        await WriteInvalidParamsAsync(request.RequestId, "setModEnabled requires params.fileName and params.enabled.");
                        return;
                    }

                    var fileName = fileNameElement.GetString();
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        await WriteInvalidParamsAsync(request.RequestId, "params.fileName must be a non-empty string.");
                        return;
                    }

                    await RunCancellableAsync(request.RequestId, async ct =>
                    {
                        var result = await _application.SetModEnabledAsync(fileName, enabledElement.GetBoolean(), ct);
                        return result;
                    });
                    return;
                }

                case JsonlProtocol.Methods.Install:
                {
                    var gameInfo = request.Params.Deserialize<GameFolderDto>(JsonlProtocol.SerializerOptions);
                    if (gameInfo == null)
                    {
                        await WriteInvalidParamsAsync(request.RequestId, "install requires params with game folder fields.");
                        return;
                    }

                    await RunCancellableWithProgressAsync(request.RequestId, (progress, ct) =>
                        _application.InstallAsync(gameInfo, progress, ct));
                    return;
                }

                case JsonlProtocol.Methods.Uninstall:
                {
                    string? path = null;
                    if (request.Params.ValueKind == JsonValueKind.Object
                        && request.Params.TryGetProperty("gameFolderPath", out var pathElement))
                    {
                        path = pathElement.GetString();
                    }

                    await RunCancellableWithProgressAsync(request.RequestId, (progress, ct) =>
                        _application.UninstallAsync(path, progress, ct));
                    return;
                }

                case JsonlProtocol.Methods.LaunchGame:
                {
                    var result = _application.LaunchGame();
                    if (result.Success)
                    {
                        await WriteResponseAsync(request.RequestId, true, result, null);
                        return;
                    }

                    await WriteResponseAsync(request.RequestId, false, result, new JsonlError
                    {
                        Code = JsonlProtocol.ErrorCodes.ApplicationError,
                        Message = result.ErrorMessage ?? "Launch failed."
                    });
                    return;
                }

                case JsonlProtocol.Methods.PreparePatchesFolder:
                {
                    if (!request.Params.TryGetProperty("gameFolderPath", out var folderPathElement))
                    {
                        await WriteInvalidParamsAsync(request.RequestId, "preparePatchesFolder requires params.gameFolderPath.");
                        return;
                    }

                    var gameFolderPath = folderPathElement.GetString();
                    if (string.IsNullOrWhiteSpace(gameFolderPath))
                    {
                        await WriteInvalidParamsAsync(request.RequestId, "params.gameFolderPath must be a non-empty string.");
                        return;
                    }

                    var prepareResult = _application.PreparePatchesFolder(gameFolderPath);
                    if (prepareResult.Success)
                    {
                        await WriteResponseAsync(request.RequestId, true, prepareResult, null);
                        return;
                    }

                    await WriteResponseAsync(request.RequestId, false, prepareResult, new JsonlError
                    {
                        Code = JsonlProtocol.ErrorCodes.ApplicationError,
                        Message = prepareResult.ErrorMessage ?? "Failed to prepare patches folder."
                    });
                    return;
                }

                case JsonlProtocol.Methods.MarkInstalled:
                {
                    if (!request.Params.TryGetProperty("gameFolderPath", out var folderPathElement)
                        || !request.Params.TryGetProperty("gameVersion", out var versionElement))
                    {
                        await WriteInvalidParamsAsync(request.RequestId, "markInstalled requires params.gameFolderPath and params.gameVersion.");
                        return;
                    }

                    var gameFolderPath = folderPathElement.GetString();
                    if (string.IsNullOrWhiteSpace(gameFolderPath))
                    {
                        await WriteInvalidParamsAsync(request.RequestId, "params.gameFolderPath must be a non-empty string.");
                        return;
                    }

                    _application.MarkInstalled(gameFolderPath, versionElement.GetString() ?? string.Empty);
                    await WriteResponseAsync(request.RequestId, true, new { marked = true }, null);
                    return;
                }

                case JsonlProtocol.Methods.MarkUninstalled:
                {
                    _application.MarkUninstalled();
                    await WriteResponseAsync(request.RequestId, true, new { marked = true }, null);
                    return;
                }

                default:
                    await WriteResponseAsync(request.RequestId, false, null, new JsonlError
                    {
                        Code = JsonlProtocol.ErrorCodes.UnknownMethod,
                        Message = $"Unknown method '{request.Method}'."
                    });
                    return;
            }
        }

        private async Task HandleCancelAsync(JsonlRequest request)
        {
            if (!request.Params.TryGetProperty("targetRequestId", out var targetElement))
            {
                await WriteInvalidParamsAsync(request.RequestId, "cancel requires params.targetRequestId.");
                return;
            }

            var targetRequestId = targetElement.GetString();
            if (string.IsNullOrWhiteSpace(targetRequestId))
            {
                await WriteInvalidParamsAsync(request.RequestId, "params.targetRequestId must be a non-empty string.");
                return;
            }

            if (_operations.TryRemove(targetRequestId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                await WriteResponseAsync(request.RequestId, true, new { cancelled = true, targetRequestId }, null);
                return;
            }

            await WriteResponseAsync(request.RequestId, false, null, new JsonlError
            {
                Code = JsonlProtocol.ErrorCodes.NotFound,
                Message = $"No in-flight operation found for requestId '{targetRequestId}'."
            });
        }

        private async Task RunCancellableAsync(string requestId, Func<CancellationToken, Task<object>> action)
        {
            using var operationCts = new CancellationTokenSource();
            if (!_operations.TryAdd(requestId, operationCts))
            {
                await WriteResponseAsync(requestId, false, null, new JsonlError
                {
                    Code = JsonlProtocol.ErrorCodes.InvalidRequest,
                    Message = $"Operation already in flight for requestId '{requestId}'."
                });
                return;
            }

            try
            {
                var result = await action(operationCts.Token);
                await WriteResponseAsync(requestId, true, result, null);
            }
            catch (OperationCanceledException)
            {
                await WriteResponseAsync(requestId, false, null, new JsonlError
                {
                    Code = JsonlProtocol.ErrorCodes.Cancelled,
                    Message = "Operation was cancelled."
                });
            }
            finally
            {
                _operations.TryRemove(requestId, out _);
            }
        }

        private async Task RunCancellableWithProgressAsync(
            string requestId,
            Func<IProgress<InstallationProgressDto>, CancellationToken, Task<InstallationResultDto>> action)
        {
            using var operationCts = new CancellationTokenSource();
            if (!_operations.TryAdd(requestId, operationCts))
            {
                await WriteResponseAsync(requestId, false, null, new JsonlError
                {
                    Code = JsonlProtocol.ErrorCodes.InvalidRequest,
                    Message = $"Operation already in flight for requestId '{requestId}'."
                });
                return;
            }

            var progress = new Progress<InstallationProgressDto>(update =>
            {
                if (operationCts.Token.IsCancellationRequested)
                    return;

                _ = WriteStdoutAsync(new JsonlProgressMessage
                {
                    RequestId = requestId,
                    Progress = update
                });
            });

            try
            {
                var result = await action(progress, operationCts.Token);
                await WriteResponseAsync(requestId, result.Success, result, result.Success
                    ? null
                    : new JsonlError
                    {
                        Code = JsonlProtocol.ErrorCodes.ApplicationError,
                        Message = result.ErrorMessage ?? result.Message
                    });
            }
            catch (OperationCanceledException)
            {
                await WriteResponseAsync(requestId, false, null, new JsonlError
                {
                    Code = JsonlProtocol.ErrorCodes.Cancelled,
                    Message = "Operation was cancelled."
                });
            }
            finally
            {
                _operations.TryRemove(requestId, out _);
            }
        }

        private async Task WriteInvalidParamsAsync(string requestId, string message) =>
            await WriteResponseAsync(requestId, false, null, new JsonlError
            {
                Code = JsonlProtocol.ErrorCodes.InvalidRequest,
                Message = message
            });

        private async Task WriteResponseAsync(
            string? requestId,
            bool success,
            object? result,
            JsonlError? error)
        {
            await WriteStdoutAsync(new JsonlResponse
            {
                RequestId = requestId,
                Success = success,
                Result = result,
                Error = error
            });
        }

        private async Task WriteStdoutAsync(object message)
        {
            var json = JsonSerializer.Serialize(message, JsonlProtocol.SerializerOptions);
            await _stdoutLock.WaitAsync();
            try
            {
                await Console.Out.WriteLineAsync(json);
                await Console.Out.FlushAsync();
            }
            finally
            {
                _stdoutLock.Release();
            }
        }

        private async Task WaitForOperationsAsync()
        {
            while (!_operations.IsEmpty)
                await Task.Delay(50);
        }

        private static void LogDiagnostic(string message) =>
            Console.Error.WriteLine($"[asher-host] {message}");

        public static async Task<int> RunFromHostAsync()
        {
            try
            {
                var host = AsherServiceHost.Create();
                using var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                };

                var session = new JsonlHostSession(host.Application);
                await session.RunAsync(cts.Token);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[asher-host] fatal: {ex.Message}");
                return 2;
            }
        }
    }
}
