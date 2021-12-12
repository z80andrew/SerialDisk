using Avalonia;
using Avalonia.ReactiveUI;

namespace SerialDiskUI
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            #region Dependency injection

            //var serviceCollection = new ServiceCollection();
            //ConfigureServices(serviceCollection);
            //var serviceProvider = serviceCollection.BuildServiceProvider();

            #endregion

            BuildAvaloniaApp()
              .StartWithClassicDesktopLifetime(args);
        }

        //private static void ConfigureServices(IServiceCollection serviceCollection)
        //{
        //    serviceCollection.AddSingleton<ILogger>(new Logger(Constants.LoggingLevel.Info));
        //}

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI()
                .UseSkia();
    }
}
