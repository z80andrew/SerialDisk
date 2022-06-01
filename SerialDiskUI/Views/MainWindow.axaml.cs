using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using SerialDiskUI.Models;
using SerialDiskUI.ViewModels;
using System;
using System.Reactive;
using System.Threading.Tasks;

namespace SerialDiskUI.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        private double SavedWindowHeight { get; set; }

        private double _windowMinHeight;
        private double WindowMinHeight
        {
            get => _logScrollViewer.IsVisible ? _windowMinHeight + 50 : _windowMinHeight;
            set => _windowMinHeight = value;
        }

        private TextBlock _logTextBlock;
        private ScrollViewer _logScrollViewer;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            this.WhenActivated(d =>
                d(ViewModel.ShowSettingsDialog.RegisterHandler(DoShowSettingsDialogAsync)));

            this.WhenActivated(d =>
                d(ViewModel.ShowAboutDialog.RegisterHandler(DoShowAboutDialogAsync)));


            var logBorder = this.FindControl<Border>("LogBorder");
            
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
            this.MinHeight = WindowMinHeight;
            this.MaxHeight = double.PositiveInfinity;
            this.SizeToContent = SizeToContent.Manual;
            this.Height = SavedWindowHeight;
        }

        private void DisableWindowResize()
        {
            WindowMinHeight = this.MinHeight;
            this.MinHeight = 0;
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
