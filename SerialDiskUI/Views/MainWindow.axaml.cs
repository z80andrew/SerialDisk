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
        private double SavedWindowHeight { get; set; }

        public PixelPoint SavedWindowPosition { get; set; }

        private TextBlock _logTextBlock;
        private ScrollViewer _logScrollViewer;

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
            this.WhenActivated(d =>
                d(ViewModel.ShowSettingsDialog.RegisterHandler(DoShowSettingsDialogAsync)));
            this.WhenActivated(d =>
                d(ViewModel.ShowAboutDialog.RegisterHandler(DoShowAboutDialogAsync)));
            this.WhenActivated(d =>
                SetPosition(xPos, yPos));

            this.PositionChanged += MainWindow_PositionChanged;

            var logBorder = this.FindControl<Border>("LogBorder");

            this.WhenActivated(d =>
                SetSize(width, height, logBorder));

            this.WhenActivated(d =>
                d(ViewModel.WhenAnyValue(m => m.IsLogDisplayEnabled).Subscribe(isLogDisplayed =>
                {
                    logBorder.IsVisible = isLogDisplayed;
                })));

            logBorder.PropertyChanged += LogBorder_PropertyChanged;

            _logScrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
            _logTextBlock = this.FindControl<TextBlock>("LogText");

            _logScrollViewer.PropertyChanged += _logScrollViewer_PropertyChanged;
        }

        private void SetSize(int width, int height, Border logBorder)
        {
            if (width > -1) Width = Convert.ToDouble(width);
            
            if (height > -1 && logBorder.IsVisible)
            {
                this.SizeToContent = SizeToContent.Manual;
                Height = Convert.ToDouble(height);
            }

            else
            {
                this.MinHeight = Height;
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
            var logBorder = sender as Border;

            if (e.Property.Name == nameof(logBorder.IsVisible))
            {
                if (!logBorder.IsVisible) DisableWindowResize();
                else EnableWindowResize();
            }

            else if (e.Property.Name == nameof(logBorder.TransformedBounds))
            {
                if (!logBorder.IsVisible && logBorder.TransformedBounds == null)
                {
                    this.MinHeight = this.Height;
                    this.MaxHeight = this.Height;
                }
            }
        }

        private void _logScrollViewer_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == nameof(_logScrollViewer.Extent))
            {
                _logScrollViewer.ScrollToEnd();
                _logScrollViewer.LineDown();
            }
        }

        private void EnableWindowResize()
        {
            this.MaxHeight = double.PositiveInfinity;
            this.SizeToContent = SizeToContent.Manual;
            this.Height = SavedWindowHeight;
        }

        private void DisableWindowResize()
        {
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
