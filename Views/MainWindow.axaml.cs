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
            if (tb == BlockedAppsTextBox)
                vm.BlockedAppsString = tb.Text ?? "";
            else if (tb == AllowedTabTextBox)
                vm.AllowedTabString = tb.Text ?? "";
            else if (tb == CustomMinsTextBox)
                vm.CustomMinutesString = tb.Text ?? "";
            else if (tb == BreakMinsTextBox)
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