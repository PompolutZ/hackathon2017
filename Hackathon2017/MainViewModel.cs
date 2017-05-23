using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Tobii.Interaction;
using Tobii.Interaction.Wpf;
using Rectangle = Tobii.Interaction.Rectangle;

namespace Hackathon2017
{
    public class MainViewModel
    {
        private readonly INarrator _narrator;
        private readonly Host _host;
        private RelayCommand _narrateGazeOutOfBoundsCommand;
        private Rectangle _screenBounds;
        private VirtualInteractorAgent _agent;
        private VirtualInteractor _screen;
        private VirtualWindowsAgent _vwagent;
        private VirtualWindowBase _screenWindow;
        private ConcurrentQueue<IntPtr> _hwnds;
        private IntPtr _lastHwnd;
        private string _lastTitle;
        
        public MainViewModel(INarrator narrator, Host host)
        {
            _hwnds = new ConcurrentQueue<IntPtr>();
            _narrator = narrator;
            _host = host;
            Task.Run(InitStates);
            _host.Streams.CreateGazePointDataStream().GazePoint(CheckGazePointIsInScreenBounds);
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    IntPtr hwnd;
                    while (_hwnds.TryDequeue(out hwnd))
                    {
                        Debug.WriteLine($"{hwnd} at {DateTime.UtcNow}");
                        var title = GetWindowTitle(hwnd);
                        _narrator.Cancel(_lastTitle);
                        _narrator.Say(title);
                        _lastTitle = title;
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        public ICommand SettingElementHasGazeChangedCommand
        {
            get
            {
                return _narrateGazeOutOfBoundsCommand ?? new RelayCommand(
                           args =>
                           {
                               var hasGazeChangedArgs = args as HasGazeChangedArgs;

                               var grid = hasGazeChangedArgs?.Interactor.Element as Grid;
                               if (grid == null) return;

                               var settingsButton = grid.Parent as Button;
                               var wrapPanel = settingsButton?.Parent as WrapPanel;
                               var indexOfSettingsItem = wrapPanel?.Children.IndexOf(settingsButton);
                               var itemsCount = wrapPanel?.Children.Count;

                               var textBlock = grid.Children.OfType<TextBlock>().FirstOrDefault();

                               var phrase = $"{textBlock?.Text}. {indexOfSettingsItem + 1} out of {itemsCount}.";
                               Debug.WriteLine(phrase);

                               if (hasGazeChangedArgs.HasGaze)
                               {
                                   _narrator.Say(phrase);
                               }
                               else
                               {
                                   _narrator.Cancel(phrase);
                               }
                           });
            }
        }

        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(System.Drawing.Point p);

        private async Task InitStates()
        {
            var screenBoundsState = await _host.States.GetScreenBoundsAsync();
            if (!screenBoundsState.IsValid)
                return;

            _screenBounds = screenBoundsState.Value;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public string GetWindowTitle(IntPtr id)
        {
            int length = GetWindowTextLength(id);
            StringBuilder title = new StringBuilder(length + 1);
            GetWindowText(id, title, title.Capacity);
            return title.ToString();
        }


        private void CheckGazePointIsInScreenBounds(double x, double y, double timestamp)
        {
            var hwnd = WindowFromPoint(new Point((int) x, (int) y));
            if (hwnd == _lastHwnd || hwnd == IntPtr.Zero)
            {
                return;
            }

            _hwnds.Enqueue(hwnd);
            _lastHwnd = hwnd;

            //Debug.WriteLine(GetWindowTitle(hwnd));
            //int gazePointX = (int) x;
            //int gazePointY = (int) y;

            //if (gazePointX < _screenBounds.X || gazePointX > _screenBounds.Width ||
            //    gazePointY < _screenBounds.Y || gazePointY > _screenBounds.Height)
            //{
            //    _narrator.Say("Outside screen bounds.");
            //}
            //else
            //{
            //    _narrator.Cancel("Outside screen bounds.");
            //}
        }
    }
}