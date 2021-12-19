using System.ComponentModel;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using SerialDiskUI.Models;
using SerialDiskUI.ViewModels;

namespace SerialDiskUI.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        private double savedWindowHeight;

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

            this.FindControl<Expander>("LogExpander").PropertyChanged += LogExpander_PropertyChanged;
            
            _logScrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
            _logTextBlock = this.FindControl<TextBlock>("LogText");

            _logScrollViewer.PropertyChanged += _logScrollViewer_PropertyChanged;

            savedWindowHeight = this.Height + 100;
        }

        private void _logScrollViewer_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == nameof(_logScrollViewer.Extent))
            {
                _logScrollViewer.ScrollToEnd();
            }
        }

        private void LogExpander_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            var logExpander = sender as Expander;

            if (logExpander != null)
            {
                if (e.Property.Name == nameof(logExpander.IsExpanded))
                {
                    if (!logExpander.IsExpanded)
                    {
                        savedWindowHeight = this.Height;
                        this.SizeToContent = SizeToContent.Height;
                    }

                    else
                    {
                        this.MaxHeight = double.PositiveInfinity;
                        this.SizeToContent = SizeToContent.Manual;
                        this.Height = savedWindowHeight;
                    }
                }

                else if (e.Property.Name == nameof(logExpander.Bounds))
                {
                    // Need to set this after bounds have changed, which is after IsExpanded has been changed
                    if (!logExpander.IsExpanded) this.MaxHeight = this.Height;
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

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }
    }
}
