using Avalonia;
using ReactiveUI.Avalonia;
using System;

namespace Z80andrew.SerialDisk.SerialDiskUI
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            BuildAvaloniaApp()
              .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
		        .UseReactiveUI(rxui => { /* Optional: add custom registration here via rxui.WithRegistration(...) */ })
		        .RegisterReactiveUIViewsFromEntryAssembly()
                .UseSkia();
    }
}
