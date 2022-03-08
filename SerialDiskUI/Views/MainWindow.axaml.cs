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
        private double SavedWindowHeight;

        private TextBlock _logTextBlock;
        private ScrollViewer _logScrollViewer;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            //LogCommand = ReactiveCommand.CreateFromTask(async () =>
            //{
            //    (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).Shutdown(0);
            //});

            this.WhenActivated(d =>
                d(ViewModel.ShowSettingsDialog.RegisterHandler(DoShowSettingsDialogAsync)));

            this.WhenActivated(d =>
                d(ViewModel.ShowAboutDialog.RegisterHandler(DoShowAboutDialogAsync)));

            var logExpander = this.FindControl<Expander>("LogExpander");

            // logExpander.PropertyChanged += LogExpander_PropertyChanged;

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

            // SavedWindowHeight = this.Height + 100;
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
            this.MinHeight = 0;
            SavedWindowHeight = this.Height;
            this.SizeToContent = SizeToContent.Height;
        }

        private void LogExpander_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            var logExpander = sender as Expander;

            if (logExpander != null)
            {
                if (e.Property.Name == nameof(logExpander.IsExpanded))
                {
                    if (logExpander.IsExpanded) EnableWindowResize();
                    else DisableWindowResize();
                }

                else if (e.Property.Name == nameof(logExpander.Bounds))
                {
                    // Need to set this after bounds have changed, which is after IsExpanded has been changed
                    if (!logExpander.IsExpanded)
                    {
                        this.SizeToContent = SizeToContent.Height;
                        this.MaxHeight = this.Height;
                        this.MinHeight = this.Height;
                    }
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
