using SMTPRelay;
using SMTPRelay.Models;
using SMTPRelay.Services;
using SmtpServer.Storage;
using SmtpServer.Authentication;
using SmtpServer.Mail;

var builder = Host.CreateApplicationBuilder(args);

// Detect if running as Windows Service or Console application
var isService = !Environment.UserInteractive;

// Configure logging based on execution mode
if (!isService)
{
    // Running from command line - add console logging with verbose output
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.SetMinimumLevel(LogLevel.Information);

    Console.WriteLine("Running in console mode - logging to console output");
}

builder.Services.Configure<SmtpServerSettings>(builder.Configuration.GetSection("SmtpServer"));
builder.Services.Configure<GraphApiSettings>(builder.Configuration.GetSection("GraphApi"));

builder.Services.AddSingleton(sp =>
{
    var config = builder.Configuration.GetSection("SmtpServer").Get<SmtpServerSettings>() 
        ?? new SmtpServerSettings();
    return config;
});

builder.Services.AddSingleton(sp =>
{
    var config = builder.Configuration.GetSection("GraphApi").Get<GraphApiSettings>() 
        ?? throw new InvalidOperationException("GraphApi configuration is missing");
    return config;
});

builder.Services.AddSingleton<GraphEmailService>();
builder.Services.AddSingleton<EventLogService>();
builder.Services.AddSingleton<SessionEventHandler>();
// Register SmtpMessageStore and factory
builder.Services.AddTransient<SmtpMessageStore>();
builder.Services.AddSingleton<MessageStoreFactory>();
builder.Services.AddSingleton<IMessageStoreFactory>(sp => sp.GetRequiredService<MessageStoreFactory>());
// Register authenticator to allow connections without authentication
builder.Services.AddSingleton<NoAuthenticationUserAuthenticator>();
builder.Services.AddSingleton<IUserAuthenticator>(sp => sp.GetRequiredService<NoAuthenticationUserAuthenticator>());
builder.Services.AddSingleton<IUserAuthenticatorFactory>(sp => sp.GetRequiredService<NoAuthenticationUserAuthenticator>());
// Register mailbox filter to accept all recipients
builder.Services.AddSingleton<AcceptAllMailboxFilter>();
builder.Services.AddSingleton<IMailboxFilter>(sp => sp.GetRequiredService<AcceptAllMailboxFilter>());
builder.Services.AddSingleton<IMailboxFilterFactory>(sp => sp.GetRequiredService<AcceptAllMailboxFilter>());

builder.Services.AddHostedService<Worker>();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "SMTP Relay Service";
});

var host = builder.Build();
host.Run();
