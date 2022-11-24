using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;

namespace SimpleLauncher;

public class SimpleLauncher : IDisposable
{
    public readonly string DefaultFileName = "Parameters.json";

    public string DefaultFilePath
        => Path.Join(AppDomain.CurrentDomain.BaseDirectory, DefaultFileName);

    public Parameter? Parameter => _parameter;
    public Process? Process => _process;

    public int ApplicationCount
    {
        get => (_parameter == null || _parameter.ApplicationPaths == null)
            ? -1
            : _parameter.ApplicationPaths.Length;
    }

    public int LaunchTarget
    {
        get => _parameter == null
            ? -1
            : _parameter.LaunchTarget;
    }

    private Parameter? _parameter;
    private Process? _process;



    public void SaveAsTemplate(string? filepath = null)
    {
        _parameter ??= new Parameter();
        _parameter.ApplicationPaths = new string[] { "Path/To/Your/Application" };
        _parameter.LaunchTarget = 0;
        SaveParameters(filepath ?? DefaultFilePath);
    }

    public void SaveParameters(string? filepath = null)
    {
        filepath ??= DefaultFilePath;
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(filepath, JsonSerializer.Serialize(_parameter, options));
    }

    public bool IsFileExists(string? filepath = null)
    {
        filepath ??= DefaultFilePath;
        return File.Exists(filepath);
    }

    public void LoadParameters(string? filepath = null)
    {
        filepath ??= DefaultFilePath;
        _parameter ??= new Parameter();
        _parameter = JsonSerializer.Deserialize<Parameter>(File.ReadAllText(filepath));
    }

    public int ProcessStart(Action? whileAction = null, int intervalMs = 1000)
    {
        if (_parameter == null)
            throw new InvalidOperationException("Parameter object is null");

        if (_parameter.ApplicationPaths == null)
            throw new InvalidOperationException("Application paths is null");

        if (_process != null && _process.HasExited == false)
            throw new InvalidOperationException("Process already running");

        _process = new Process();
        _process.StartInfo.FileName = _parameter.ApplicationPaths[_parameter.LaunchTarget];
        _process.Start();

        do
        {
            if (!_process.HasExited)
            {
                _process.Refresh();
                whileAction?.Invoke();
            }
        }
        while (!_process.WaitForExit(intervalMs));

        int exitCode = _process.ExitCode;
        return exitCode;
    }

    public void SetLaunchTarget(int index)
    {
        if (_parameter == null)
            throw new InvalidOperationException("Parameter object is null");
        
        _parameter.LaunchTarget = index;
    }

    public string[] GetApplicationPaths()
    {
        if (_parameter == null)
            throw new InvalidOperationException("Parameter object is null");

        if (_parameter.ApplicationPaths == null)
            throw new InvalidOperationException("ApplicationPaths is null");

        return _parameter.ApplicationPaths;
    }

    public void Dispose()
    {
        if (_process != null && _process.HasExited == false)
        {
            _process.CloseMainWindow();
            _process.Close();
        }
    }
}

