using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProClubesApp.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    private bool _isBusy;
    private string? _errorMessage;
    private string? _successMessage;
    private bool _isOffline;
    private string _syncBannerText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string? SuccessMessage
    {
        get => _successMessage;
        set { SetProperty(ref _successMessage, value); OnPropertyChanged(nameof(HasSuccess)); }
    }

    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    public bool IsOffline
    {
        get => _isOffline;
        set => SetProperty(ref _isOffline, value);
    }

    public string SyncBannerText
    {
        get => _syncBannerText;
        set => SetProperty(ref _syncBannerText, value);
    }

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
