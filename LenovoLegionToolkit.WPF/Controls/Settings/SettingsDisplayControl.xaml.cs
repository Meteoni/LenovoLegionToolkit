using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Extensions;
using LenovoLegionToolkit.WPF.Windows.Settings;

namespace LenovoLegionToolkit.WPF.Controls.Settings;

public partial class SettingsDisplayControl
{
    private readonly ApplicationSettings _settings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly RefreshRateFeature _refreshRateFeature = IoCContainer.Resolve<RefreshRateFeature>();

    private bool _isRefreshing;

    public SettingsDisplayControl()
    {
        InitializeComponent();
    }

    public async Task RefreshAsync()
    {
        _isRefreshing = true;

        _synchronizeBrightnessToAllPowerPlansToggle.IsChecked = _settings.Store.SynchronizeBrightnessToAllPowerPlans;
        _bootLogoCard.Visibility = await BootLogo.IsSupportedAsync() ? Visibility.Visible : Visibility.Collapsed;

        var refreshRates = await _refreshRateFeature.GetAllStatesAsync();
        var fixedFrequencies = refreshRates
            .Where(r => !r.IsDynamic)
            .Select(r => r.Frequency)
            .Distinct()
            .OrderBy(f => f)
            .ToArray();
        var dynamicRefreshRate = refreshRates.FirstOrDefault(r => r.IsDynamic);

        if (dynamicRefreshRate.IsDynamic && fixedFrequencies.Length >= 2)
        {
            var lowFrequency = dynamicRefreshRate.BaseFrequency;
            var highFrequency = dynamicRefreshRate.Frequency;

            _dynamicRefreshRateLowComboBox.SetItems(
                fixedFrequencies.Where(f => f < highFrequency).ToArray(),
                lowFrequency,
                f => $"{f} Hz");
            _dynamicRefreshRateHighComboBox.SetItems(
                fixedFrequencies.Where(f => f > lowFrequency).ToArray(),
                highFrequency,
                f => $"{f} Hz");

            _dynamicRefreshRateCard.Visibility = Visibility.Visible;
            _dynamicRefreshRateLowComboBox.Visibility = Visibility.Visible;
            _dynamicRefreshRateHighComboBox.Visibility = Visibility.Visible;
        }
        else
        {
            _dynamicRefreshRateCard.Visibility = Visibility.Collapsed;
        }

        _synchronizeBrightnessToAllPowerPlansToggle.Visibility = Visibility.Visible;

        _isRefreshing = false;
    }

    private void SynchronizeBrightnessToAllPowerPlansToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var state = _synchronizeBrightnessToAllPowerPlansToggle.IsChecked;
        if (state is null)
            return;

        _settings.Store.SynchronizeBrightnessToAllPowerPlans = state.Value;
        _settings.SynchronizeStore();
    }

    private async void DynamicRefreshRateLowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (!_dynamicRefreshRateLowComboBox.TryGetSelectedItem(out int lowFrequency) ||
            !_dynamicRefreshRateHighComboBox.TryGetSelectedItem(out int highFrequency))
            return;

        await SaveDynamicRefreshRateRangeAsync(lowFrequency, highFrequency);
    }

    private async void DynamicRefreshRateHighComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (!_dynamicRefreshRateLowComboBox.TryGetSelectedItem(out int lowFrequency) ||
            !_dynamicRefreshRateHighComboBox.TryGetSelectedItem(out int highFrequency))
            return;

        await SaveDynamicRefreshRateRangeAsync(lowFrequency, highFrequency);
    }

    private async Task SaveDynamicRefreshRateRangeAsync(int lowFrequency, int highFrequency)
    {
        if (lowFrequency <= 0 || highFrequency <= lowFrequency)
            return;

        _isRefreshing = true;
        try
        {
            _settings.Store.DynamicRefreshRateLowFrequency = lowFrequency;
            _settings.Store.DynamicRefreshRateHighFrequency = highFrequency;
            _settings.SynchronizeStore();

            var currentState = await _refreshRateFeature.GetStateAsync();
            if (currentState.IsDynamic)
            {
                await _refreshRateFeature.SetStateAsync(new RefreshRate(
                    highFrequency,
                    isDynamic: true,
                    baseFrequency: lowFrequency));
            }
        }
        finally
        {
            _isRefreshing = false;
        }

        await RefreshAsync();
    }

    private void BootLogo_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var window = new BootLogoWindow { Owner = Window.GetWindow(this) };
        window.ShowDialog();
    }
}
