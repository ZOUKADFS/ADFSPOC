using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using POC_ADFS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace POC_ADFS.App_Start
{
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {

        public ApplicationUserManager(UserStore<ApplicationUser> userStore)
            : base(userStore)
        {
        }
        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context)
        {
            var manager = new ApplicationUserManager(new UserStore<ApplicationUser>(context.Get<ApplicationDbContext>()));
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 10,
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = false,
                RequireUppercase = false,
            };
            return manager;
        }

        
    }
}