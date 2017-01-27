using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(POC_ADFS.Web.Startup))]

namespace POC_ADFS.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
           ConfigureAuth(app);
        }
    }
}