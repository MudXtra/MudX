using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MudX
{
    public partial class MudXSecurityCode : MudComponentBase
    {
        /// <summary>
        /// The gap between items in increments of 4px. 
        /// </summary>
        /// <remarks>Defaults to 3 in <see cref="MudX.MudGlobal.StackDefaults"/></remarks>
        [Parameter]
        public int Spacing { get; set; } = MudGlobal.StackDefaults.Spacing;

        /// <summary>
        /// Displays items horizontally, if true will display items horizontally.
        /// </summary>
        /// <remarks>Defaults to <langword="false" /></remarks>
        [Parameter]
        public bool Row { get; set; } = false;

        /// <summary>
        /// <para>The display variant of the component. Options are Outlined, Text, and Filled.</para>
        /// <para>Setting this value will overried the value of all nested <see cref="MudX.MudXCodeItem"/></para>
        /// </summary>
        /// <remarks>Defaults to <langword="null" /></remarks>
        [Parameter]
        public Variant? Variant { get; set; }

        /// <summary>
        /// The content of the component. If supplied this will override the default content and allow you to create it customized.
        /// </summary>
        /// <remarks>Defaults to <langword="null" /></remarks>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }
    }
}
