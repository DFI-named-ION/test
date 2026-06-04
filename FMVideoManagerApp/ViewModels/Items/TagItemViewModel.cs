using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.DTO.Tags;

namespace FMVideoManagerApp.ViewModels.Items
{
    public sealed class TagItemViewModel : ObservableObject
    {
        public long Id { get; }

        public string Name { get; }

        public string? Description { get; }

        public string? BackgroundColorHex { get; }

        public string? ForegroundColorHex { get; }

        public TagItemViewModel(TagDto dto)
        {
            Id = dto.Id;
            Name = dto.Name;
            Description = dto.Description;
            BackgroundColorHex = dto.BackgroundColorHex;
            ForegroundColorHex = dto.ForegroundColorHex;
        }
    }
}