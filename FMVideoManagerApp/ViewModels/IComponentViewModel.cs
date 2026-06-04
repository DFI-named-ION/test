namespace FMVideoManagerApp.ViewModels
{
    public interface IComponentViewModel
    {
        Task OnActivatedAsync() => Task.CompletedTask;

        void OnDeactivated() { }
    }
}