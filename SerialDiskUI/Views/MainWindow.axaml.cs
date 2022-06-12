using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using SerialDiskUI.Models;
using SerialDiskUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Reactive;
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

            logBorder.PropertyChanged += LogBorder_PropertyChanged;

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
            
            if (height > -1 && logBorder.IsVisible)
            {
                SavedWindowHeight = Convert.ToDouble(height);
                EnableWindowResize();
            }

            else
            {
                DisableWindowResize();
                this.MaxHeight = Height;
            }
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

        private void LogBorder_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == nameof(Border.IsVisible))
            {
                if (!(bool)e.NewValue) DisableWindowResize();
                else EnableWindowResize();
            }

            else if (e.Property.Name == nameof(Border.TransformedBounds))
            {
                var logBorder = sender as Border;

                if (!logBorder.IsVisible && logBorder.TransformedBounds == null)
                {
                    this.MinHeight = this.Height;
                    this.MaxHeight = this.Height;
                }
            }
        }

        private void LogScrollViewer_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == nameof(ScrollViewer.Extent))
            {
                var logScrollViewer = sender as ScrollViewer;
                logScrollViewer.ScrollToEnd();
                logScrollViewer.LineDown();
            }
        }

        private void EnableWindowResize()
        {
            this.MinHeight = _savedMinHeight + _logMinHeight;
            this.MaxHeight = double.PositiveInfinity;
            this.SizeToContent = SizeToContent.Manual;
            this.Height = SavedWindowHeight;
        }

        private void DisableWindowResize()
        {
            this.MinHeight = _savedMinHeight;
            SavedWindowHeight = this.Height;
            this.SizeToContent = SizeToContent.Height;
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
