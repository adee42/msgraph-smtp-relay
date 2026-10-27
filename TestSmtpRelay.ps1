# SMTP Relay Test Script
# This script tests the SMTP relay by sending a test email

param(
    [string]$SmtpServer = "localhost",
    [int]$Port = 25,
    [string]$From = "test@example.com",
    [string]$To = "recipient@yourdomain.com",
    [string]$Subject = "Test Email from SMTP Relay",
    [string]$Body = "This is a test email sent through the SMTP Relay Service."
)

Write-Host "Testing SMTP Relay Service..." -ForegroundColor Cyan
Write-Host "Server: $SmtpServer"
Write-Host "Port: $Port"
Write-Host "From: $From"
Write-Host "To: $To"
Write-Host ""

try {
    # Create SMTP client
    $smtp = New-Object Net.Mail.SmtpClient($SmtpServer, $Port)
    $smtp.EnableSsl = $false
    $smtp.Timeout = 30000
    
    # Create email message
    $msg = New-Object Net.Mail.MailMessage($From, $To, $Subject, $Body)
    
    Write-Host "Sending email..." -ForegroundColor Yellow
    
    # Send email
    $smtp.Send($msg)
    
    Write-Host "✓ Email sent successfully!" -ForegroundColor Green
    Write-Host "Check the SMTP Relay Service logs to verify forwarding to Office 365" -ForegroundColor Cyan
}
catch {
    Write-Host "✗ Failed to send email:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
    Write-Host "1. Ensure the SMTP Relay Service is running (check console output)"
    Write-Host "2. Verify the service is listening on port $Port"
    Write-Host "3. Check firewall settings"
    Write-Host "4. Review service logs for errors"
}
finally {
    if ($smtp) { $smtp.Dispose() }
    if ($msg) { $msg.Dispose() }
}
