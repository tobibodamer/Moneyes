using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Moneyes.UI.Blazor;

/// <summary>
/// A component that renders an anchor tag, automatically toggling its 'active'
/// class based on whether its 'href' matches the current URI.
/// </summary>
public class MudNavTabs : MudBlazor.MudTabs
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // We'll consider re-rendering on each location change
        NavigationManager.LocationChanged += OnLocationChanged;
    }


    /// <inheritdoc />
    public void Dispose()
    {
        // To avoid leaking memory, it's important to detach any event handlers in Dispose()
        NavigationManager.LocationChanged -= OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        // We could just re-render always, but for this component we know the
        // only relevant state change is to the _isActive property.        
        for (int index = 0; index < Panels.Count; index++)
        {
            if (ActivePanelIndex == index)
            {
                continue;
            }

            if (Panels[index] is MudNavTab navPanel && navPanel.ShouldMatch(args.Location))
            {
                ActivatePanel(index);
            }
        }
    }
}