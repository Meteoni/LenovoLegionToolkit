using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Windows.Settings;
using LenovoLegionToolkit.WPF.Extensions;
using LenovoLegionToolkit.WPF.Resources;

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
        var dynamicRate = refreshRates.LastOrDefault(rate => rate.IsDynamic);
        var physicalFrequencies = refreshRates.Where(rate => !rate.IsDynamic && rate.Frequency > 1)
            .Select(rate => rate.Frequency).OrderBy(frequency => frequency).ToArray();
        var highFrequency = _settings.Store.DynamicRefreshRateHighFrequency;
        if (!physicalFrequencies.Contains(highFrequency))
            highFrequency = dynamicRate.Frequency;

        _dynamicRefreshRateHighComboBox.SetItems(physicalFrequencies, highFrequency, frequency => $"{frequency} Hz");
        _dynamicRefreshRateLowTextBox.Text = (_settings.Store.DynamicRefreshRateLowFrequency > 0
            ? _settings.Store.DynamicRefreshRateLowFrequency
            : dynamicRate.BaseFrequency).ToString();
        _dynamicRefreshRateHeader.Warning = string.Empty;
        _dynamicRefreshRateCard.Visibility = await _refreshRateFeature.IsDynamicSupportedAsync() ? Visibility.Visible : Visibility.Collapsed;

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

    private void BootLogo_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var window = new BootLogoWindow { Owner = Window.GetWindow(this) };
        window.ShowDialog();
    }

    private void SaveDynamicRefreshRate_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (!int.TryParse(_dynamicRefreshRateLowTextBox.Text, out var lowFrequency) ||
            !_dynamicRefreshRateHighComboBox.TryGetSelectedItem(out int highFrequency) ||
            lowFrequency <= 0 || lowFrequency >= highFrequency)
        {
            _dynamicRefreshRateHeader.Warning = Resource.SettingsPage_DynamicRefreshRate_InvalidRange;
            return;
        }

        _settings.Store.DynamicRefreshRateLowFrequency = lowFrequency;
        _settings.Store.DynamicRefreshRateHighFrequency = highFrequency;
        _settings.SynchronizeStore();
        _dynamicRefreshRateHeader.Warning = string.Empty;
    }

    private async void ResetDynamicRefreshRate_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        _settings.Store.DynamicRefreshRateLowFrequency = 0;
        _settings.Store.DynamicRefreshRateHighFrequency = 0;
        _settings.SynchronizeStore();
        await RefreshAsync();
    }
}
