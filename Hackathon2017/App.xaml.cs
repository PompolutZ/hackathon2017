using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Tobii.Interaction;
using Tobii.Interaction.Framework;
using Tobii.Interaction.Model;

namespace Hackathon2017
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Host _host;
        private MainViewModel _mainViewModel;
        private MainWindow _mainView;

        protected override void OnStartup(StartupEventArgs e)
        {
            _host = new Host();

            _host.InitializeWpfAgent();
            
            _mainViewModel = new MainViewModel(new SimpleNarrator(), _host);
            _mainView = new MainWindow
            {
                DataContext = _mainViewModel
            };

            _mainView.Show();
            BluredWindow bw = new BluredWindow();
            bw.Show();

            _host.AddInteractorAgent(new CustomVirtualAgent("ProcessesListener", _mainView), "*");
        }
    }


    public class CustomVirtualAgent : VirtualInteractorAgent
    {
        private Subject<string> _ids = new Subject<string>();
        private IDisposable _disposable;
        public CustomVirtualAgent(string name, Window view) : base(name, new WindowInteropHelper(view).Handle.ToString())
        {
            _disposable = _ids.Subscribe(x => Debug.WriteLine(x));
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public CustomVirtualAgent(string agentId, string defaultWindowId) : base(agentId, defaultWindowId)
        {
        }

        protected override Task PopulateSnapshotAsync(Snapshot snapshot, Query query, CancellationToken cancellationToken)
        {
            //Debug.WriteLine(string.Join(";", query.WindowIds.Select(id => id.StartsWith(Literals.VirtualWindowPrefix) ? "" : GetWindowTitle(int.Parse(id)))));

            return base.PopulateSnapshotAsync(snapshot, query, cancellationToken);
        }

        public string GetWindowTitle(int id)
        {
            int length = GetWindowTextLength(new IntPtr(id));
            StringBuilder title = new StringBuilder(length + 1);
            GetWindowText(new IntPtr(id), title, title.Capacity);
            return title.ToString();
        }
    }
}