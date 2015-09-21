#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIM;

#endregion

namespace PrettyInstallationWindow
{
  internal static class PluginProperties
  {
    #region Fields

    public static readonly AdvancedProperty<int> DialogHeightDelta = AdvancedSettings.Create("App/Plugins/PrettyInstallationWindow/Install/HeightDelta", 150);

    #endregion

    #region Constructors

    static PluginProperties()
    {
    }

    #endregion

    #region Methods

    public static void Init()
    {
      //Empty, just to trigger static ctor.
    }

    #endregion
  }
}