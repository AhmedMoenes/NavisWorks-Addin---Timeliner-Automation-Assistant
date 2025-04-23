using System.Windows;
using Autodesk.Navisworks.Api.Plugins;
using TimeLiner_Assistant.Views;

namespace TimeLiner_Assistant
{
    [Plugin("TimeLinerAssistant", "AhmedMoenes", DisplayName = "TimeLiner Automation Assistant")]
    public class AppEntry: AddInPlugin
    {
        public override int Execute(params string[] parameters)
        {
            Window view = new TimeLinerView();
            view.Show();
            return 0;
        }
    }
}
