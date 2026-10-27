# SMTP Relay Service

A Windows service that accepts SMTP connections and forwards the messages to Microsoft 365 through the Microsoft Graph `sendMail` API. Use it to let devices and applications that only speak SMTP (printers, scanners, line-of-business apps) send mail without SMTP AUTH or a connector.

## Features

- Receives email over SMTP on a configurable port (default: 25)
- Sends it through Microsoft Graph with an app registration (client credentials)
- Accepts mail only from allowed client IP addresses or ranges
- Sends only as a configured mailbox or an explicit list of allowed senders
- Supports HTML and plain-text bodies, Cc/Bcc headers and attachments
- Runs as a Windows service or from the console
- Writes received, forwarded and failed messages to the Windows Event Log

## Download

Download `SMTPRelay-<version>-win-x64.zip` from the [latest release](https://github.com/adee42/msgraph-smtp-relay/releases/latest). It contains a self-contained `SMTPRelay.exe` (no .NET runtime required) and a template `appsettings.json`.

## Prerequisites

1. **App registration in Microsoft Entra ID**
   1. Go to *Microsoft Entra admin center > App registrations > New registration*, name it (e.g. "SMTP Relay"), choose *Accounts in this organizational directory only* and register it.
   2. Note the **Application (client) ID** and **Directory (tenant) ID**.
   3. Under *Certificates & secrets*, create a client secret and copy its value.
   4. Under *API permissions*, add *Microsoft Graph > Application permissions > Mail.Send* and grant admin consent.
2. **A mailbox to send from** (for example a shared mailbox `noreply@yourdomain.com`).
3. **Restrict the app to that mailbox.** The `Mail.Send` application permission allows sending as *any* mailbox in the tenant. Limit it with [RBAC for Applications](https://learn.microsoft.com/exchange/permissions-exo/application-rbac) or an [application access policy](https://learn.microsoft.com/graph/auth-limit-mailbox-access), for example:
   ```powershell
   New-ApplicationAccessPolicy -AppId <client-id> -PolicyScopeGroupId relay-senders@yourdomain.com -AccessRight RestrictAccess -Description "SMTP relay"
   ```

## Configuration

Edit `appsettings.json` next to `SMTPRelay.exe`:

```json
{
  "SmtpServer": {
    "Port": 25,
    "MaxMessageSize": 10485760,
    "AllowedClients": [ "127.0.0.1", "::1", "192.168.1.0/24" ]
  },
  "GraphApi": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "SendFromAddress": "noreply@yourdomain.com",
    "AllowedSenders": [ "scanner@yourdomain.com" ]
  }
}
```

| Setting | Description |
| --- | --- |
| `SmtpServer:Port` | Port the SMTP server listens on (default: 25). |
| `SmtpServer:MaxMessageSize` | Maximum message size in bytes (default: 10 MB). |
| `SmtpServer:AllowedClients` | IP addresses or CIDR ranges that may send mail. Other clients are rejected at `MAIL FROM`. The template allows only the local machine. **An empty list allows every client.** |
| `GraphApi:TenantId` | Directory (tenant) ID. |
| `GraphApi:ClientId` | Application (client) ID. |
| `GraphApi:ClientSecret` | Client secret value. |
| `GraphApi:SendFromAddress` | Mailbox used as the sender. |
| `GraphApi:AllowedSenders` | Other mailboxes that may appear as the sender. If a message's `From` address is not `SendFromAddress` or in this list, the message is sent as `SendFromAddress`. |

Any setting can also be passed on the command line (`--SmtpServer:Port=2525`) or as an environment variable (`SmtpServer__Port=2525`).

## Installation as a Windows service

Run these from an elevated PowerShell prompt:

```powershell
sc.exe create "SMTPRelayService" binPath="C:\SMTPRelay\SMTPRelay.exe" start=auto
sc.exe start SMTPRelayService
```

To stop and remove the service:

```powershell
sc.exe stop SMTPRelayService
sc.exe delete SMTPRelayService
```

Allow the SMTP port in Windows Firewall only for the hosts that need it.

## Running from the console

Run `SMTPRelay.exe` (or `dotnet run` from the source folder). All log output goes to the console; press `Ctrl+C` to stop. Port 25 requires an elevated prompt; for testing, use another port:

```powershell
.\SMTPRelay.exe --SmtpServer:Port=2525
```

## Testing

Send a test message with the included script:

```powershell
.\TestSmtpRelay.ps1 -Port 2525 -From "noreply@yourdomain.com" -To "you@yourdomain.com"
```

A successful run logs:

```
Accepting sender: noreply@yourdomain.com from client 127.0.0.1
Accepting recipient: you@yourdomain.com from noreply@yourdomain.com
Received email from noreply@yourdomain.com to you@yourdomain.com with subject: Test Email from SMTP Relay, format: Text, attachments: 0
Email sent successfully via Graph API to you@yourdomain.com
```

## Logging

In console mode, logs are written to the console. As a service, logs go to the Windows Event Log (*Windows Logs > Application*, source *SMTP Relay Service*). Creating the event source requires administrator rights once; until then the service logs a warning and continues.

| Event ID | Meaning |
| --- | --- |
| 1000 | Service started |
| 1001 | Email received |
| 1002 | Email forwarded |
| 1003 | Email forwarding failed |
| 1004 | Service stopped |

## Security considerations

- The relay does not authenticate SMTP clients. Keep `AllowedClients` as narrow as possible and firewall the port.
- Restrict the app registration to the mailboxes it sends as (see [Prerequisites](#prerequisites)).
- Protect `appsettings.json`: it contains the client secret. Limit file access to administrators and the service account, and rotate the secret regularly.
- Recipients are taken from the message's `To`, `Cc` and `Bcc` headers.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Service does not start | Event Log and console output; the port may be in use (`netstat -ano \| findstr :25`) or an `AllowedClients` entry may be invalid. |
| `MAIL FROM` rejected | The client's IP is not in `SmtpServer:AllowedClients`; the log shows the address that was rejected. |
| Mail accepted but not delivered | Look for `Failed to send email via Graph API`. Check tenant ID, client ID and secret (including expiry), admin consent for `Mail.Send`, and that `SendFromAddress` exists and is allowed by your access policy. |
| Wrong sender | The `From` address was not in `AllowedSenders`, so `SendFromAddress` was used. |

## Building from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet build
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Releasing

Pushing a tag that starts with `v` (for example `v1.0.2`) runs the release workflow, which builds the single-file executable and publishes a GitHub release with the zip attached.

## License

[MIT](LICENSE)
