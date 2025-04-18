namespace MudX.Components.MudXOutline
{
    internal interface IOutlineContainer
    {
        int Level { get; }
        Task RegisterSectionAsync(MudXOutlineSection section);
    }
}
