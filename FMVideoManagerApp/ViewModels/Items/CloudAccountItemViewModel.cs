using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.DTO;

namespace FMVideoManagerApp.ViewModels.Items
{
    public sealed class CloudAccountItemViewModel : ObservableObject
    {
        public long Id { get; }

        private CloudProviderType _provider;
        public string Provider => _provider switch
        {
            CloudProviderType.Dropbox => "Dropbox",
            CloudProviderType.GoogleDrive => "GoogleDrive",
            _ => "Unknown"
        };

        public string ProviderAccountId { get; } = null!;

        public string? DisplayName { get; }

        public string? Email { get; }

        public string AccountText => $"{DisplayName}, {Email}";

        public string? Scopes { get; }

        public bool IsActive { get; }

        public bool IsInactive => !IsActive;

        public string IsActiveText => IsActive switch
        {
            true => "Active",
            false => "Inactive"
        };

        public DateTime CreatedAtUtc { get; }

        public DateTime UpdatedAtUtc { get; }

        public DateTime? LastSyncAtUtc { get; }

        public CloudAccountItemViewModel(CloudProviderAccountDto dto)
        {
            Id = dto.Id;
            _provider = dto.Provider;
            DisplayName = dto.DisplayName;
            Email = dto.Email;
            Scopes = dto.Scopes;
            IsActive = dto.IsActive;
            CreatedAtUtc = dto.CreatedAtUtc;
            UpdatedAtUtc = dto.UpdatedAtUtc;
            LastSyncAtUtc = dto.LastSyncAtUtc;
        }
    }
}