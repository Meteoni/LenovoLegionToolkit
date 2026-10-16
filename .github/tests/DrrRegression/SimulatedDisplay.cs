// Only the Windows driver boundary and unrelated infrastructure are simulated.
// RefreshRateFeature, DisplayExtensions and both Actions step classes are production sources.
using System.Drawing;
using LenovoLegionToolkit.Lib.Features;
using WindowsDisplayAPI;
using WindowsDisplayAPI.DisplayConfig;

namespace LenovoLegionToolkit.Lib
{
    public enum OS { Windows10, Windows11 }
    public readonly record struct RefreshRate
    {
        public int Frequency { get; }
        public bool IsDynamic { get; }
        public int BaseFrequency { get; }
        public RefreshRate(int frequency, bool isDynamic = false, int baseFrequency = 0)
        {
            Frequency = frequency; IsDynamic = isDynamic; BaseFrequency = baseFrequency;
        }
    }
    public static class IoCContainer
    {
        public static IFeature<RefreshRate> Feature { get; set; } = null!;
        public static T Resolve<T>() => (T)Feature;
    }
}
namespace LenovoLegionToolkit.Lib.Settings
{
    public class ApplicationSettings
    {
        public class StoreData
        {
            public int DynamicRefreshRateLowFrequency { get; set; }
            public int DynamicRefreshRateHighFrequency { get; set; }
        }
        public StoreData Store { get; } = new();
    }
}
namespace LenovoLegionToolkit.Lib.Utils
{
    public class Log
    {
        public static Log Instance { get; } = new();
        public void Trace(string message, Exception? exception = null) { }
    }
}
namespace LenovoLegionToolkit.Lib.Extensions
{
    public static class OSExtensions
    {
        public static LenovoLegionToolkit.Lib.OS Current { get; set; } = LenovoLegionToolkit.Lib.OS.Windows11;
        public static LenovoLegionToolkit.Lib.OS GetCurrent() => Current;
    }
}
namespace LenovoLegionToolkit.Lib.System
{
    public static class InternalDisplay
    {
        public static Display? Current { get; set; } = new();
        public static Task<Display?> GetAsync() => Task.FromResult(Current);
    }
}
namespace Newtonsoft.Json
{
    [AttributeUsage(AttributeTargets.Constructor)]
    public class JsonConstructorAttribute : Attribute;
}
namespace LenovoLegionToolkit.Lib.Messaging
{
    public static class MessagingCenter { public static void Publish<T>(T message) { } }
}
namespace LenovoLegionToolkit.Lib.Messaging.Messages
{
    public record FeatureStateMessage<T>(T State);
}
namespace LenovoLegionToolkit.Lib.Automation
{
    public class AutomationContext;
    public class AutomationEnvironment;
}
namespace LenovoLegionToolkit.Lib.Automation.Steps
{
    public interface IAutomationStep { }
    public interface IAutomationStep<T> : IAutomationStep { }
}
namespace WindowsDisplayAPI.Exceptions { public class ModeChangeException : Exception; }
namespace WindowsDisplayAPI.Native.DeviceContext { public class UnusedNamespaceMarker; }
namespace WindowsDisplayAPI
{
    public class DisplayPossibleSetting
    {
        public Size Resolution { get; set; } = new(2560, 1600);
        public int ColorDepth { get; set; } = 32;
        public bool IsInterlaced { get; set; }
        public int Frequency { get; set; }
        public bool IsTooSmall() => false;
    }
    public class DisplaySetting : DisplayPossibleSetting
    {
        public Point Position { get; set; } = new(200, 100);
        public int Orientation { get; set; }
        public int OutputScalingMode { get; set; }
        public DisplaySetting() { Frequency = 120; }
        public DisplaySetting(Size resolution, Point position, int colorDepth, int frequency, bool isInterlaced, int orientation, int outputScalingMode)
        {
            Resolution = resolution; Position = position; ColorDepth = colorDepth; Frequency = frequency;
            IsInterlaced = isInterlaced; Orientation = orientation; OutputScalingMode = outputScalingMode;
        }
        public string ToExtendedString() => $"{Resolution}, {Frequency} Hz";
    }
    public class Display
    {
        public bool IsInternal { get; set; } = true;
        public DisplayScreen DisplayScreen { get; } = new();
        public PathDisplayTarget ToPathDisplayTarget() => PathDisplayTarget.Instance;
    }
    public class DisplayScreen
    {
        public int[] Frequencies { get; set; } = [60, 100, 120];
        public List<int> PhysicalChanges { get; } = [];
        public DisplaySetting CurrentSetting { get; set; } = new();
        public IEnumerable<DisplayPossibleSetting> GetPossibleSettings() => Frequencies.Select(f => new DisplayPossibleSetting { Frequency = f });
        public object ToPathDisplaySource() => "internal";
        public void SetSettings(DisplaySetting setting, bool apply)
        {
            PhysicalChanges.Add(setting.Frequency);
            CurrentSetting = setting;
            PathInfo.Active = [new PathInfo("internal", setting.Position, setting.Resolution, setting.ColorDepth,
                [new PathTargetInfo(PathDisplayTarget.Instance, new PathTargetSignalInfo(setting, setting.Resolution), 0, 0, true, false)])];
        }
    }
}
namespace WindowsDisplayAPI.DisplayConfig
{
    public struct DisplayAdvancedColorInfo;
    public class PathDisplayTarget
    {
        public static PathDisplayTarget Instance { get; } = new();
        // Emulate the default detector refusing sub-120 Hz physical modes.
        public int GetDynamicLowFrequency(IEnumerable<int> frequencies) => frequencies.DefaultIfEmpty().Max() >= 120 ? 60 : 0;
        public DisplayAdvancedColorInfo GetAdvancedColorInfo() => default;
        public void SetAdvancedColorState(bool state) { }
        public void SetHdrState(bool state) { }
        public void SetWcgState(bool state) { }
    }
    public class PathTargetSignalInfo
    {
        public ulong VerticalSyncFrequencyInMillihertz { get; set; } = 120000;
        public PathTargetSignalInfo() { }
        public PathTargetSignalInfo(DisplaySetting setting, Size resolution) => VerticalSyncFrequencyInMillihertz = (ulong)setting.Frequency * 1000;
    }
    public class PathTargetDesktopImage(Size size, Rectangle source, Rectangle target);
    public class PathTargetInfo
    {
        public PathDisplayTarget DisplayTarget { get; } = PathDisplayTarget.Instance;
        public bool IsBoostRefreshRate { get; set; }
        public bool IsDynamicRefreshRateSupported { get; set; } = true;
        public bool IsVirtualModeSupportedByPath { get; set; } = true;
        public bool IsDesktopImageInformationAvailable { get; set; } = true;
        public bool IsSignalInformationAvailable => SignalInfo is not null;
        public PathTargetSignalInfo? SignalInfo { get; set; } = new();
        public ulong FrequencyInMillihertz { get; set; } = 120000;
        public int Rotation { get; }
        public int Scaling { get; }
        public PathTargetInfo() { }
        public PathTargetInfo(PathDisplayTarget target, PathTargetSignalInfo signal, int rotation, int scaling, bool virtualMode, bool boost)
        {
            DisplayTarget = target; SignalInfo = signal; Rotation = rotation; Scaling = scaling;
            IsVirtualModeSupportedByPath = virtualMode; IsBoostRefreshRate = boost;
            FrequencyInMillihertz = signal.VerticalSyncFrequencyInMillihertz;
        }
        public PathTargetInfo(PathDisplayTarget target, PathTargetSignalInfo signal, PathTargetDesktopImage desktop, int rotation, int scaling, bool virtualMode, bool boost, ulong frequency)
            : this(target, signal, rotation, scaling, virtualMode, boost) => FrequencyInMillihertz = frequency;
    }
    public class PathInfo(object source, Point position, Size resolution, int pixelFormat, PathTargetInfo[] targets)
    {
        public static PathInfo[] Active { get; set; } = [new("internal", new(200, 100), new(2560, 1600), 32, [new()])];
        public static bool RejectApply { get; set; }
        public static bool IgnoreBoost { get; set; }
        public object DisplaySource { get; } = source;
        public Point Position { get; } = position;
        public int PixelFormat { get; } = pixelFormat;
        public PathTargetInfo[] TargetsInfo { get; } = targets;
        public static PathInfo[] GetActivePaths(bool virtualModeAware) => Active.ToArray();
        public static void ApplyPathInfos(PathInfo[] paths, bool saveToDatabase)
        {
            if (RejectApply) throw new InvalidOperationException("Driver rejected the configuration");
            Active = paths;
            if (IgnoreBoost) Active[0].TargetsInfo[0].IsBoostRefreshRate = false;
            LenovoLegionToolkit.Lib.System.InternalDisplay.Current!.DisplayScreen.CurrentSetting.Frequency = (int)(Active[0].TargetsInfo[0].FrequencyInMillihertz / 1000);
        }
    }
}
