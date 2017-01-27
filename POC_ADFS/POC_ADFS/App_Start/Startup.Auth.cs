namespace POC_ADFS.Web
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    // The following using statements were added for this sample.
    using Owin;
    using Microsoft.Owin.Security;
    using Microsoft.Owin.Security.Cookies;
    using Microsoft.Owin.Security.WsFederation;
    using System.Configuration;
    using System.Globalization;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.IdentityModel.Tokens;
    using System.Security.Claims;
    using Microsoft.Owin;
    using Castle.Windsor;
    //using POC_ADFS.Web.Plumbing;
    //using POC_ADFS.Business.Services.Contracts;
    //using POC_ADFS.Business.Services.Implementation;

    using Microsoft.AspNet.Identity.Owin;
    using System.Web.Mvc;
    using Microsoft.Owin.Infrastructure;
    using POC_ADFS.Models;
    using POC_ADFS.App_Start;

    public partial class Startup
    {
        //
        // The Client ID is used by the application to uniquely identify itself to Azure AD.
        // The Metadata Address is used by the application to retrieve the signing keys used by Azure AD.
        // The AAD Instance is the instance of Azure, for example public Azure or Azure China.
        // The Authority is the sign-in URL of the tenant.
        // The Post Logout Redirect Uri is the URL where the user will be redirected after they sign out.
        //

        public void ConfigureAuth(IAppBuilder app)
        {

            app.CreatePerOwinContext<ApplicationDbContext>(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);

            

            //app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);
            //app.CreatePerOwinContext(() => DependencyResolver.Current.GetService<ApplicationUserManager>());
         //HttpContext.GetOwinContext().Get<ApplicationSignInManager>()

            //app.CreatePerOwinContext<ApplicationUserManager>((options, owinContext) =>
            //{
            //    ApplicationUserManager applicationUserManager = DependencyResolver.Current.GetService<ApplicationUserManager>();
            //    if (options.DataProtectionProvider != null)
            //    {
            //        applicationUserManager.UserTokenProvider = new DataProtectorTokenProvider<ApplicationUser>(options.DataProtectionProvider.Create("ASP.NET Identity"));
            //    }

            //    return applicationUserManager;
            //});

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                LoginPath = new PathString("/Login/Login"),
                Provider = new CookieAuthenticationProvider
                {
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                                        validateInterval: TimeSpan.FromDays(14), // 14 days
                                        regenerateIdentity: (manager, applicationUser) => manager.CreateIdentityAsync(applicationUser, app.GetDefaultSignInAsAuthenticationType()))
                },
                CookieManager = new SystemWebCookieManager()
            });


            bool adfsEnabled;
            if (bool.TryParse(ConfigurationManager.AppSettings["AdfsEnabled"], out adfsEnabled) && adfsEnabled)
            {


                var wsFederationAuthenticationOptions = new WsFederationAuthenticationOptions
                {
                    MetadataAddress = ConfigurationManager.AppSettings["AdfsMetadataAddress"],
                    Wtrealm = ConfigurationManager.AppSettings["AdfsRealm"],
                    Wreply = ConfigurationManager.AppSettings["AdfsReply"],
                    TokenValidationParameters = new TokenValidationParameters
                    {
                    },
                    Notifications = new WsFederationAuthenticationNotifications()
                    {
                        RedirectToIdentityProvider = notification => Task.Run(() =>
                        {
                            // If the intercepted message is for a sign-in,
                            // set the authentication type as configured in Web.config
                            if (notification.ProtocolMessage.IsSignInMessage)
                            {
                                notification.ProtocolMessage.Wauth = ConfigurationManager.AppSettings["AdfsAuthType"] ?? string.Empty;
                            }
                        }),
                        AuthenticationFailed = context =>
                        {
                            context.HandleResponse();
                            context.Response.Redirect("/Error/ShowError?signIn=true&errorMessage=" + context.Exception.Message);
                            return Task.FromResult(0);
                        },
                        SecurityTokenValidated = notification => Task.Run(async () =>
                        {
                            var applicationUserManager = notification.OwinContext.GetUserManager<ApplicationUserManager>();
                            ApplicationUser user = await applicationUserManager.FindByNameAsync(notification.AuthenticationTicket.Identity.Name);
                            ClaimsIdentity identity = notification.AuthenticationTicket.Identity;
                            if (user != null)
                            {
                                ClaimsIdentity identity0 = await applicationUserManager.CreateIdentityAsync(user, app.GetDefaultSignInAsAuthenticationType());
                                if (identity0 != null)
                                {
                                    foreach (Claim claim in identity.Claims)
                                    {
                                        if (!identity0.Claims.Any(c => c.Type.Equals(claim.Type)))
                                        {
                                            identity0.AddClaim(claim);
                                        }
                                    }
                                    identity = identity0;
                                }
                            }


                            notification.OwinContext.Authentication.SignIn(identity);
                        })
                    }
                };
                app.UseWsFederationAuthentication(wsFederationAuthenticationOptions);
            }
        }
    }

    // This is a work-around for the "me first cookie conflict" between
    // ASP.NET and OWIN. Please see
    // http://katanaproject.codeplex.com/wikipage?title=System.Web%20response%20cookie%20integration%20issues&referringTitle=Documentation
    public class SystemWebCookieManager : ICookieManager
    {
        public string GetRequestCookie(IOwinContext context, string key)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var webContext = context.Get<HttpContextBase>(typeof(HttpContextBase).FullName);
            var cookie = webContext.Request.Cookies[key];
            return cookie == null ? null : cookie.Value;
        }

        public void AppendResponseCookie(IOwinContext context, string key, string value, CookieOptions options)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            var webContext = context.Get<HttpContextBase>(typeof(HttpContextBase).FullName);

            bool domainHasValue = !string.IsNullOrEmpty(options.Domain);
            bool pathHasValue = !string.IsNullOrEmpty(options.Path);
            bool expiresHasValue = options.Expires.HasValue;

            var cookie = new HttpCookie(key, value);
            if (domainHasValue)
            {
                cookie.Domain = options.Domain;
            }
            if (pathHasValue)
            {
                cookie.Path = options.Path;
            }
            if (expiresHasValue)
            {
                cookie.Expires = options.Expires.Value;
            }
            if (options.Secure)
            {
                cookie.Secure = true;
            }
            if (options.HttpOnly)
            {
                cookie.HttpOnly = true;
            }

            webContext.Response.AppendCookie(cookie);
        }

        public void DeleteCookie(IOwinContext context, string key, CookieOptions options)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            AppendResponseCookie(
                context,
                key,
                string.Empty,
                new CookieOptions
                {
                    Path = options.Path,
                    Domain = options.Domain,
                    Expires = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                });
        }
    }
}
