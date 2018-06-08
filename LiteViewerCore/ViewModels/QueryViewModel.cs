﻿// 
//  QueryViewModel.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;

using Couchbase.Lite;

using LiteViewerCore.Models;
using LiteViewerCore.Util;

using Newtonsoft.Json;

namespace LiteViewerCore.ViewModels
{
    public sealed class QueryViewModel : NotifyPropertyChanged
    {
        #region Variables

        private static readonly IReadOnlyCollection<string> QuotedItems = new HashSet<string>
        {
            "Expression.Property", "Expression.Parameter", "Ordering.Property"
        };

        private static readonly IReadOnlyCollection<string> NoArgItems = new HashSet<string>
        {
            "Expression.Meta", "SelectResult.All", ".Ascending", ".Descending"
        };

        private readonly QueryModel _model;
        private int _count;

        private uint _limit = UInt32.MaxValue;
        private List<string> _queryResults;
        private string _queryText;
        private uint _skip;
        private uint _pageSkip;

        public event EventHandler<string> Error;

        #endregion

        #region Properties

        public bool Ready => true;

        public int Count
        {
            get => _count;
            set {
                SetProperty(ref _count, value);
                RaisePropertyChanged(nameof(PaginationText));
            }
        }

        public ICommand EnterAllDocIDsCommand => new Command(() => QueryText = "Query.Select(SelectResult.Expression(Expression.Meta().ID)).From(DataSource.Database(Db));");

        public string Limit
        {
            get => _limit.ToString();
            set {
                if (UInt32.TryParse(value, out var tmp))  {
                    SetProperty(ref _limit, tmp);
                }
            }
        }

        public ICommand NextPageCommand => new Command(NextPage);

        public string PaginationText => $"{_pageSkip + 1} - {Math.Min(_pageSkip + 100, _count)} of {_count}";

        public ICommand PreviousPageCommand => new Command(PreviousPage);

        public ICommand QueryCommand => new Command(() =>
        {
            _pageSkip = 0;
            _model.Clear();
            DoQuery();
            
        });

        public List<string> QueryResults
        {
            get => _queryResults;
            set => SetProperty(ref _queryResults, value);
        }

        public string QueryText
        {
            get => _queryText;
            set => SetProperty(ref _queryText, value);
        }

        public string Skip
        {
            get => _skip.ToString();
            set {
                if (UInt32.TryParse(value, out var tmp)) {
                    SetProperty(ref _skip, tmp);
                }
            }
        }

        #endregion

        #region Constructors

        public QueryViewModel(string path)
        {
            var config = new DatabaseConfiguration
            {
                Directory = Path.GetDirectoryName(path)
            };

            if (!Database.Exists(Path.GetFileNameWithoutExtension(path), config.Directory)) {
                throw new InvalidOperationException("Selected folder is not a Couchbase Lite database");
            }

            _model = new QueryModel(new Database(Path.GetFileNameWithoutExtension(path), config));
        }

        #endregion

        public (int start, int length) InsertQueryText(int start, int length, string text)
        {
            var queryText = QueryText ?? String.Empty;
            if (text.StartsWith("I")) {
                text = $".{text.Split('.')[1]}";
            }
            (int, int) retVal;
            string insertion;
            if (NoArgItems.Contains(text)) {
                insertion = $"{text}()";
                retVal = (start + text.Length + 2, 0);
            } else if (QuotedItems.Contains(text)) {
                insertion = $"{text}(\"x\")";
                retVal = (start + text.Length + 2, 1);
            } else {  
                insertion = $"{text}(x)";
                retVal = (start + text.Length + 1, 1);
            }

            QueryText = queryText.Remove(start, length).Insert(start, insertion);
            return retVal;
        }

        #region Private Methods

        private async void DoQuery()
        {
            var queryResults = new List<string>();
            try {
                var results = await _model.Interpret(QueryText, _pageSkip, _skip, _limit);
                Count = _model.Count;
                foreach (var result in results) {
                    queryResults.Add(JsonConvert.SerializeObject(result.ToList()));
                }
            } catch (Exception e) {
                Debug.WriteLine($"Exception interpreting query: {e}");
                Error?.Invoke(this, e.Message);
                return;
            }

            QueryResults = queryResults;
        }

        private void NextPage()
        {
            if (_pageSkip > Count - 100) {
                return;
            }

            _pageSkip += 100;
            DoQuery();
        }

        private void PreviousPage()
        {
            if (_pageSkip == 0) {
                return;
            }

            if (_pageSkip < 100) {
                _pageSkip = 0;
            } else {
                _pageSkip -= 100;
            }

            DoQuery();
        }

        #endregion
    }
}