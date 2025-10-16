VulnerableApp package (zip) - contents:
- VulnerableApp/: the .NET 6 project (intentionally insecure)
- Dockerfile + docker-compose.yml : run in a container
- semgrep-rules/: example Semgrep rules to detect the patterns in the project
- demo/: scripts that exercise endpoints (non-destructive demos)

IMPORTANT SAFETY NOTICE:
This repository intentionally contains insecure code and old dependencies. Only run it in an isolated environment (local VM/container) and do NOT expose it to the public internet. The demo scripts call endpoints that execute system commands and run SQL queries — these are intended for local testing only.
