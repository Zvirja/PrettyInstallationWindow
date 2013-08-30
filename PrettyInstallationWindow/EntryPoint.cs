using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APODirtyPlugin;
using SIM.Tool.Base.Plugins;

namespace PrettyInstallationWindow
{
    class EntryPoint : IInitProcessor
    {
        void IInitProcessor.Process()
        {
            InstanceDetailsInjector.Inject();
        }
    }
}
