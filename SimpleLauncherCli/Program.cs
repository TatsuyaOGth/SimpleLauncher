using System;

int BaseExitCode = 100000;
int QuitExitCode = 200000;

var launcher = new SimpleLauncher.SimpleLauncher();

string filepath = launcher.DefaultFilePath;

// If parameter file is not exists, create template.
if (!launcher.IsFileExists())
{
    Console.WriteLine($"{filepath} is not exist. Create new template file");
    launcher.SaveAsTemplate();
    return 0;
}

try
{
    launcher.LoadParameters();
}
catch (Exception ex)
{
    Console.WriteLine("Load error!");
    Console.WriteLine(ex.GetType());
    Console.WriteLine(ex.Message);
    Console.WriteLine("Any key to quit");
    Console.Read();
    return 1;
}

bool isContinue = true;
do
{
    int exitCode = -1;
    Exception? processException = null;
    try
    {
        exitCode = launcher.ProcessStart(() =>
        {
            var proc = launcher.Process;
            if (proc == null) return;

            // Display current process statistics.
            Console.Clear();
            Console.WriteLine($"{proc}, ID:{proc.Id}");
            Console.WriteLine("-------------------------------------");

            Console.WriteLine($"  Physical memory usage     : {proc.WorkingSet64}");
            Console.WriteLine($"  Base priority             : {proc.BasePriority}");
            Console.WriteLine($"  User processor time       : {proc.UserProcessorTime}");
            Console.WriteLine($"  Privileged processor time : {proc.PrivilegedProcessorTime}");
            Console.WriteLine($"  Total processor time      : {proc.TotalProcessorTime}");
            Console.WriteLine($"  Paged system memory size  : {proc.PagedSystemMemorySize64}");
            Console.WriteLine($"  Paged memory size         : {proc.PagedMemorySize64}");

            if (proc.Responding)
            {
                Console.WriteLine("Status = Running");
            }
            else
            {
                Console.WriteLine("Status = Not Responding");
            }
        });
    }
    catch (Exception ex)
    {
        processException = ex;
    }

    // Set next launch target by exit code
    if (exitCode >= BaseExitCode)
    {
        if (exitCode == QuitExitCode)
        {
            break;
        }

        int idx = exitCode - BaseExitCode;
        if (idx < launcher.ApplicationCount)
        {
            launcher.SetLaunchTarget(idx);
            launcher.SaveParameters();
        }
    }
    else
    {
        // Set next launch target manually
        while (true)
        {
            Console.Clear();

            if (processException != null)
            {
                Console.WriteLine($"[E] {processException.GetType}: {processException.Message}");
                Console.WriteLine();
            }

            Console.WriteLine("Application Select");
            var appPaths = launcher.GetApplicationPaths();
            for (int i = 0; i < launcher.ApplicationCount; ++i)
            {
                Console.WriteLine($"{i}: {Path.GetFileName(appPaths[i])}");
            }

            Console.Write(">> ");
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape)
            {
                isContinue = false;
                break;
            }

            int keyInt = (int)Char.GetNumericValue(key.KeyChar);
            if (keyInt < 0)
            {
                continue;
            }

            Console.WriteLine(keyInt);

            if (keyInt < 0 || keyInt >= launcher.ApplicationCount)
            {
                continue;
            }

            launcher.SetLaunchTarget(keyInt);
            launcher.SaveParameters();
            break;
        }
    }
}
while (isContinue);

return 0;