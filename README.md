# SMTP Relay Service

A Windows Service that accepts SMTP connections and forwards emails to Office 365 via Microsoft Graph API.

## Features

- Receives emails via SMTP on a configurable port (default: 25)
- Forwards emails to Office 365 using Microsoft Graph API
- Runs as a Windows Service
- Supports HTML and plain text emails
- Supports email attachments
- Configurable message size limits
- **Windows Event Log integration** - Logs all incoming and outgoing emails with detailed information

## Prerequisites

1. **Azure AD App Registration**:
   - Register an application in Azure AD
   - Grant API permissions: `Mail.Send` (Application permission)
   - Create a client secret
   - Grant admin consent for the permissions

2. **Office 365 Configuration**:
   - The sending mailbox must exist in your Office 365 tenant
   - The app registration must have permission to send email on behalf of users

## Configuration

Edit `appsettings.json` to configure the service:

```json
{
  "SmtpServer": {
    "Port": 25,
    "MaxMessageSize": 10485760
  },
  "GraphApi": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "SendFromAddress": "noreply@yourdomain.com"
  }
}
```

### Configuration Options

- **SmtpServer.Port**: The port the SMTP server listens on (default: 25)
- **SmtpServer.MaxMessageSize**: Maximum message size in bytes (default: 10MB)
- **GraphApi.TenantId**: Your Azure AD tenant ID
- **GraphApi.ClientId**: The application (client) ID from Azure AD
- **GraphApi.ClientSecret**: The client secret from Azure AD
- **GraphApi.SendFromAddress**: The email address to send from (must exist in Office 365)

## Installation as Windows Service

1. **Build the application**:
   ```powershell
   dotnet publish -c Release
   ```

2. **Install as Windows Service**:
   ```powershell
   sc.exe create "SMTPRelayService" binPath="C:\path\to\SMTPRelay.exe"
   ```

3. **Start the service**:
   ```powershell
   sc.exe start SMTPRelayService
   ```

4. **Stop the service**:
   ```powershell
   sc.exe stop SMTPRelayService
   ```

5. **Uninstall the service**:
   ```powershell
   sc.exe delete SMTPRelayService
   ```

## Azure AD App Registration Setup

1. Navigate to Azure Portal > Azure Active Directory > App registrations
2. Click "New registration"
3. Enter a name (e.g., "SMTP Relay Service")
4. Select "Accounts in this organizational directory only"
5. Click "Register"
6. Note the **Application (client) ID** and **Directory (tenant) ID**
7. Go to "Certificates & secrets" > "New client secret"
8. Create a secret and copy its value
9. Go to "API permissions" > "Add a permission"
10. Select "Microsoft Graph" > "Application permissions"
11. Add **Mail.Send** permission
12. Click "Grant admin consent"

## Testing

### Running from Command Line (Development/Testing)

For development and testing, you can run the application directly from the command line. When running in this mode, all logging messages will be displayed in the console:

```powershell
# Navigate to the project directory
cd SMTPRelay

# Run the application
dotnet run

# Or run the published executable
.\bin\Release\net9.0\SMTPRelay.exe
```

When running from the command line, you'll see real-time logging output including:
- Service startup messages
- Incoming email details (from, to, subject, size, attachments)
- Outgoing email status (success/failure)
- Any errors or warnings

Press `Ctrl+C` to stop the service when running in console mode.

### Testing Email Relay

You can test the SMTP relay by sending an email through it:

```powershell
# Using PowerShell
$smtp = New-Object Net.Mail.SmtpClient("localhost", 25)
$msg = New-Object Net.Mail.MailMessage("sender@test.com", "recipient@yourdomain.com", "Test Subject", "Test Body")
$smtp.Send($msg)
```

Or using telnet:
```
telnet localhost 25
EHLO localhost
MAIL FROM: <sender@test.com>
RCPT TO: <recipient@yourdomain.com>
DATA
Subject: Test Email

This is a test email.
.
QUIT
```

## Logging

### Console Logging (Command Line Mode)
When running from the command line, all log messages are written to the console output in real-time. This is useful for:
- Development and debugging
- Testing configuration changes
- Troubleshooting issues before deploying as a service

### Windows Event Log (Service Mode)
When running as a Windows Service, logs are written to the Windows Event Log and can be viewed using Event Viewer under:
- Windows Logs > Application
- Source: SMTP Relay Service

Event IDs:
- **1000**: Service Startup
- **1001**: Incoming Email Received
- **1002**: Email Forwarded Successfully
- **1003**: Email Forwarding Failed
- **1004**: Service Shutdown

## Security Considerations

- Store the client secret securely (consider using Azure Key Vault or User Secrets for development)
- Restrict network access to the SMTP port to trusted sources
- Run the service with minimal required permissions
- Regularly rotate the client secret
- Monitor the service logs for suspicious activity

## Troubleshooting

### "Unrecognized command" Error

If you receive an "Unrecognized command" error when testing with telnet or SMTP client:

1. **Verify the service is running**:
   ```powershell
   # Run in console mode to see detailed logs
   dotnet run
   ```

2. **Check for these log messages** in order:
   - "SMTP Relay Service starting on port 25"
   - "SmtpServer options configured. Creating server instance..."
   - "SmtpMessageStore initialized and ready to process messages"
   - "SmtpServer instance created successfully"
   - "Starting SMTP server..."
   - "SMTP Relay Service started successfully on port 25"

3. **Test with the provided PowerShell script**:
   ```powershell
   .\TestSmtpRelay.ps1 -From "test@test.com" -To "recipient@yourdomain.com"
   ```

4. **Manual telnet test**:
   ```
   telnet localhost 25
   EHLO test.local
   MAIL FROM: <test@example.com>
   RCPT TO: <recipient@yourdomain.com>
   DATA
   Subject: Test

   Test body
   .
   QUIT
   ```

### Expected Console Output When Email is Received

When an email is successfully received and processed, you should see:
```
SaveAsync called - processing incoming email from [endpoint]
Accepting sender: test@example.com
Accepting recipient: recipient@yourdomain.com from test@example.com
Received email from test@example.com to recipient@yourdomain.com with subject: Test, format: Text, attachments: 0
Email sent successfully via Graph API to recipient@yourdomain.com
```

### Service won't start
- Check Windows Event Log for error messages
- Verify configuration settings in appsettings.json
- Ensure the port is not already in use (especially port 25 may require admin privileges)
- Verify firewall rules allow traffic on the SMTP port
- Check if another SMTP service is using the port: `netstat -ano | findstr :25`

### Emails not being forwarded
- Check Azure AD app permissions are granted
- Verify the sending mailbox exists in Office 365
- Check the client secret is valid and not expired
- Review service logs for error messages
- Verify network connectivity to Microsoft Graph API

### Authentication errors
- Verify TenantId, ClientId, and ClientSecret are correct
- Ensure admin consent has been granted for API permissions
- Check if the client secret has expired

### Port 25 Access Denied
Port 25 requires administrator privileges on Windows:
- Run Visual Studio or PowerShell as Administrator
- Or use a different port (e.g., 2525) and update appsettings.json
