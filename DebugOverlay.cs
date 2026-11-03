using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using static SpotifyTaskbarVisualizer.NativeMethods;

namespace SpotifyTaskbarVisualizer;

// Click-through window over the taskbar: red frames for everything the scanner found, green for the widget.
internal sealed class DebugOverlay : Window
{
    private readonly Canvas _canvas = new();
    private ZOrderGuard? _zOrder;

    public DebugOverlay()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        ResizeMode = ResizeMode.NoResize;
        Content = _canvas;
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TRANSPARENT);
            _zOrder = new ZOrderGuard(hwnd);
        };
        Closed += (_, _) => _zOrder?.Dispose();
    }

    // Rectangles in physical pixels; scale converts them to DIPs.
    public void Draw(RECT bar, IEnumerable<RECT> occupied, RECT? widget, double scale)
    {
        Left = bar.Left / scale;
        Top = bar.Top / scale;
        Width = (bar.Right - bar.Left) / scale;
        Height = bar.Height / scale;
        Show();

        _canvas.Children.Clear();
        foreach (var r in occupied) Add(r, Brushes.Red);
        if (widget is { } w) Add(w, Brushes.LimeGreen);
        _zOrder?.BringToFront();

        void Add(RECT r, Brush brush)
        {
            var frame = new Rectangle { Width = (r.Right - r.Left) / scale, Height = r.Height / scale, Stroke = brush, StrokeThickness = 1 };
            Canvas.SetLeft(frame, (r.Left - bar.Left) / scale);
            Canvas.SetTop(frame, (r.Top - bar.Top) / scale);
            _canvas.Children.Add(frame);
        }
    }
}
