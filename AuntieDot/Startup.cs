using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(AuntieDot.Startup))]
namespace AuntieDot
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
