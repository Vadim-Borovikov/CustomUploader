using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CustomUploader.Logic.Timepad.Data;

namespace CustomUploader
{
    /// <summary>
    /// Interaction logic for SelectionWindow.xaml
    /// </summary>
    internal partial class SelectionWindow
    {
        public int? SelectedId { get; private set; }

        public SelectionWindow(IEnumerable<Event> events)
        {
            InitializeComponent();

            foreach (Button button in events.OrderBy(e => e.StartsAt).Select(e => CreateButton(e.Name, e.StartsAt, e.Id)))
            {
                StackPanel.Children.Add(button);
            }
        }

        private Button CreateButton(string name, DateTime startsAt, int id)
        {
            var button = new Button
            {
                Name = $"b{id}",
                Content = $"{startsAt:dd.MM.yyyy}: {name}"
            };
            button.Click += Button_Click;
            return button;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            SelectedId = int.Parse(button.Name.Replace("b", ""));
            Close();
        }
    }
}
