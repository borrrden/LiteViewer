// 
//  QueryWindow.xaml.cs
// 
//  Author:
//   Jim Borden  <jim.borden@couchbase.com>
// 
//  Copyright (c) 2017 Couchbase, Inc All rights reserved.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//  http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// 

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using LiteViewerCore.Models;
using LiteViewerCore.ViewModels;

using Ookii.Dialogs.Wpf;

namespace LiteViewer.WPF
{
    /// <summary>
    /// Interaction logic for QueryWindow.xaml
    /// </summary>
    public partial class QueryWindow : Window
    {
        #region Constructors

        public QueryWindow()
        {
            InitializeComponent();

            DataContext = new FakeQueryViewModel();
        }

        #endregion

        #region Private Methods

        private void OnInsertClicked(object sender, RoutedEventArgs routedEventArgs)
        {
            var mi = (MenuItem) sender;
            var parent = (MenuItem) mi.Parent;
            var code = $"{parent.Header}.{mi.Header}";
            var length = 0;
            QueryTextBox.Focus();
            var start = QueryTextBox.CaretIndex;
            if (QueryTextBox.SelectionLength > 0) {
                length = QueryTextBox.SelectionLength;
                start = QueryTextBox.SelectionStart;
            }

            var range = ((QueryViewModel) DataContext).InsertQueryText(start, length, code);
            QueryTextBox.SelectionStart = range.start;
            QueryTextBox.SelectionLength = range.length;
        }

        private void OnOpenClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == true) {
                try {
                    DataContext = new QueryViewModel(dialog.SelectedPath);
                } catch (Exception ex) {
                    MessageBox.Show(ex.Message, "Error opening database");
                    return;
                }

                ((QueryViewModel) DataContext).Error += ShowError;
                QueryTextBox.Focus();
                ((QueryViewModel) DataContext).QueryText = "Query.Select()";
                QueryTextBox.SelectionStart = 13;
                if (InsertMenu.Items.Count == 0) {
                    PopulateInsertMenu();
                }
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            ((QueryViewModel) DataContext).QueryText = ((TextBox) sender).Text;
        }

        private void PopulateInsertMenu()
        {
            foreach (var item in QueryModel.InsertMenuItems) {
                var container = new MenuItem
                {
                    Header = item.Key
                };
                foreach(var child in item.Value.Select(x => new MenuItem { Header = x })) {
                    child.Click += OnInsertClicked;
                    container.Items.Add(child);
                }

                InsertMenu.Items.Add(container);
            }
        }

        private void ShowError(object sender, string e)
        {
            MessageBox.Show(e, "Error running query");
        }

        #endregion
    }
}
