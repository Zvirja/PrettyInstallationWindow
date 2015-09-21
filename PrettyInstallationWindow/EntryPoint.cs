#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APODirtyPlugin;
using SIM.Tool.Base.Plugins;

#endregion

namespace PrettyInstallationWindow
{
  internal class EntryPoint : IInitProcessor
  {
    #region Interface Impl

    void IInitProcessor.Process()
    {
      InstanceDetailsInjector.Inject();
      PluginProperties.Init();
    }

    #endregion
  }
}