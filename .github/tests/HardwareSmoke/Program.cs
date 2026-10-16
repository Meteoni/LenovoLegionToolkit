using Autofac;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Automation;
using LenovoLegionToolkit.Lib.Automation.Steps;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Settings;
using Windows.Devices.Display.Core;
using WindowsDisplayAPI.DisplayConfig;

// Optional hardware test. CI uses DrrRegression instead. This never saves app settings.
var settings = new ApplicationSettings();
settings.Store.DynamicRefreshRateLowFrequency = 50;
settings.Store.DynamicRefreshRateHighFrequency = 100;
var feature = new RefreshRateFeature(settings);
IoCContainer.Initialize(new DisplayTestModule(feature));

void Report(string label)
{
    using var manager = DisplayManager.Create(DisplayManagerOptions.VirtualRefreshRateAware);
    var result = manager.TryReadCurrentStateForAllTargets();
    foreach (var target in result.State.Targets.Where(target => target.IsConnected))
    {
        var path = result.State.GetPathForTarget(target);
        Console.WriteLine($"{label}: {target.DeviceInterfacePath}");
        foreach (var rate in new[] { path.PresentationRate, path.PhysicalPresentationRate })
        {
            if (rate is { } value)
                Console.WriteLine($"  WinRT rate: {value.VerticalSyncRate.Numerator}/{value.VerticalSyncRate.Denominator}, syncs per presentation: {value.VerticalSyncsPerPresentation}");
        }
    }
    foreach (var target in PathInfo.GetActivePaths(virtualModeAware: true).SelectMany(path => path.TargetsInfo))
        Console.WriteLine($"  CCD: boost={target.IsBoostRefreshRate}, virtual={target.FrequencyInMillihertz} mHz, signal={target.SignalInfo?.VerticalSyncFrequencyInMillihertz} mHz");
}

Report("Original");
if (!args.Contains("--switch"))
    return;

var originalPaths = PathInfo.GetActivePaths(virtualModeAware: true);
var dynamic100 = new RefreshRate(100, true, 50);
var fixed120 = new RefreshRate(120);
try
{
    if (!(await feature.GetAllStatesAsync()).Contains(dynamic100))
        throw new Exception("50/100 Hz is missing from the shared dashboard/Actions state list.");

    await feature.SetStateAsync(dynamic100);
    Report("Direct 50/100");
    if (await feature.GetStateAsync() != dynamic100)
        throw new Exception("Direct DRR readback did not match 50/100 Hz.");

    await feature.SetStateAsync(fixed120);
    Report("Direct fixed 120");
    if (await feature.GetStateAsync() != fixed120)
        throw new Exception("Direct fixed readback did not match 120 Hz.");

    var context = new AutomationContext();
    var environment = new AutomationEnvironment();
    var dcAction = new RefreshRateAutomationStep(dynamic100);
    if (!(await dcAction.GetAllStatesAsync()).Contains(dynamic100))
        throw new Exception("50/100 Hz is missing from Actions.");
    await dcAction.RunAsync(context, environment, default);
    Report("DC Action 50/100");
    if (await feature.GetStateAsync() != dynamic100)
        throw new Exception("DC Action did not restore 50/100 Hz.");

    await new RefreshRateAutomationStep(fixed120).RunAsync(context, environment, default);
    Report("AC Action fixed 120");
    if (await feature.GetStateAsync() != fixed120)
        throw new Exception("AC Action did not restore fixed 120 Hz.");
    Console.WriteLine("PASS: Direct and Actions switching to dynamic 50/100 Hz and fixed 120 Hz.");
}
finally
{
    PathInfo.ApplyPathInfos(originalPaths, saveToDatabase: true);
    Report("Restored");
}

sealed class DisplayTestModule(RefreshRateFeature feature) : Module
{
    protected override void Load(ContainerBuilder builder) => builder.RegisterInstance(feature).As<IFeature<RefreshRate>>();
}
