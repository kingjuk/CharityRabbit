namespace CharityRabbit.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

#if DEBUG
        // Test hook: launch straight at a route, e.g.
        //   SIMCTL_CHILD_CR_START_PATH=/search xcrun simctl launch booted com.jdmsolutions.charityrabbit
        var startPath = Environment.GetEnvironmentVariable("CR_START_PATH");
        if (!string.IsNullOrEmpty(startPath))
        {
            blazorWebView.StartPath = startPath;
        }
#endif
    }
}
