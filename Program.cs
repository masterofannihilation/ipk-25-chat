namespace ipk_25_chat;
class Program
{
    static int Main(string[] args)
    {
        var argParser = new cliArgParser.CliArgParser();
        argParser.ParseCliArgs(args);
        argParser.PrintArgs();
        
        return 0;
    }
}