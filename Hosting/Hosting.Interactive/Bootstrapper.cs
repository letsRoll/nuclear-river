using System;
using System.Collections.Generic;

using Nancy;
using Nancy.TinyIoc;

namespace NuClear.River.Hosting.Interactive
{
    internal sealed class Bootstrapper : DefaultNancyBootstrapper
    {
        private readonly INancyModuleCatalog _moduleCatalog;

        public Bootstrapper(INancyModule interactiveModule)
        {
            _moduleCatalog = new ModuleCatalog(interactiveModule);
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            container.Register(_moduleCatalog);
        }

        private sealed class ModuleCatalog : INancyModuleCatalog
        {
            private readonly INancyModule _interactiveModule;

            public ModuleCatalog(INancyModule interactiveModule)
            {
                _interactiveModule = interactiveModule;
            }

            public IEnumerable<INancyModule> GetAllModules(NancyContext context)
            {
                return new[] { _interactiveModule };
            }

            public INancyModule GetModule(Type moduleType, NancyContext context)
            {
                return moduleType == _interactiveModule.GetType() ? _interactiveModule : null;
            }
        }
    }
}