using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Automation;
using LenovoLegionToolkit.Lib.Automation.Steps;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.System;
using WindowsDisplayAPI;
using WindowsDisplayAPI.DisplayConfig;

var settings = new ApplicationSettings();
settings.Store.DynamicRefreshRateLowFrequency = 50;
settings.Store.DynamicRefreshRateHighFrequency = 100;
var feature = new RefreshRateFeature(settings);
IoCContainer.Feature = feature;
var display = InternalDisplay.Current!;
var screen = display.DisplayScreen;
var dynamic100 = new RefreshRate(100, true, 50);
var fixed120 = new RefreshRate(120);
var assertions = 0;

void Check(bool condition, string message)
{
    if (!condition)
        throw new Exception(message);
    assertions++;
    Console.WriteLine($"PASS: {message}");
}

Check(!screen.GetPossibleSettings().Any(mode => mode.Frequency == 50), "Fixture has no static 50 Hz mode");
Check((await feature.GetAllDynamicStatesAsync()).Contains(dynamic100), "Dashboard exposes configured 50/100 Hz");
var dcAction = new RefreshRateAutomationStep(dynamic100);
Check((await dcAction.GetAllStatesAsync()).Contains(dynamic100), "Actions exposes configured 50/100 Hz");
Check((await dcAction.GetAllStatesAsync()).Contains(fixed120), "Actions retains fixed 120 Hz");

await feature.SetStateAsync(dynamic100);
Check(await feature.GetStateAsync() == dynamic100, "Direct switching applies 50/100 Hz without a static 50 Hz mode");
Check(PathInfo.Active[0].TargetsInfo[0].IsBoostRefreshRate, "Direct switching enables the Windows boost flag");
Check(screen.PhysicalChanges.Last() == 100, "DRR pre-initializes physical 100 Hz, not virtual 50 Hz");

await feature.SetStateAsync(fixed120);
Check(await feature.GetStateAsync() == fixed120, "Direct switching restores fixed 120 Hz");
Check(!PathInfo.Active[0].TargetsInfo[0].IsBoostRefreshRate, "Static switching clears the boost flag");

await dcAction.RunAsync(new AutomationContext(), new AutomationEnvironment(), default);
Check(await feature.GetStateAsync() == dynamic100, "DC Action applies 50/100 Hz through production Actions code");
await new RefreshRateAutomationStep(fixed120).RunAsync(new AutomationContext(), new AutomationEnvironment(), default);
Check(await feature.GetStateAsync() == fixed120, "AC Action applies fixed 120 Hz through production Actions code");
Check(screen.CurrentSetting.Position == new System.Drawing.Point(200, 100) && screen.CurrentSetting.ColorDepth == 32,
    "Switching preserves display position and color depth");

settings.Store.DynamicRefreshRateLowFrequency = 40;
Check((await feature.GetAllDynamicStatesAsync()).Contains(new RefreshRate(100, true, 40)), "Custom virtual rates are configurable");
await feature.SetStateAsync(dynamic100);
Check(await feature.GetStateAsync() == dynamic100, "Readback uses the active range, even after the configured default changes");
settings.Store.DynamicRefreshRateLowFrequency = 50;

screen.Frequencies = [60, 120];
Check(!(await feature.GetAllStatesAsync()).Contains(dynamic100), "Hide custom range if the physical mode disappears");
screen.Frequencies = [60, 100, 120];

settings.Store.DynamicRefreshRateLowFrequency = 100;
Check(!(await feature.GetAllDynamicStatesAsync()).Any(rate => rate.BaseFrequency >= rate.Frequency), "Invalid saved ranges never appear");
settings.Store.DynamicRefreshRateLowFrequency = 0;
settings.Store.DynamicRefreshRateHighFrequency = 0;
Check((await feature.GetAllDynamicStatesAsync()).Contains(new RefreshRate(120, true, 60)), "Reset preserves automatic upstream ranges");
settings.Store.DynamicRefreshRateLowFrequency = 50;
settings.Store.DynamicRefreshRateHighFrequency = 100;

await feature.SetStateAsync(fixed120);
PathInfo.RejectApply = true;
await feature.SetStateAsync(dynamic100);
Check(await feature.GetStateAsync() == new RefreshRate(100), "Preserve upstream static fallback after a driver error");
PathInfo.RejectApply = false;
PathInfo.IgnoreBoost = true;
await dcAction.RunAsync(new AutomationContext(), new AutomationEnvironment(), default);
Check(!(await feature.GetStateAsync()).IsDynamic, "Readback distinguishes a driver that ignores the boost flag");
PathInfo.IgnoreBoost = false;

await feature.SetStateAsync(dynamic100);
PathInfo.Active[0].TargetsInfo[0].SignalInfo!.VerticalSyncFrequencyInMillihertz = 120001;
Check(await feature.GetStateAsync() == dynamic100, "Keep the selected 50/100 range when the driver reports a 120 Hz transport signal");
PathInfo.Active[0].TargetsInfo[0].SignalInfo!.VerticalSyncFrequencyInMillihertz = 99950;
Check(await feature.GetStateAsync() == dynamic100, "Round fractional physical timings to the advertised 100 Hz rate");

OSExtensions.Current = OS.Windows10;
Check((await feature.GetAllDynamicStatesAsync()).Length == 0, "Do not advertise DRR on Windows 10");
OSExtensions.Current = OS.Windows11;
display.IsInternal = false;
Check((await feature.GetAllDynamicStatesAsync()).Length == 0, "Do not advertise DRR on an external display");
display.IsInternal = true;
await feature.SetStateAsync(fixed120);
PathInfo.Active[0].TargetsInfo[0].IsDynamicRefreshRateSupported = false;
Check((await feature.GetAllDynamicStatesAsync()).Length == 0, "Retain upstream hardware DRR capability checks");

Console.WriteLine($"All {assertions} refresh-rate regression checks passed.");
