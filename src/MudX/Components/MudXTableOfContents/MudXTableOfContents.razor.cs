using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.State;
using MudX.Components.MudXTableOfContents;

namespace MudX
{
    public partial class MudXTableOfContents : MudComponentBase
    {
        private bool _contentDrawerOpen = true;
        private bool _displayView = false;
        private readonly string _tocId = Guid.NewGuid().ToString();

        private readonly ParameterState<bool> _contentDrawerOpenState;

        private MudPageContentNavigation? _contentNavigation;
        private Queue<MudXSectionLink> _bufferedSections = new();
        private Dictionary<MudXSectionLink, MudXSection> _sections = new Dictionary<MudXSectionLink, MudXSection>();

        public MudXTableOfContents()
        {
            using var registerScope = CreateRegisterScope();
            _contentDrawerOpenState = registerScope.RegisterParameter<bool>(nameof(ContentDrawerOpen))
                .WithParameter(() => ContentDrawerOpen)
                .WithChangeHandler(HandleContentDrawerOpenChanged);
        }

        [Inject] NavigationManager NavigationManager { get; set; } = default!;

        /// <summary>
        /// Whether the Table of Contents drawer is open or closed
        /// </summary>
        /// <remarks>Defaults to <see langword="true"/></remarks>
        [Parameter]
        public bool ContentDrawerOpen { get; set; } = true;

        /// <summary>
        /// The Headline of the Table of Contents, this will display at the top of the MudDrawer if there is at least one section.
        /// </summary>
        [Parameter]
        public string? Headline { get; set; }

        /// <summary>
        /// Whether the scroll to top button is visible
        /// </summary>
        /// <remarks>Defaults to <see langword="true"/></remarks>
        [Parameter]
        public bool ScrollToTopVisible { get; set; } = true;

        /// <summary>
        /// The content of the Table of Contents Document
        /// </summary>
        [Parameter, EditorRequired]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Where you want the Table of Contents to be rendered
        /// </summary>
        /// <remarks>Defaults to <see cref="Anchor.End"/></remarks>
        [Parameter]
        public Anchor Anchor { get; set; } = Anchor.End;


        /// <summary>
        /// The Breakpoint at which the Table of Contents should be hidden
        /// </summary>
        /// <remarks>Defaults to <see cref="Breakpoint.Lg"/></remarks>
        [Parameter]
        public Breakpoint Breakpoint { get; set; } = Breakpoint.Lg;

        /// <summary>
        /// The Clip Mode for the Table of Contents
        /// </summary>
        /// <remarks>Defaults to <see cref="DrawerClipMode.Always"/></remarks>
        [Parameter]
        public DrawerClipMode DrawerClipMode { get; set; } = DrawerClipMode.Always;

        /// <summary>
        /// The Header for the Table of Contents Items
        /// </summary>
        [Parameter]
        public RenderFragment? HeaderContent { get; set; }

        /// <summary>
        /// The Footer for the Table of Contents Items
        /// </summary>
        [Parameter]
        public RenderFragment? FooterContent { get; set; }

        private void HandleContentDrawerOpenChanged(ParameterChangedEventArgs<bool> args)
        {
            _contentDrawerOpen = args.Value;
        }

        /// <summary>
        /// Adds a section to the Table of Contents
        /// </summary>
        public async Task AddSectionAsync(MudXSectionLink link, MudXSection section)
        {
            await Task.CompletedTask;
        }
    }
}
