using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Utilities;
using MudX.Models;

namespace MudX
{
    public partial class MudXCodeDisplay : MudComponentBase
    {

        protected string Classname =>
            new CssBuilder("mudx-code-display")
                .AddClass(Class, !string.IsNullOrWhiteSpace(Class))
                .Build();

        protected string Stylename =>
            new StyleBuilder()
                .AddStyle(Style, !string.IsNullOrWhiteSpace(Style))
                .Build();

        /// <summary>
        /// An IEnumerable of CodeFiles. Each CodeFile has a Name and the code.
        /// </summary>
        /// <remarks>At least one CodeFile is required, otherwise an exception is thrown.</remarks>
        [Parameter, EditorRequired]
        public required IEnumerable<CodeFile> Codes { get; set; }

        /// <summary>
        /// The current active CodeFile
        /// </summary>
        public CodeFile CodeFile { get; private set; } = default!;

        protected override void OnParametersSet()
        {
            if (!Codes.Any())
                throw new Exception("MudXCodeDisplay must have at least one CodeFile");

            CodeFile = Codes.First();
            base.OnParametersSet();
        }
    }
}
