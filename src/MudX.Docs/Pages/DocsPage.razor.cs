using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using MudX.Docs.Data;

namespace MudX.Docs
{
    public partial class DocsPage
    {
        [Inject]
        private NavigationManager Navigation { get; set; } = default!;

        [Inject]
        private HttpClient Http { get; set; } = default!;

        [Parameter, EditorRequired]
        public required RenderFragment ChildContent { get; set; }

        [Parameter, EditorRequired]
        public required string ApiJson { get; set; }

        protected DocumentedTypeModel? DocumentedType { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (DocumentedType is null && !string.IsNullOrWhiteSpace(ApiJson))
            {
                try
                {
                    var baseUri = Navigation.BaseUri;
                    var url = $"{baseUri}_content/MudX.Docs/api/{ApiJson}";
                    DocumentedType = await Http.GetFromJsonAsync<DocumentedTypeModel>(url);
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to load API JSON: {ex.Message}");
                }
            }
        }
    }
}
