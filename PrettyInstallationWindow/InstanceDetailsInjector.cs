using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SIM.Tool;
using SIM.Tool.Windows.UserControls.Install;
using SIM.Tool.Wizards;

namespace APODirtyPlugin
{
  class InstanceDetailsInjector
  {
    private static Dictionary<string, WizardPipeline> _definitionsCached;

    public static Dictionary<string, WizardPipeline> Definitions
    {
      get
      {
        if (_definitionsCached != null)
          return _definitionsCached;
        var fld = typeof(WizardPipelineManager).GetField("Definitions",
                                                          BindingFlags.Static | BindingFlags.NonPublic |
                                                          BindingFlags.GetField);
        _definitionsCached = fld.GetValue(null) as Dictionary<string, WizardPipeline>;
        return _definitionsCached;
      }
    }

    public static void SetControlValue(StepInfo step, Type newValue)
    {
      var fld = typeof(StepInfo).GetField("Control", BindingFlags.Instance | BindingFlags.SetField | BindingFlags.Public);
      fld.SetValue(step, newValue);
    }

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
  }
}
