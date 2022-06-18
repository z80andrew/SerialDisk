using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using SerialDiskUI.Models;
using SerialDiskUI.ViewModels;
using System;
using System.Threading.Tasks;

namespace SerialDiskUI.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        private readonly double _savedMinHeight;
        private const double _logMinHeight = 100;
        private double SavedWindowHeight { get; set; }
        public PixelPoint SavedWindowPosition { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(int width, int height, int xPos, int yPos)
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            _savedMinHeight = this.MinHeight;

            this.WhenActivated(d =>
                d(ViewModel.ShowSettingsDialog.RegisterHandler(DoShowSettingsDialogAsync)));
            this.WhenActivated(d =>
                d(ViewModel.ShowAboutDialog.RegisterHandler(DoShowAboutDialogAsync)));

            this.PositionChanged += MainWindow_PositionChanged;

            var logBorder = this.FindControl<Border>("LogBorder");

            this.WhenActivated(d =>
                ConfigureMainWindow(xPos, yPos, width, height, logBorder));

            this.WhenActivated(d =>
                d(ViewModel.WhenAnyValue(m => m.IsLogDisplayEnabled).Subscribe(isLogDisplayed =>
                {
                    logBorder.IsVisible = isLogDisplayed;
                })));

            var logScrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
            logScrollViewer.PropertyChanged += LogScrollViewer_PropertyChanged;
        }

        private void ConfigureMainWindow(int xPos, int yPos, int width, int height, Border logBorder)
        {            
            SetSize(width, height, logBorder);
            SetPosition(xPos, yPos);
        }

        private void SetSize(int width, int height, Border logBorder)
        {
            if (width > -1) Width = Convert.ToDouble(width);            
            if (height > -1) Height = Convert.ToDouble(height);
        }

        private void SetPosition(int xPos, int yPos)
        {
            if (xPos > -1 && yPos > -1)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Position = new PixelPoint(xPos, yPos);
            }
        }

        private void MainWindow_PositionChanged(object? sender, PixelPointEventArgs e)
        {
            SavedWindowPosition = Position;
        }

        private void LogScrollViewer_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            var logScrollViewer = sender as ScrollViewer;

            if (e.Property.Name == nameof(ScrollViewer.Extent))
            {
                logScrollViewer.ScrollToEnd();
                logScrollViewer.LineDown();
            }

            else if (e.Property.Name == nameof(ScrollViewer.TransformedBounds))
            {
                if (logScrollViewer.IsEffectivelyVisible) this.MinHeight = _savedMinHeight + _logMinHeight;
                
                else
                {
                    this.MinHeight = _savedMinHeight;
                    this.Height = MinHeight;
                }
            }
        }

        private async Task DoShowSettingsDialogAsync(InteractionContext<SettingsWindowViewModel, SerialDiskUIModel> interaction)
        {
            var dialog = new SettingsWindow
            {
                DataContext = interaction.Input
            };

            var result = await dialog.ShowDialog<SerialDiskUIModel>(this);
            interaction.SetOutput(result);
        }

        private async Task DoShowAboutDialogAsync(InteractionContext<AboutWindowViewModel, SimpleDialogModel> interaction)
        {
            var dialog = new AboutWindow
            {
                DataContext = interaction.Input
            };

            var result = await dialog.ShowDialog<SimpleDialogModel>(this);
            interaction.SetOutput(result);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
