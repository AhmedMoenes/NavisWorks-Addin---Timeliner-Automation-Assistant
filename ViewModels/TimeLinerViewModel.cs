using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Timeliner;
using ClosedXML.Excel;
using Microsoft.Win32;
using TimeLiner_Assistant.Models;

namespace TimeLiner_Assistant.ViewModels
{
    public class TimeLinerViewModel : INotifyPropertyChanged
    {
        private string _title = "TimeLiner Automation Assistant";

        public ObservableCollection<TimeLinerTaskModel> Tasks { get; } = new ObservableCollection<TimeLinerTaskModel>();
        public ObservableCollection<LinkRuleModel> LinkRules { get; } = new ObservableCollection<LinkRuleModel>();
        public ObservableCollection<LinkPreviewModel> LinkPreview { get; } = new ObservableCollection<LinkPreviewModel>();

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoadTasksCommand { get; }
        public ICommand RunAutomationCommand { get; }

        public TimeLinerViewModel()
        {
            LoadTasksCommand = new RelayCommand(OnLoadTasks);
            RunAutomationCommand = new RelayCommand(OnRunAutomation);

            // Example rules
            LinkRules.Add(new LinkRuleModel { Pattern = "Beam", TaskName = "Install Beams" });
            LinkRules.Add(new LinkRuleModel { Pattern = "Column", TaskName = "Install Columns" });
        }

        private void OnLoadTasks()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                Title = "Select Excel Task File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    LoadTasksFromExcel(openFileDialog.FileName);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }

        private void LoadTasksFromExcel(string filePath)
        {
            var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheets.First();
            Tasks.Clear();

            foreach (var row in worksheet.RowsUsed().Skip(1)) // Skip header
            {
                try
                {
                    Tasks.Add(new TimeLinerTaskModel
                    {
                        Name = row.Cell(1).GetValue<string>(),
                        StartDate = row.Cell(2).GetDateTime(),
                        EndDate = row.Cell(3).GetDateTime(),
                        Level = row.Cell(4).GetValue<string>(),
                        Type = row.Cell(5).GetValue<string>()
                    });
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }

        private void OnRunAutomation()
        {
            LinkPreview.Clear();

            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            var dTime = doc.GetTimeliner();
            var modelItems = doc.Models.SelectMany(m => m.RootItem.DescendantsAndSelf);

            foreach (var rule in LinkRules)
            {
                var matchingTask = Tasks.FirstOrDefault(t => t.Name.Equals(rule.TaskName, StringComparison.OrdinalIgnoreCase));
                if (matchingTask == null)
                    continue;

                var matchingElements = modelItems
                    .Where(item =>
                        item.DisplayName.IndexOf(rule.Pattern, StringComparison.OrdinalIgnoreCase) >= 0 &&
                        HasMatchingLevel(item, matchingTask.Level))
                    .ToList();

                var newTask = new TimelinerTask
                {
                    DisplayName = matchingTask.Name,
                    PlannedStartDate = matchingTask.StartDate,
                    PlannedEndDate = matchingTask.EndDate,
                    SimulationTaskTypeName = matchingTask.Type
                };

                var sourceItems = new ModelItemCollection();
                foreach (var item in matchingElements)
                {
                    sourceItems.Add(item);

                    LinkPreview.Add(new LinkPreviewModel
                    {
                        ElementName = item.DisplayName,
                        LinkedTask = matchingTask.Name
                    });
                }

                var selSet = new SelectionSet(sourceItems);
                var selSource = doc.SelectionSets.CreateSelectionSource(selSet);
                var selSourceCol = new SelectionSourceCollection { selSource };
                newTask.Selection.CopyFrom(selSourceCol);
                dTime.TaskAddCopy(newTask);
            }

            MessageBox.Show("Automation Complete! Preview updated.");
        }

        private bool HasMatchingLevel(ModelItem item, string expectedLevel)
        {
            var levelProp = item.PropertyCategories
                .SelectMany(cat => cat.Properties)
                .FirstOrDefault(p => p.DisplayName.Equals("Level", StringComparison.OrdinalIgnoreCase));

            return levelProp != null &&
                   levelProp.Value.IsDisplayString &&
                   levelProp.Value.ToDisplayString().Equals(expectedLevel, StringComparison.OrdinalIgnoreCase);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
        public void Execute(object parameter) => _execute();
    }
}

