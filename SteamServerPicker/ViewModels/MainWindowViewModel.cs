using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using SteamServerPicker.Models;
using SteamServerPicker.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SteamServerPicker.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly RouteService _routeService;

        public ObservableCollection<RouteViewModel> Servers { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel()
        {
            _routeService = new RouteService();
            Servers = new ObservableCollection<RouteViewModel>();

            UpdateServers();
        }

        // Simple Version
        /*private void UpdateServers()
        {
            List<Route> routes = _routeService.PopulateRoutes();

            Servers.Clear();

            int i = 1;

            foreach (Route route in routes)
            {
                foreach (KeyValuePair<string, string> range in route.Ranges!)
                {
                    string name = $"{route.Description!} {i}";
                    long ping = _routeService.PingHost(range.Key);
                    bool isBanned = _routeService.IsServerBanned(name);

                    //Servers.Add(new RouteViewModel($"{route.Description!} {i}", range.Key, range.Value, _routeService.PingHostAsync(range.Key).ToString(), false));
                    Servers.Add(new RouteViewModel(name, range.Key, range.Value, ping.ToString(), isBanned));
                    i++;
                }
                i = 1;
            }
        }*/

        /// <summary>
        /// Updates the servers.
        /// </summary>
        private void UpdateServers()
        {
            List<Route> routes = _routeService.PopulateRoutes();

            List<RouteViewModel> servers = new List<RouteViewModel>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            Parallel.ForEach(routes, parallelOptions, route =>
            {
                int i = 1;

                foreach (KeyValuePair<string, string> range in route.Ranges!)
                {
                    string name = $"{route.Description!} {i}";
                    long ping = _routeService.PingHost(range.Key);
                    bool isBanned = _routeService.IsServerBanned(name);

                    // thread-safe
                    lock (servers)
                    {
                        servers.Add(new RouteViewModel(name, range.Key, range.Value, ping.ToString(), isBanned));
                    }

                    i++;
                }
            });

            Dispatcher.UIThread.Invoke(() =>
            {
                Servers.Clear();
                foreach (RouteViewModel server in servers.OrderBy(i => i.Name))
                {
                    Servers.Add(server);
                }

                //Servers = new ObservableCollection<RouteViewModel>(servers.OrderBy(i => i.Name));
            });
        }

        /// <summary>
        /// Updates the servers asynchronous.
        /// </summary>
        [RelayCommand]
        private async Task UpdateServersAsync()
        {
            await Task.Run(() =>
            {
                UpdateServers();
            });
        }

        /// <summary>
        /// Toggles all is blocked asynchronous.
        /// </summary>
        [RelayCommand]
        private async Task ToggleAllIsBlockedAsync()
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            await Task.Run(() =>
            {
                Parallel.ForEach(Servers, parallelOptions, server =>
                {
                    if (server.IsEnable)
                        server.IsEnable = false;
                    else
                        server.IsEnable = true;
                });
            });
        }

        /// <summary>
        /// Clears the rules asynchronous.
        /// </summary>
        [RelayCommand]
        private async Task ClearRulesAsync()
        {
            await Task.Run(() =>
            {
                _routeService.ClearAllRules();

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                Parallel.ForEach(Servers, parallelOptions, server =>
                {
                    server.IsEnable = false;
                });

            });
        }

        /// <summary>
        /// Pings all servers asynchronous.
        /// </summary>
        [RelayCommand]
        private async Task PingAllServersAsync()
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            await Task.Run(() =>
            {
                Parallel.ForEach(Servers, parallelOptions, async route =>
                {
                    long ping = await _routeService.PingHostAsync(route.IP!);
                    route.Ping = ping.ToString();
                });
            });
        }
    }
}
