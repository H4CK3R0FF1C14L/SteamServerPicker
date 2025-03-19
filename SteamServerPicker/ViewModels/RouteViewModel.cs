using CommunityToolkit.Mvvm.ComponentModel;
using SteamServerPicker.Services;
using System.Diagnostics;

namespace SteamServerPicker.ViewModels
{
    public partial class RouteViewModel : ObservableObject
    {
        public string? Name { get; set; }
        public string? IP { get; set; }
        public string? Ports { get; set; }

        [ObservableProperty]
        private string? _ping;

        [ObservableProperty]
        private bool _isEnable;

        public RouteViewModel(string name, string ip, string ports, string ping, bool isEnable)
        {
            Name = name;
            IP = ip;
            Ports = ports;
            Ping = ping;
            IsEnable = isEnable;
        }


        // Ideally it should be in MainWindowViewModel, but I'm too lazy to subscribe to events there, so I decided to use RouteService in this class as well.
        /// <summary>
        /// Called when [is enable changed].
        /// </summary>
        /// <param name="oldValue">if set to <c>true</c> [old value].</param>
        /// <param name="newValue">if set to <c>true</c> [new value].</param>
        partial void OnIsEnableChanged(bool oldValue, bool newValue)
        {
            Debug.WriteLine($"IsEnable changed from {oldValue} to {newValue} for {Name}");

            RouteService routeService = new RouteService();

            if (newValue)
                routeService.SetRule(Name!, IP!, Ports!);
            else
                routeService.ClearRule(Name!);
        }
    }
}
