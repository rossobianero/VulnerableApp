# VulnerableApp (for scanner testing only)

This small .NET 6 Web API intentionally contains multiple insecure patterns and older package versions so you can test static/dynamic and dependency scanners (Semgrep, osv-scanner, Snyk, etc.)

**DO NOT** deploy this to production. Use in an isolated VM/docker container.

## Features (intentionally insecure)
- `appsettings.json` includes a plaintext API key.
- `/login` uses hardcoded credentials.
- `/search` builds SQL with string concatenation (SQL injection).
- `/exec` executes system commands from user input (command injection).
- `/deserialize` uses `BinaryFormatter` to deserialize untrusted data (insecure deserialization).
- `/hash` uses MD5 (weak hash).
- `/fetch` uses an `HttpClientHandler` that accepts any TLS certificate (insecure HTTPS validation).
- `/parse` uses old Newtonsoft JSON parsing (no validation).
- `VulnerableApp.csproj` references older package versions (`Newtonsoft.Json` v9.0.1, `Microsoft.Data.Sqlite` v2.0.0).

## How to build & run
```bash
# from the directory containing VulnerableApp.csproj
dotnet restore
dotnet build
dotnet run --project VulnerableApp
```

The app binds to the default Kestrel ports. You can also use the included Dockerfile to run in a container.

## Demonstration scripts
See `demo/` for simple curl scripts that exercise each insecure endpoint. These are not destructive but demonstrate the insecure behavior (SQL injection, command execution, etc.). Run them locally only.
