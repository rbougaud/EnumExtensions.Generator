using EnumExtensions.Tool.Helpers;

var root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();

Console.WriteLine($"Scanning: {root}");

bool modified = await EnumScanner.RunAsync(root);

if (modified)
{
    Console.WriteLine("Files updated.");
    Environment.Exit(1); // utile en CI
}
Console.WriteLine("Done.");
