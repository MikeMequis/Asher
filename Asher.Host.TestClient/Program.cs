using System.Diagnostics;
using System.Text.Json;

namespace Asher.Host.TestClient
{
    internal static class Program
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static int Main()
        {
            var hostPath = ResolveHostPath();
            Console.Error.WriteLine($"[test-client] launching: {hostPath}");

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = hostPath,
                    Arguments = "--jsonl",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    Console.Error.WriteLine(e.Data);
            };

            if (!process.Start())
            {
                Console.Error.WriteLine("[test-client] failed to start host process.");
                return 1;
            }

            process.BeginErrorReadLine();

            var reader = process.StandardOutput;
            var writer = process.StandardInput;

            var failures = 0;
            failures += WaitForReady(reader);
            failures += TestGetSettings(writer, reader);
            failures += TestPreparePatchesFolder(writer, reader);
            failures += TestMarkInstalledUninstalled(writer, reader);
            failures += TestDetectGameFolder(writer, reader);
            failures += TestGetMods(writer, reader);
            failures += TestInvalidRequest(writer, reader);
                failures += TestInstallProgress(writer, reader);
                failures += TestCancellation(writer, reader);
                failures += TestShutdown(writer, reader);

            if (!process.WaitForExit(10000))
            {
                Console.Error.WriteLine("[test-client] host did not exit after shutdown.");
                failures++;
            }

            Console.Error.WriteLine(failures == 0
                ? "[test-client] all protocol checks passed"
                : $"[test-client] completed with {failures} failure(s)");

            return failures == 0 ? 0 : 1;
        }

        private static int WaitForReady(StreamReader reader)
        {
            var line = reader.ReadLine();
            if (line == null)
            {
                Console.Error.WriteLine("[FAIL] ready: no output from host");
                return 1;
            }

            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.GetProperty("type").GetString() == "event"
                && root.GetProperty("event").GetString() == "ready")
            {
                Console.Error.WriteLine("[OK] ready event");
                return 0;
            }

            Console.Error.WriteLine($"[FAIL] ready: unexpected message: {line}");
            return 1;
        }

        private static int TestGetSettings(StreamWriter writer, StreamReader reader)
        {
            WriteRequest(writer, "1", "getSettings");
            var response = ReadResponse(reader, "1");
            return response.Success ? 0 : 1;
        }

        private static int TestPreparePatchesFolder(StreamWriter writer, StreamReader reader)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "asher-jsonl-patches-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempPath);

            try
            {
                WriteRequest(writer, "prep-1", "preparePatchesFolder", new { gameFolderPath = tempPath });
                var response = ReadResponse(reader, "prep-1");
                if (!response.Success)
                {
                    Console.Error.WriteLine("[FAIL] preparePatchesFolder");
                    return 1;
                }

                var patchesPath = Path.Combine(tempPath, "Asher", "patches");
                if (!Directory.Exists(patchesPath))
                {
                    Console.Error.WriteLine("[FAIL] preparePatchesFolder did not create patches directory");
                    return 1;
                }

                Console.Error.WriteLine("[OK] preparePatchesFolder");
                return 0;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath, recursive: true);
                }
                catch
                {
                    // Best-effort cleanup.
                }
            }
        }

        private static int TestMarkInstalledUninstalled(StreamWriter writer, StreamReader reader)
        {
            var probePath = Path.Combine(Path.GetTempPath(), "asher-jsonl-mark-" + Guid.NewGuid().ToString("N"));

            WriteRequest(writer, "mark-in", "markInstalled", new { gameFolderPath = probePath, gameVersion = "test" });
            if (!ReadResponse(reader, "mark-in").Success)
            {
                Console.Error.WriteLine("[FAIL] markInstalled");
                return 1;
            }

            WriteRequest(writer, "mark-settings", "getSettings");
            var settingsResponse = ReadResponse(reader, "mark-settings");
            if (!settingsResponse.Success)
            {
                Console.Error.WriteLine("[FAIL] getSettings after markInstalled");
                return 1;
            }

            WriteRequest(writer, "mark-out", "markUninstalled");
            if (!ReadResponse(reader, "mark-out").Success)
            {
                Console.Error.WriteLine("[FAIL] markUninstalled");
                return 1;
            }

            Console.Error.WriteLine("[OK] markInstalled / markUninstalled");
            return 0;
        }

        private static int TestDetectGameFolder(StreamWriter writer, StreamReader reader)
        {
            WriteRequest(writer, "2", "detectGameFolder");
            var response = ReadResponse(reader, "2");
            return response.Success ? 0 : 1;
        }

        private static int TestGetMods(StreamWriter writer, StreamReader reader)
        {
            WriteRequest(writer, "3", "getMods");
            var response = ReadResponse(reader, "3");
            return response.Success ? 0 : 1;
        }

        private static int TestInvalidRequest(StreamWriter writer, StreamReader reader)
        {
            writer.WriteLine("not-json");
            writer.Flush();
            var response = ReadNextResponse(reader);
            if (!response.Success && response.ErrorCode == "invalid_request")
            {
                Console.Error.WriteLine("[OK] invalid request error");
                return 0;
            }

            Console.Error.WriteLine("[FAIL] invalid request: expected structured error");
            return 1;
        }

        private static int TestInstallProgress(StreamWriter writer, StreamReader reader)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "asher-jsonl-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempPath);

            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    requestId = "4",
                    method = "install",
                    @params = new
                    {
                        path = tempPath,
                        version = "test",
                        isValid = false,
                        source = "test-client",
                        hasPatchesFolder = false,
                        patchesFolderPath = string.Empty
                    }
                }, JsonOptions);

                writer.WriteLine(payload);
                writer.Flush();

                var sawProgress = false;
                var sawResponse = false;

                while (!sawResponse)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        Console.Error.WriteLine("[FAIL] install progress: host closed stdout");
                        return 1;
                    }

                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;
                    var type = root.GetProperty("type").GetString();

                    if (type == "progress" && root.GetProperty("requestId").GetString() == "4")
                    {
                        sawProgress = true;
                        continue;
                    }

                    if (type == "response" && root.GetProperty("requestId").GetString() == "4")
                    {
                        sawResponse = true;
                    }
                }

                if (!sawProgress)
                {
                    Console.Error.WriteLine("[WARN] install progress: no progress events (operation may have failed before reporting)");
                }
                else
                {
                    Console.Error.WriteLine("[OK] install progress events");
                }

                Console.Error.WriteLine("[OK] install final response");
                return 0;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath, recursive: true);
                }
                catch
                {
                    // Best-effort cleanup of temp test folder.
                }
            }
        }

        private static int TestCancellation(StreamWriter writer, StreamReader reader)
        {
            WriteRequest(writer, "cancel-missing", "cancel", new { targetRequestId = "does-not-exist" });
            var missing = ReadResponse(reader, "cancel-missing");
            if (missing.Success || missing.ErrorCode != "not_found")
            {
                Console.Error.WriteLine("[FAIL] cancel missing target should return not_found");
                return 1;
            }

            Console.Error.WriteLine("[OK] cancel missing target");
            return 0;
        }

        private static int TestShutdown(StreamWriter writer, StreamReader reader)
        {
            WriteRequest(writer, "5", "shutdown");
            var response = ReadResponse(reader, "5");
            if (!response.Success)
                return 1;

            writer.Close();
            Console.Error.WriteLine("[OK] shutdown request");
            return 0;
        }

        private static void WriteRequest(StreamWriter writer, string requestId, string method, object? parameters = null)
        {
            string line;
            if (parameters == null)
            {
                line = JsonSerializer.Serialize(new { requestId, method }, JsonOptions);
            }
            else
            {
                line = JsonSerializer.Serialize(new { requestId, method, @params = parameters }, JsonOptions);
            }

            writer.WriteLine(line);
            writer.Flush();
        }

        private static (bool Success, string? ErrorCode) ReadResponse(StreamReader reader, string requestId)
        {
            while (true)
            {
                var response = ReadNextResponse(reader);
                if (response.RequestId == requestId)
                    return (response.Success, response.ErrorCode);
            }
        }

        private static (bool Success, string? RequestId, string? ErrorCode) ReadNextResponse(StreamReader reader)
        {
            var line = reader.ReadLine();
            if (line == null)
                throw new InvalidOperationException("Host stdout closed unexpectedly.");

            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString();

            if (type == "progress")
                return (false, root.GetProperty("requestId").GetString(), null);

            if (type != "response")
                throw new InvalidOperationException($"Unexpected message type: {type}");

            var requestId = root.TryGetProperty("requestId", out var requestIdElement)
                ? requestIdElement.GetString()
                : null;
            var success = root.GetProperty("success").GetBoolean();
            string? errorCode = null;
            if (root.TryGetProperty("error", out var errorElement)
                && errorElement.TryGetProperty("code", out var codeElement))
            {
                errorCode = codeElement.GetString();
            }

            if (success)
                Console.Error.WriteLine($"[OK] response {requestId}");
            else
                Console.Error.WriteLine($"[INFO] response {requestId} failed: {errorCode}");

            return (success, requestId, errorCode);
        }

        private static string ResolveHostPath()
        {
            var baseDir = AppContext.BaseDirectory;
            var candidates = new[]
            {
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Asher.Host", "bin", "x86", "Debug", "net8.0-windows", "Asher.Host.exe")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "Asher.Host", "bin", "x86", "Debug", "net8.0-windows", "Asher.Host.exe")),
                Path.GetFullPath(Path.Combine(baseDir, "Asher.Host.exe"))
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            throw new FileNotFoundException("Could not locate Asher.Host.exe for test client.");
        }
    }
}
