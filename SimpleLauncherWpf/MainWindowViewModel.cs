using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.IO;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Threading;

using SimpleLauncher;

namespace SimpleLauncherWpf
{
    public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ReactiveCollection<ApplicationSelectItem> ApplicationItems { get; set; } = new ReactiveCollection<ApplicationSelectItem>();

        public ReactiveProperty<bool> IsProcessRunning { get; } = new ReactiveProperty<bool>();
        public ReadOnlyReactivePropertySlim<bool> IsProcessNotRunning { get; private set; }

        public ReactivePropertySlim<string> ProcessStatus { get; } = new ReactivePropertySlim<string>();

        public ReactivePropertySlim<string> Message { get; } = new ReactivePropertySlim<string>();

        public ReactiveCommand ReloadCommand { get; private set; }

        public ReactiveCommand<EventArgs> LoadedCommand { get; } = new ReactiveCommand<EventArgs>();

        private SimpleLauncher.SimpleLauncher _launcher = new();

        private CompositeDisposable _disposables = new CompositeDisposable();
        private Dispatcher _dispatcher;
        private System.Timers.Timer _timer = new(1000);
        private StringBuilder _sb = new StringBuilder();

        public MainWindowViewModel()
        {
            _dispatcher = Application.Current.Dispatcher;

            IsProcessRunning.Value = false;

            ReloadCommand = IsProcessRunning.Select(x => !x).ToReactiveCommand();
            ReloadCommand.Subscribe(() => InitializeParameter());

            IsProcessNotRunning = IsProcessRunning
                .Select(x => !x)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(_disposables);

            

            _timer.Elapsed += (s, e) => UpdateProcessStatus();

            LoadedCommand.Subscribe(_ =>
            {
                bool ready = InitializeParameter();
                if (ready)
                {
                    RunProcess();
                }
            });
        }

        bool InitializeParameter()
        {
            if (IsProcessRunning.Value) return false;

            ApplicationItems.Clear();
            Message.Value = string.Empty;
            string filepath = _launcher.DefaultFilePath;

            if (!_launcher.IsFileExists())
            {
                _launcher.SaveAsTemplate();
                Message.Value = $"パラメータファイルが見つかりません。テンプレートファイルを作成しました: {filepath}";
            }
            else
            {
                try
                {
                    _launcher.LoadParameters();

                    int n = _launcher.ApplicationCount;
                    var apps = _launcher.GetApplicationPaths();
                    for (int i = 0; i < n; ++i)
                    {
                        string name = Path.GetFileNameWithoutExtension(apps[i]);
                        ApplicationItems.Add(new ApplicationSelectItem(i, name, ApplicationSelected));
                    }
                    Message.Value = $"パラメータファイルを読み込みました: {filepath}";
                    return true;
                }
                catch (Exception ex)
                {
                    Message.Value = $"エラー {ex.GetType()}: {ex.Message}, Filepath={filepath}";
                }
            }
            return false;
        }

        void RunProcess()
        {
            IsProcessRunning.Value = true;
            ProcessStatus.Value = "Process starting...";
            _timer.Start();
            int exitCode = -1;

            var task = Task.Run(async () =>
            {
                exitCode = await _launcher.ProcessStartAsync();
            });

            task.ContinueWith(ca =>
            {
                _timer.Stop();

                string resultMsg = ca.Exception is AggregateException ex
                    ? $"例外が発生しました: {ex.InnerException?.GetType()} - {ex.InnerException?.Message}"
                    : $"プロセスを終了しました: EXIT_CODE={exitCode}";

                _dispatcher.Invoke(() =>
                {
                    ProcessStatus.Value = "Process stopped";
                    IsProcessRunning.Value = false;
                    Message.Value = resultMsg;
                });
            });
        }

        void UpdateProcessStatus()
        {
            if (IsProcessRunning.Value)
            {
                try
                {
                    var proc = _launcher.Process;
                    if (proc == null) return;

                    // Display current process statistics.
                    _sb.Clear();
                    _sb.AppendLine($"{proc.ProcessName}, ID:{proc.Id}");
                    _sb.AppendLine("-------------------------------------");

                    _sb.AppendLine($"  Physical memory usage     : {proc.WorkingSet64}");
                    _sb.AppendLine($"  Base priority             : {proc.BasePriority}");
                    _sb.AppendLine($"  User processor time       : {proc.UserProcessorTime}");
                    _sb.AppendLine($"  Privileged processor time : {proc.PrivilegedProcessorTime}");
                    _sb.AppendLine($"  Total processor time      : {proc.TotalProcessorTime}");
                    _sb.AppendLine($"  Paged system memory size  : {proc.PagedSystemMemorySize64}");
                    _sb.AppendLine($"  Paged memory size         : {proc.PagedMemorySize64}");

                    if (proc.Responding)
                    {
                        _sb.AppendLine("Status = Running");
                    }
                    else
                    {
                        _sb.AppendLine("Status = Not Responding");
                    }
                }
                catch (Exception ex)
                {
                    _sb.Append(ex.ToString());
                }

                try
                {
                    _dispatcher.Invoke(() =>
                    {
                        ProcessStatus.Value = _sb.ToString();
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }



        void ApplicationSelected(int index)
        {
            if (index < 0 || index >= _launcher.ApplicationCount)
                return;

            _launcher.SetLaunchTarget(index);
            _launcher.SaveParameters();
            RunProcess();
        }

        public void Dispose()
        {
            _launcher.Dispose();
            _disposables.Dispose();
        }
    }

    public class ApplicationSelectItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ReactivePropertySlim<int> Index { get; } = new ReactivePropertySlim<int>();
        public ReactivePropertySlim<string> Name { get; } = new ReactivePropertySlim<string>();

        public ReactiveCommand SelectCommand { get; } = new ReactiveCommand();

        private Action<int> _callback;

        public ApplicationSelectItem(int index, string name, Action<int> callback)
        {
            Index.Value = index;
            Name.Value = name;
            _callback = callback;

            SelectCommand.Subscribe(() => _callback(Index.Value));
        }
    }
}
