using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace ipk_25_chat.cliArgParser;

public class CliArgParser
{
    private string _protocol = string.Empty;
    private string _server = string.Empty;
    private int _port;
    private int _timeout;
    private int _maxRetries;

    public void ParseCliArgs(string[] args)
    {
        var rootCommand = new RootCommand("Client for a chat server using IPK-25-CHAT protocol.")
        {
            new Option<string>(
                "-t",
                description: "Transport protocol used for connection (TCP or UDP).")
            {
                IsRequired = true
            },
            new Option<string>(
                "-s",
                description: "Server IP address or hostname.")            
            {
                IsRequired = true
            },
            new Option<int>(
                "-p",
                () => 4567,
                description: "Server port."),
            new Option<int>(
                "-d",
                () => 250,
                description: "UDP confirmation timeout in milliseconds."),
            new Option<int>(
                "-r",
                () => 3,
                description: "Maximum number of UDP retransmissions.")
        };
    
        rootCommand.Handler = CommandHandler.Create<string, string, int, int, int>((t, s, p, d, r) =>
        {
            _protocol = t;
            _server = s;
            _port = p;
            _timeout = d;
            _maxRetries = r;
        });
        
        rootCommand.Invoke(args);

    }
    
    public void PrintArgs()
    {
        Console.WriteLine($"Protocol: {_protocol}");
        Console.WriteLine($"Server: {_server}");
        Console.WriteLine($"Port: {_port}");
        Console.WriteLine($"Timeout: {_timeout}");
        Console.WriteLine($"Max Retries: {_maxRetries}");
    }
}