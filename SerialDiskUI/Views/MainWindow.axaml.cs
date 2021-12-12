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
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            //this.WhenActivated(d =>
            //    d(ViewModel.ToggleLogVisibility.RegisterHandler(DoToggleLogVisibility)));
            this.WhenActivated(d =>
                d(ViewModel.ShowSettingsDialog.RegisterHandler(DoShowDialogAsync)));

            this.FindControl<Expander>("LogExpander").PropertyChanged += LogExpander_PropertyChanged;
        }

        private void LogExpander_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            var thingy = sender as Expander;

            if (e.Property.Name == "IsExpanded" && thingy != null)
            {
                if (thingy.IsExpanded == false)
                {
                    this.SizeToContent = SizeToContent.Height;
                }
            }
        }

        private async Task DoShowDialogAsync(InteractionContext<SettingsWindowViewModel, SerialDiskUIModel> interaction)
        {
            var dialog = new SettingsWindow
            {
                DataContext = interaction.Input
            };

            var result = await dialog.ShowDialog<SerialDiskUIModel>(this);
            interaction.SetOutput(result);
        }

        private async Task DoToggleLogVisibility(InteractionContext<bool, Unit> interaction)
        {
            if (interaction.Input == false)
            {
                this.SizeToContent = SizeToContent.Height;
            }

            interaction.SetOutput(Unit.Default);
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
