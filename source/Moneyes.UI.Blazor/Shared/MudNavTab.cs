using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;

namespace Moneyes.UI.Blazor;

public class MudNavTab : MudBlazor.MudTabPanel
{
    private string? _hrefAbsolute;

    [Inject]
    NavigationManager NavigationManager { get; set; }

    /// <summary>
    /// Gets or sets a value representing the URL matching behavior.
    /// </summary>
    [Parameter]
    public NavLinkMatch Match { get; set; }

    /// <summary>
    /// Raised when tab is clicked
    /// </summary>
    //[Parameter] public new EventCallback<MouseEventArgs> OnClick { get; set; }

    public MudNavTab()
    {
        // Hook into MudTabPanels OnClicked callback
        base.OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, OnClicked);
    }

    private async Task OnClicked(MouseEventArgs e)
    {
        // Navigate to clicked link if not already matching

        if (!ShouldMatch())
        {
            NavigationManager.NavigateTo(_hrefAbsolute);
        }

        // Forward to our callback
        //await OnClick.InvokeAsync(e);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Update computed state
        var href = (string?)null;
        if (UserAttributes != null && UserAttributes.TryGetValue("href", out var obj))
        {
            href = Convert.ToString(obj, CultureInfo.InvariantCulture);
        }

        _hrefAbsolute = href == null ? null : NavigationManager.ToAbsoluteUri(href).AbsoluteUri;
    }

    public bool ShouldMatch(string? currentUriAbsolute = null)
    {
        currentUriAbsolute ??= NavigationManager.Uri;

        if (_hrefAbsolute == null)
        {
            return false;
        }

        if (EqualsHrefExactlyOrIfTrailingSlashAdded(currentUriAbsolute))
        {
            return true;
        }

        if (Match == NavLinkMatch.Prefix
            && IsStrictlyPrefixWithSeparator(currentUriAbsolute, _hrefAbsolute))
        {
            return true;
        }

        return false;
    }

    private bool EqualsHrefExactlyOrIfTrailingSlashAdded(string currentUriAbsolute)
    {
        Debug.Assert(_hrefAbsolute != null);

        if (string.Equals(currentUriAbsolute, _hrefAbsolute, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (currentUriAbsolute.Length == _hrefAbsolute.Length - 1)
        {
            // Special case: highlight links to http://host/path/ even if you're
            // at http://host/path (with no trailing slash)
            //
            // This is because the router accepts an absolute URI value of "same
            // as base URI but without trailing slash" as equivalent to "base URI",
            // which in turn is because it's common for servers to return the same page
            // for http://host/vdir as they do for host://host/vdir/ as it's no
            // good to display a blank page in that case.
            if (_hrefAbsolute[_hrefAbsolute.Length - 1] == '/'
                && _hrefAbsolute.StartsWith(currentUriAbsolute, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsStrictlyPrefixWithSeparator(string value, string prefix)
    {
        var prefixLength = prefix.Length;
        if (value.Length > prefixLength)
        {
            return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && (
                    // Only match when there's a separator character either at the end of the
                    // prefix or right after it.
                    // Example: "/abc" is treated as a prefix of "/abc/def" but not "/abcdef"
                    // Example: "/abc/" is treated as a prefix of "/abc/def" but not "/abcdef"
                    prefixLength == 0
                    || !char.IsLetterOrDigit(prefix[prefixLength - 1])
                    || !char.IsLetterOrDigit(value[prefixLength])
                );
        }
        else
        {
            return false;
        }
    }
}