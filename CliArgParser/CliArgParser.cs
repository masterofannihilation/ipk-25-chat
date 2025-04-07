using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace ipk_25_chat.cliArgParser;

public class CliArgParser
{
    public string Protocol = string.Empty;
    public string Server = string.Empty;
    public int Port;
    public int Timeout;
    public int MaxRetries;

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
            Protocol = t;
            Server = s;
            Port = p;
            Timeout = d;
            MaxRetries = r;
        });
        
        rootCommand.Invoke(args);

    }
    
    public void PrintArgs()
    {
        Console.WriteLine($"Protocol: {Protocol}");
        Console.WriteLine($"Server: {Server}");
        Console.WriteLine($"Port: {Port}");
        Console.WriteLine($"Timeout: {Timeout}");
        Console.WriteLine($"Max Retries: {MaxRetries}");
    }
}