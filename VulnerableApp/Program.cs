using System;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Net.Http;
using System.Net;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq; // old Newtonsoft

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Quick: ensure DB file exists (SQLite) and create a vulnerable table
var dbFile = builder.Configuration.GetConnectionString("Default")?.Replace("Data Source=", "") ?? "vulnerable.db";
if (!File.Exists(dbFile))
{
    // SqliteConnection.CreateFile("yourdb.sqlite"); // <-- Remove or comment out this line
    using var conn = new SqliteConnection($"Data Source={dbFile}");
    conn.Open();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "CREATE TABLE users (id INTEGER PRIMARY KEY AUTOINCREMENT, username TEXT, password TEXT)";
    cmd.ExecuteNonQuery();
    // add a sample user
    cmd.CommandText = "INSERT INTO users (username,password) VALUES ('admin','password123)";
    cmd.ExecuteNonQuery();
}

// 1) Hardcoded credentials and secret: /login
app.MapPost("/login", (HttpRequest request) =>
{
    // BAD: hardcoded credentials + reading API key from appsettings in cleartext
    var fixedUser = "admin";
    var fixedPass = "password123";

    var form = request.Form;
    var user = form["username"].ToString();
    var pass = form["password"].ToString();

    var configuredStripe = builder.Configuration["ApiKeys:StripeApiKey"]; 

    if (user == fixedUser && pass == fixedPass)
    {
        return Results.Ok(new { message = "Logged in (insecure demo)", apiKey = configuredStripe });
    }
    return Results.Unauthorized();
});

// 2) SQL Injection: /search?username=...
app.MapGet("/search", (string username) =>
{
    // FIX: Use parameterized query to prevent SQL injection
    using var conn = new SqliteConnection($"Data Source={dbFile}");
    conn.Open();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT id, username FROM users WHERE username = @username";
    cmd.Parameters.AddWithValue("@username", username);
    using var reader = cmd.ExecuteReader();
    var results = new System.Collections.Generic.List<object>();
    while (reader.Read())
    {
        results.Add(new { id = reader.GetInt32(0), username = reader.GetString(1) });
    }
    return Results.Ok(results);
});

// 3) Command injection / OS command execution endpoint: /exec?cmd=...
app.MapGet("/exec", (string cmd) =>
{
    // BAD: directly runs system shell + user input
    string shell, args;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        shell = "cmd.exe";
        args = "/c " + cmd;
    }
    else
    {
        shell = "/bin/sh";
        args = "-c \"" + cmd + "\"";
    }

    var psi = new ProcessStartInfo(shell, args)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    try
    {
        using var p = Process.Start(psi);
        var outText = p?.StandardOutput.ReadToEnd();
        p?.WaitForExit();
        return Results.Ok(new { stdout = outText });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// 4) Insecure deserialization endpoint: /deserialize  (body=base64 of serialized object)
app.MapPost("/deserialize", async (HttpRequest request) =>
{
    // SAFE: replaced BinaryFormatter with JSON for deserialization
    using var sr = new StreamReader(request.Body);
    var body = await sr.ReadToEndAsync();
    try
    {
        var obj = JObject.Parse(body); // Assuming we want a JSON object
        return Results.Ok(new { type = obj?.GetType().FullName ?? "null" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// 5) Weak crypto usage: /hash?input=...
app.MapGet("/hash", (string input) =>
{
    // BAD: MD5 for hashing (weak)
    using var md5 = MD5.Create();
    var bytes = Encoding.UTF8.GetBytes(input);
    var hash = md5.ComputeHash(bytes);
    return Results.Ok(Convert.ToHexString(hash));
});

// 6) Insecure TLS bypass when calling an external URL: /fetch?url=...
app.MapGet("/fetch", async (string url) =>
{
    // BAD: disables certificate validation
    var handler = new HttpClientHandler();
    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true; // insecure: always accept
    using var client = new HttpClient(handler);
    try
    {
        var text = await client.GetStringAsync(url);
        return Results.Ok(new { length = text.Length });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// 7) Example of using old Newtonsoft for parsing user JSON: /parse
app.MapPost("/parse", async (HttpRequest request) =>
{
    using var sr = new StreamReader(request.Body);
    var body = await sr.ReadToEndAsync();
    // BAD: using old Newtonsoft and loading JSON without schema validation
    var jo = JObject.Parse(body);
    return Results.Ok(new { parsed = jo["name"]?.ToString() ?? "<none>" });
});

app.Run();

public partial class Program { }
