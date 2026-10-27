# SMTP Relay Service - Troubleshooting Guide

## Issue: Mail accepted by MailboxFilter but not processed by MessageStore

### Diagnostic Steps

1. **Run the service in console mode with detailed logging**:
   ```powershell
   cd SMTPRelay
   dotnet run
   ```

2. **Expected startup log sequence**:
   ```
   Running in console mode - logging to console output
   SMTP Relay Service starting on port 25
   SmtpMessageStore initialized and ready to process messages
   MessageStore successfully resolved from service provider
   SmtpServer options configured. Creating server instance...
   Creating NoAuthenticationUserAuthenticator instance
   SmtpServer instance created successfully with event handlers attached
   Starting SMTP server...
   SMTP Relay Service started successfully on port 25
   ```

3. **Send a test email**:
   ```powershell
   .\TestSmtpRelay.ps1 -To "your-email@yourdomain.com"
   ```

4. **Expected email processing log sequence**:
   ```
   SMTP Session Created - Remote: 127.0.0.1:xxxxx, SessionId: xxx
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
   SaveAsync called - processing incoming email from 127.0.0.1:xxxxx
   Received email from test@example.com to your-email@yourdomain.com with subject: Test Email from SMTP Relay, format: Text, attachments: 0
   Email sent successfully via Graph API to your-email@yourdomain.com
   SMTP Session Completed - Remote: 127.0.0.1:xxxxx
   ```

### Common Issues and Solutions

#### Issue 1: SaveAsync is not called after DATA command

**Symptoms:**
- You see "SMTP Command Executed: DATA" but no "SaveAsync called" message
- The SMTP session completes without processing the message

**Possible Causes & Solutions:**

1. **MessageStore not properly registered**:
   - Check if you see "MessageStore successfully resolved from service provider" in the logs
   - If you see "MessageStore could not be resolved from service provider!", the DI registration is incorrect

2. **Exception in SaveAsync being swallowed**:
   - Check for any error logs after the DATA command
   - Look for "Failed to process and forward email" messages

3. **SmtpServer not using the MessageStore**:
   - Verify the MessageStore is registered as `MessageStore` (base class), not just `SmtpMessageStore`
   - The registration should be: `builder.Services.AddTransient<MessageStore, SmtpMessageStore>();`

#### Issue 2: Session faults after DATA command

**Symptoms:**
- You see "SMTP Session Faulted" messages
- Exception details in the logs

**Solutions:**
- Check the exception message for details
- Common causes:
  - Invalid email format
  - Memory issues with large attachments
  - Graph API authentication failures

#### Issue 3: MailboxFilter accepts but nothing happens

**Symptoms:**
- You see "Accepting sender" and "Accepting recipient" messages
- No further processing occurs
- No SaveAsync calls

**Solution:**
- This is the exact issue you're experiencing
- The SmtpServer library may not be configured correctly
- Verify all three components are registered:
  1. MessageStore
  2. MailboxFilter  
  3. UserAuthenticator

### Verification Checklist

Run these checks in order:

- [ ] Service starts without errors
- [ ] "MessageStore successfully resolved from service provider" appears in logs
- [ ] "SmtpMessageStore initialized and ready to process messages" appears
- [ ] Can connect via telnet to port 25
- [ ] EHLO command works
- [ ] MAIL FROM command works (returns 250 Ok)
- [ ] RCPT TO command works (returns 250 Ok)
- [ ] DATA command works (returns 354 Start mail input)
- [ ] Message terminator (.) is accepted (returns 250 Ok)
- [ ] "SaveAsync called" message appears after message terminator
- [ ] Email is forwarded to Graph API successfully

### Debug Output Files

When running in console mode, you can redirect output to a file for analysis:

```powershell
dotnet run > smtp_debug.log 2>&1
```

Then send a test email and check the log file for the complete sequence.

### Last Resort: Manual Telnet Test

If PowerShell tests don't work, test manually with telnet:

```
telnet localhost 25
EHLO test.local
MAIL FROM:<test@test.com>
RCPT TO:<your-email@yourdomain.com>
DATA
From: test@test.com
To: your-email@yourdomain.com
Subject: Manual Test

This is a manual test message.
.
QUIT
```

Watch the console output for each command and note where the process stops.

## Getting Help

If the issue persists, provide:
1. Complete console output from startup to failed email attempt
2. The exact point where logging stops
3. Any error messages or exceptions
4. The test command you're using
