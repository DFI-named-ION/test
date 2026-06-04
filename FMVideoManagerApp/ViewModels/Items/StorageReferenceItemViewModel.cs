using FMVideoManagerApp.Data.DTO;

namespace FMVideoManagerApp.ViewModels.Items
{
    public sealed class StorageReferenceItemViewModel
    {
        public long Id { get; }

        public CloudProviderType Provider { get; }

        public string ProviderText => Provider.ToString();

        public string? ProviderPath { get; }

        public string Name { get; }

        public StorageReferenceState State { get; }

        public string StateText => State switch
        {
            StorageReferenceState.Active => "Active",
            StorageReferenceState.Missing => "Missing",
            StorageReferenceState.Deleted => "Deleted",
            StorageReferenceState.SyncError => "Sync error",
            _ => "Unknown"
        };

        public string? AccountDisplayName { get; }

        public string? AccountEmail { get; }

        public string AccountText
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(AccountEmail))
                    return AccountEmail;

                if (!string.IsNullOrWhiteSpace(AccountDisplayName))
                    return AccountDisplayName;

                return "Unknown account";
            }
        }

        public DateTime LastSeenAtUtc { get; }

        public string LocationText => string.IsNullOrWhiteSpace(ProviderPath) ? Name : ProviderPath;

        public StorageReferenceItemViewModel(StorageReferenceDto dto)
        {
            Id = dto.Id;
            Provider = dto.Provider;
            ProviderPath = dto.ProviderPath;
            Name = dto.Name;
            State = dto.State;
            AccountDisplayName = dto.AccountDisplayName;
            AccountEmail = dto.AccountEmail;
            LastSeenAtUtc = dto.LastSeenAtUtc;
        }
    }
}