# SMTP Relay - Testing After MessageStoreFactory Fix

## Critical Fix Applied

Added `MessageStoreFactory` to explicitly provide the `SmtpMessageStore` to the SMTP server. This ensures the server properly invokes the message store for each incoming email.

## Testing Steps

### 1. Clean and Rebuild
```powershell
cd "C:\Users\User\Documents\Visual Studio 2019\Projects\Tools\SMTPRelay"
dotnet clean
dotnet build
```

### 2. Run in Console Mode
```powershell
dotnet run
```

### 3. Expected Startup Logs

You should now see these messages in order:
```
Running in console mode - logging to console output
SMTP Relay Service starting on port 25
SmtpMessageStore initialized and ready to process messages          ← MessageStore constructor
✓ SmtpMessageStore successfully resolved from service provider     ← DI verification
✓ IMessageStoreFactory successfully resolved from service provider ← Factory verification
SmtpServer options configured. Creating server instance...
Creating NoAuthenticationUserAuthenticator instance                 ← Auth setup
SmtpServer instance created successfully with event handlers attached
Starting SMTP server...
SMTP Relay Service started successfully on port 25
```

**Important**: If you see both ✓ checkmarks, the services are properly registered.

### 4. Send Test Email

In a new PowerShell window:
```powershell
cd "C:\Users\User\Documents\Visual Studio 2019\Projects\Tools\SMTPRelay"
.\TestSmtpRelay.ps1 -To "your-real-email@yourdomain.com"
```

### 5. Expected Email Processing Logs

**THIS IS THE CRITICAL SEQUENCE** - you should now see:
```
SMTP Session Created - Remote: 127.0.0.1:xxxxx
SMTP Command Executing: EHLO
SMTP Command Executed: EHLO
SMTP Command Executing: MAIL
SMTP Command Executed: MAIL
Accepting sender: test@example.com
SMTP Command Executing: RCPT
SMTP Command Executed: RCPT
Accepting recipient: your-email@yourdomain.com from test@example.com
SMTP Command Executing: DATA
SMTP Command Executed: DATA
MessageStoreFactory.CreateInstance called - creating SmtpMessageStore    ← NEW: Factory is called
SmtpMessageStore instance created successfully via factory               ← NEW: Store created
SaveAsync called - processing incoming email from 127.0.0.1:xxxxx      ← NEW: SaveAsync invoked!
Received email from test@example.com to your-email@yourdomain.com...
Email sent successfully via Graph API to your-email@yourdomain.com
SMTP Session Completed - Remote: 127.0.0.1:xxxxx
```

## What Changed?

### Before:
- SmtpServer was trying to resolve `MessageStore` (base class) from DI
- The server would complete the SMTP transaction without invoking SaveAsync
- Email accepted but never forwarded

### After:
- Added `MessageStoreFactory` that implements `IMessageStoreFactory`
- Factory explicitly creates `SmtpMessageStore` instances for each session
- SmtpServer now properly calls the factory to get a message store
- Factory logs when it's called, confirming the flow

## If It Still Doesn't Work

### Check 1: Factory is being called?
Look for: `"MessageStoreFactory.CreateInstance called"`
- **If you see this**: Factory works, issue is in SmtpMessageStore.SaveAsync
- **If you don't see this**: SmtpServer isn't using the factory

### Check 2: SaveAsync is being called?
Look for: `"SaveAsync called - processing incoming email"`
- **If you see this**: Message processing works! Check Graph API credentials
- **If you don't see this**: Exception in SaveAsync or issue with buffer

### Check 3: Any exceptions?
Look for any ERROR or exception messages between:
- `"MessageStoreFactory.CreateInstance called"` and `"SaveAsync called"`

## Troubleshooting

### If factory is not called:
1. Verify both ✓ checkmarks appear at startup
2. Check if `IMessageStoreFactory` registration is correct
3. Try restarting Visual Studio and rebuilding

### If SaveAsync fails:
1. Check for exceptions in the logs
2. Verify GraphApiSettings in appsettings.json
3. Test Graph API credentials separately

### If email forwarding fails:
1. Verify Azure AD app permissions (Mail.Send)
2. Check if SendFromAddress exists in Office 365
3. Verify client secret hasn't expired
4. Check network connectivity to graph.microsoft.com

## Success Criteria

✅ Startup shows both ✓ checkmarks  
✅ `MessageStoreFactory.CreateInstance called` appears when email sent  
✅ `SaveAsync called` appears  
✅ `Email sent successfully via Graph API` appears  
✅ Email received in Office 365 inbox  

## Next Steps After Success

1. Update appsettings.json with real Azure AD credentials
2. Test with various email clients (Outlook, Thunderbird, etc.)
3. Test with attachments
4. Test with HTML emails
5. Install as Windows Service for production use
