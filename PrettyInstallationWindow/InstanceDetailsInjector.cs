#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SIM.Tool.Windows.UserControls.Install;
using SIM.Tool.Wizards;

#endregion

namespace APODirtyPlugin
{
  internal class InstanceDetailsInjector
  {
    #region Fields

    private static Dictionary<string, WizardPipeline> _definitionsCached;

    #endregion

    #region Properties

    public static Dictionary<string, WizardPipeline> Definitions
    {
      get
      {
        if (_definitionsCached != null)
        {
          return _definitionsCached;
        }
        var fld = typeof(WizardPipelineManager).GetField("Definitions",
          BindingFlags.Static | BindingFlags.NonPublic |
          BindingFlags.GetField);
        _definitionsCached = fld.GetValue(null) as Dictionary<string, WizardPipeline>;
        return _definitionsCached;
      }
    }

    #endregion

    #region Methods

    public static void Inject()
    {
      foreach (var stepInfo in Definitions["install"].StepInfos)
      {
        if (stepInfo.Control.ToString().Contains("InstanceDetails"))
        {
          SetControlValue(stepInfo, typeof(InstanceDetailsExtended));
          break;
        }
      }
    }

    public static void SetControlValue(StepInfo step, Type newValue)
    {
      var fld = typeof(StepInfo).GetField("Control", BindingFlags.Instance | BindingFlags.SetField | BindingFlags.Public);
      fld.SetValue(step, newValue);
    }

    #endregion
  }
}