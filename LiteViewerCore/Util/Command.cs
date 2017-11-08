// 
//  Command.cs
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
using System.Windows.Input;

namespace LiteViewerCore.Util
{
    public sealed class Command : ICommand
    {
        #region Variables

        private readonly Action _actionToExecute;

        public event EventHandler CanExecuteChanged;

        #endregion

        #region Constructors

        public Command(Action action)
        {
            _actionToExecute = action;
        }

        #endregion

        #region ICommand

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            _actionToExecute?.Invoke();
        }

        #endregion
    }
}