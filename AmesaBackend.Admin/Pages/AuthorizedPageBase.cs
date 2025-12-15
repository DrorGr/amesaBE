using AmesaBackend.Auth.Services;
using Microsoft.AspNetCore.Components;
using System;

namespace AmesaBackend.Admin.Pages
{
    /// <summary>
    /// Base class for pages that require authentication.
    /// Redirects to login page if user is not authenticated.
    /// </summary>
    public class AuthorizedPageBase : ComponentBase
    {
        [Inject]
        protected IAdminAuthService AuthService { get; set; } = null!;

        [Inject]
        protected NavigationManager Navigation { get; set; } = null!;

        protected override void OnInitialized()
        {
            // TEMPORARILY DISABLED: Route guard disabled for debugging
            // TODO: Re-enable authentication check after debugging login issues
            /*
            // SECURITY: Check authentication before rendering any protected content
            // CRITICAL: Use regular navigation (not forceLoad) to maintain SignalR connection
            // forceLoad: true creates new HTTP request, breaking authentication state
            try
            {
                if (!AuthService.IsAuthenticated())
                {
                    Navigation.NavigateTo("/login");
                    return;
                }
            }
            catch (Exception)
            {
                // If authentication check fails, redirect to login
                Navigation.NavigateTo("/login");
                return;
            }
            */

            base.OnInitialized();
        }
    }
}
