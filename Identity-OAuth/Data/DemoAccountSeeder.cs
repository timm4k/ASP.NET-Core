using IdentityOAuth.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace IdentityOAuth.Data;

internal static class DemoAccountSeeder
{
    public static async Task SeedDemoAccountsAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        UserManager<IdentityUser> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        DemoAccountsOptions options =
            scope.ServiceProvider.GetRequiredService<IOptions<DemoAccountsOptions>>().Value;

        foreach (DemoAccount account in options.Accounts)
        {
            if (await userManager.FindByEmailAsync(account.Email) is not null)
            {
                continue;
            }

            IdentityUser user = new()
            {
                UserName = account.Email,
                Email = account.Email,
                EmailConfirmed = true
            };

            IdentityResult result = await userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                string errors = string.Join(", ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException(
                    $"Demo account '{account.Email}' could not be created: {errors}");
            }
        }
    }
}
