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
            // SECURITY: Check authentication before rendering any protected content
            try
            {
                if (!AuthService.IsAuthenticated())
                {
                    Navigation.NavigateTo("/login", forceLoad: true);
                    return;
                }
            }
            catch (Exception)
            {
                // If authentication check fails, redirect to login
                Navigation.NavigateTo("/login", forceLoad: true);
                return;
            }

            base.OnInitialized();
        }
    }
}

