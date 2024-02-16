
using Chatting.Client;

List<string> reserved = ["system", "server", "default"];

string? name = string.Empty;
bool allowed = false;
do
{
    Console.Write("Choose your display name: ");
    name = Console.ReadLine();

    if (reserved.Contains(name))
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"{name} is not allowed. | ");
        Console.ForegroundColor = ConsoleColor.White;
    }
    else
    {
        allowed = true;
    }
}
while (!allowed);

Client client = new Client(name);
client.Startup();

