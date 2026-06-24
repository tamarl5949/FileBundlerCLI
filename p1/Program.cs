using System.CommandLine;

var bundleOption = new Option<FileInfo>("--output","file path and name");
var languageOption = new Option<string>(new[] { "--language", "-l" }, "List of programming languages or 'all'");
languageOption.IsRequired = true;
var noteOption = new Option<bool>(new[] { "--note", "-n" }, "Whether to include the source file path as a comment");
var sortOption = new Option<string>(new[] { "--sort", "-s" }, () => "name", "Sort files by 'name' (alphabetical) or 'type' (file extension)");
var removeEmptyLinesOption = new Option<bool>(new[] { "--remove-empty-lines", "-r" }, "Whether to remove empty lines from the source code");
var authorOption = new Option<string>(new[] { "--author", "-a" }, "Name of the file creator");

var bundleCommand = new Command("bundle", "Bundle code file to single file");
bundleCommand.AddOption(bundleOption);
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);

bundleCommand.SetHandler((output,language, note, sort, removeEmptyLines, author) =>
{
    try
    {
        // א. יצירת הקובץ
        File.Create(output.FullName).Close();
        Console.WriteLine("File was created");
        if (!string.IsNullOrEmpty(author))
        {
            File.AppendAllText(output.FullName, $"// Author: {author}" + Environment.NewLine);
        }

        // ב. איסוף וסינון ראשוני - תיקיות מערכת
        var currentPath = Directory.GetCurrentDirectory();
        var allFiles = Directory.GetFiles(currentPath, "*.*", SearchOption.AllDirectories);
        var filteredFiles = allFiles.Where(file =>
            !file.Contains("bin") &&
            !file.Contains("debug") &&
            !file.Contains("obj") &&
            !file.Contains("publish") &&
            !file.EndsWith(".rsp") &&
            Path.GetFullPath(file) != output.FullName).ToList();

        // ג. סינון לפי שפה
        var filesByLanguage = filteredFiles.Where(file =>
        {
            if (language == "all") return true;

            var extension = Path.GetExtension(file).TrimStart('.');
            return language.Contains(extension);
        }).ToList();

        // ד. מיון הקבצים
        if (sort == "type")
            filesByLanguage = filesByLanguage.OrderBy(f => Path.GetExtension(f)).ThenBy(f => Path.GetFileName(f)).ToList();
        else
            filesByLanguage = filesByLanguage.OrderBy(f => Path.GetFileName(f)).ToList();

        // ה. כתיבה לקובץ
        foreach (var file in filesByLanguage)
        {
            if (note)
            {
                File.AppendAllText(output.FullName, $"// Source: {Path.GetFileName(file)}, Path: {file}" + Environment.NewLine);
            }
            var lines = File.ReadAllLines(file);
            if (removeEmptyLines)
            {
                lines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                File.AppendAllText(output.FullName, string.Join(Environment.NewLine, lines) + Environment.NewLine);
            }
            else
            {
                string content = File.ReadAllText(file);
                File.AppendAllText(output.FullName, content + Environment.NewLine);
            }
        }
    }
    catch ( DirectoryNotFoundException ex) {
        Console.WriteLine("Error! file path is invalid.");
    }
}, bundleOption, languageOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

var createRspCommand = new Command("create-rsp", "Create a response file for the bundle command");

createRspCommand.SetHandler(() =>
{
    var command = "bundle ";

    // 1: שם נתיב
    Console.WriteLine("Enter value for --output (file path and name):");
    var output = Console.ReadLine();
    while (string.IsNullOrWhiteSpace(output))
    {
        Console.WriteLine("Output is required. Please enter a value:");
        output = Console.ReadLine();
    }
    command += $"--output \"{output}\" ";

    // 2: שפות
    Console.WriteLine("Enter value for --language (e.g. 'cs, py' or 'all'):");
    var lang = Console.ReadLine();
    while (string.IsNullOrWhiteSpace(lang))
    {
        Console.WriteLine("Language is required. Please enter a value:");
        lang = Console.ReadLine();
    }
    command += $"--language {lang} ";

    // 3: המקור
    Console.WriteLine("Include source path as a comment? (y/n):");
    if (Console.ReadLine()?.ToLower() == "y") command += "--note ";

    //  4: מיון
    Console.WriteLine("Sort by 'name' or 'type'? (default is name):");
    var sortVal = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(sortVal)) command += $"--sort {sortVal} ";

    //  5: הסרת שורות ריקות
    Console.WriteLine("Remove empty lines? (y/n):");
    if (Console.ReadLine()?.ToLower() == "y") command += "--remove-empty-lines ";

    //  6: שם יוצר
    Console.WriteLine("Enter author name (optional):");
    var auth = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(auth)) command += $"--author \"{auth}\" ";

    try
    {
        var rspFileName = "output.rsp";
        File.WriteAllText(rspFileName, command);
        Console.WriteLine($"Response file created successfully: {rspFileName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error saving response file: " + ex.Message);
    }
});

var rootCommand = new RootCommand("Root command for file bundler CLI");
rootCommand.Add(bundleCommand);
rootCommand.Add(createRspCommand);
await rootCommand.InvokeAsync(args);