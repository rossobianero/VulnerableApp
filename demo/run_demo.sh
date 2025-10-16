#!/usr/bin/env bash
set -euo pipefail
BASE_URL=${BASE_URL:-http://localhost:5000}

echo "1) Test /hash (MD5)"
curl -s "${BASE_URL}/hash?input=hello" | jq .

echo
echo "2) Test /search (SQL injection demo). Normal search for 'admin':"
curl -s "${BASE_URL}/search?username=admin" | jq .

echo
echo "SQL injection attempt (username=admin' OR '1'='1):"
curl -s "${BASE_URL}/search?username=admin' OR '1'='1" | jq .

echo
echo "3) Test /exec (command execution). Will run a harmless command:"
if [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
  echo "Skipping exec on Windows in demo script."
else
  curl -s "${BASE_URL}/exec?cmd=echo%20demo_exec" | jq .
fi

echo
echo "4) Test /parse (old Newtonsoft JSON parsing):"
curl -s -X POST -d '{"name":"demo"}' -H 'Content-Type: application/json' "${BASE_URL}/parse" | jq .

echo
echo "5) Test /fetch (insecure TLS bypass) with https://self-signed.badssl.com/"
curl -s "${BASE_URL}/fetch?url=https://self-signed.badssl.com/" | jq .
