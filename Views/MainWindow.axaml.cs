using Avalonia.Controls;
using Avalonia.Input;

namespace FocusTimer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = -1;
    }

    private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }

    private void ResizeGrip_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginResizeDrag(WindowEdge.SouthEast, e);
            e.Handled = true;
        }
    }
    
    private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.Close();
    }

    private void TextBox_TextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm && sender is Avalonia.Controls.TextBox tb)
        {
            try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "FocusTimer", "debug.log"), $"{System.DateTime.Now:HH:mm:ss.fff}: TextChanged fired for '{tb.Name}' with text '{tb.Text}'. ViewModel currently has: '{vm.BlockedAppsString}'\n"); } catch {}

            if (tb.Name == "BlockedAppsTextBox")
                vm.BlockedAppsString = tb.Text ?? "";
            else if (tb.Name == "AllowedTabTextBox")
                vm.AllowedTabString = tb.Text ?? "";
            else if (tb.Name == "CustomMinsTextBox")
                vm.CustomMinutesString = tb.Text ?? "";
            else if (tb.Name == "BreakMinsTextBox")
                vm.BreakMinutesString = tb.Text ?? "";
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (DataContext is FocusTimer.ViewModels.MainViewModel vm)
        {
            if (vm.IsRunning && vm.IsIronWillEnabled)
            {
                e.Cancel = true;
            }
        }
    }

    protected override void OnClosed(System.EventArgs e)
    {
        base.OnClosed(e);
        System.Environment.Exit(0);
    }
}