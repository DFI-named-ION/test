using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.DTO.Tags;
using FMVideoManagerApp.ViewModels.Items;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMVideoManagerApp.Services
{
    public sealed class TagService : ObservableObject
    {
        private readonly ApiClient _apiClient;
        private readonly AuthService _authService;

        public ObservableCollection<TagItemViewModel> Tags { get; } = new();

        public ObservableCollection<TagItemViewModel> SelectedNodeTags { get; } = new();

        public TagService(ApiClient apiClient, AuthService authService)
        {
            _apiClient = apiClient;
            _authService = authService;

            _authService.LoggedOut += Clear;
        }

        public async Task RefreshTagsAsync(CancellationToken cancellationToken = default)
        {
            if (!_authService.IsLoggedIn)
            {
                Clear();
                return;
            }

            List<TagDto> tags = await _apiClient.GetTagsAsync(cancellationToken);

            Tags.Clear();

            foreach (TagDto tag in tags)
            {
                Tags.Add(new TagItemViewModel(tag));
            }
        }

        public async Task RefreshNodeTagsAsync(long nodeId, CancellationToken cancellationToken = default)
        {
            if (!_authService.IsLoggedIn)
            {
                SelectedNodeTags.Clear();
                return;
            }

            List<TagDto> tags = await _apiClient.GetNodeTagsAsync(nodeId, cancellationToken);

            SelectedNodeTags.Clear();

            foreach (TagDto tag in tags)
            {
                SelectedNodeTags.Add(new TagItemViewModel(tag));
            }
        }

        public void ClearSelectedNodeTags()
        {
            SelectedNodeTags.Clear();
        }

        public async Task<TagItemViewModel> CreateTagAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Tag name is required.");

            TagDto created = await _apiClient.CreateTagAsync(
                new CreateTagRequest
                {
                    Name = name.Trim()
                },
                cancellationToken);

            var tagVm = new TagItemViewModel(created);

            Tags.Add(tagVm);

            return tagVm;
        }

        public async Task ApplyTagToNodeAsync(long nodeId, long tagId, CancellationToken cancellationToken = default)
        {
            await _apiClient.ApplyTagToNodeAsync(nodeId, tagId, cancellationToken);

            await RefreshNodeTagsAsync(nodeId, cancellationToken);
        }

        public async Task RemoveTagFromNodeAsync(long nodeId, long tagId, CancellationToken cancellationToken = default)
        {
            await _apiClient.RemoveTagFromNodeAsync(nodeId, tagId, cancellationToken);

            await RefreshNodeTagsAsync(nodeId, cancellationToken);
        }

        private void Clear()
        {
            Tags.Clear();
            SelectedNodeTags.Clear();
        }
    }
}