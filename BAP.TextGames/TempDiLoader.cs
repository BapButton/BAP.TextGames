using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BAP.Helpers;

namespace BAP.TextGames
{
    internal class TempDiLoader : IDependencyInjectionSetup
    {
        public void AddItemsToDi(IServiceCollection services)
        {
            services.AddTransient<AnimationController>();
        }
    }
}