using System.Windows;
using TimeLiner_Assistant.ViewModels;

namespace TimeLiner_Assistant.Views
{
    public partial class TimeLinerView : Window
    {
        public TimeLinerView()
        {
            InitializeComponent();
            DataContext = new TimeLinerViewModel();
        }
    }
}
