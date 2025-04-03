using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;

namespace WebApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
    {
        private readonly TcpListener _listener;
        private bool _isListening = false;
        private string _notification;
        private int _activePlayers;
        private int _biggestMultiplier;
        private static readonly Random _random = new Random();

        public ObservableCollection<int> LastResults { get; } = new ObservableCollection<int>();
        public ObservableCollection<string> Multipliers { get; } = new ObservableCollection<string>();

        public string Notification
        {
            get => _notification;
            set { _notification = value; OnPropertyChanged(nameof(Notification)); }
        }

        public int ActivePlayers
        {
            get => _activePlayers;
            set { _activePlayers = value; OnPropertyChanged(nameof(ActivePlayers)); }
        }

        public int BiggestMultiplier
        {
            get => _biggestMultiplier;
            set { _biggestMultiplier = value; OnPropertyChanged(nameof(BiggestMultiplier)); }
        }

        public ICommand AddRandomResultCommand { get; }
        public ICommand ShowNotificationCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            _listener = new TcpListener(IPAddress.Any, 4948);
            AddRandomResultCommand = new RelayCommand(AddRandomResult);
            ShowNotificationCommand = new RelayCommand(ShowNotification);
            StartListening();
        }

        private void AddRandomResult()
        {
            int result = _random.Next(0, 37);
            int streak = _random.Next(1, 10);
            int multiplier = result * streak;

            if (LastResults.Count >= 10) LastResults.RemoveAt(0);
            LastResults.Add(result);

            if (Multipliers.Count >= 10) Multipliers.RemoveAt(0);
            Multipliers.Add($"x{multiplier}");
        }

        private void ShowNotification()
        {
            Notification = "Player VIP PlayerName has joined the table.";
            Task.Delay(5000).ContinueWith(_ => Dispatcher.UIThread.InvokeAsync(() => Notification = ""));
        }

        private async Task StartListening()
        {
            if (_isListening)
            {
                Console.WriteLine("Listener is already running.");
                return;
            }

            _isListening = true;
            _listener.Start();
            Console.WriteLine("Listening on port 4948...");
            await AcceptClientsAsync();
        }
        
        private async Task AcceptClientsAsync()
        {
            while (_isListening)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClient(client));
            }
        }

        private async void HandleClient(TcpClient client)
        {
            using var stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            try
            {
                var data = JsonSerializer.Deserialize<ServerMessage>(message);
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ActivePlayers = data.ActivePlayers;
                    BiggestMultiplier = data.BiggestMultiplier;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing JSON: " + ex.Message);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ServerMessage
    {
        public int ActivePlayers { get; set; }
        public int BiggestMultiplier { get; set; }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }