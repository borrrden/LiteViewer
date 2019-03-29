// 
//  QueryModel.cs
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
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Couchbase.Lite;
using Couchbase.Lite.Query;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace LiteViewerCore.Models
{
    public sealed class QueryModel
    {
        #region Variables

        private readonly Database _db;
        private bool _clear = true;
        private IQuery _lastQuery;

        #endregion

        #region Properties

        public int Count { get; private set; }

        public static Dictionary<string, IReadOnlyList<string>> InsertMenuItems { get; } = PopulateInsertItems();

        private static Dictionary<string, IReadOnlyList<string>> PopulateInsertItems()
        {
            var retVal = new Dictionary<string, IReadOnlyList<string>>();

            foreach (var t in new[]
            {
                typeof(SelectResult), typeof(Expression), typeof(Function), typeof(Ordering), typeof(Join), typeof(DataSource),
                typeof(ArrayExpression), typeof(ArrayFunction), typeof(Collation), typeof(FullTextExpression), typeof(FullTextFunction),
                typeof(Meta)
            }) {
                retVal[t.Name] = t.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Select(x => x.IsSpecialName ? x.Name.TrimStart('g', 'e', 't', '_') : x.Name)
                .ToImmutableArray();
            }

            foreach (var t in new[]
            {
                typeof(IExpression), typeof(IFromRouter), typeof(IWhereRouter), typeof(IJoinRouter), typeof(IOrderByRouter),
                typeof(IGroupByRouter), typeof(ISortOrder), typeof(IArrayExpressionIn), typeof(IArrayExpressionSatisfies),
                typeof(IASCIICollation), typeof(IUnicodeCollation), typeof(IDataSourceAs), typeof(IFullTextExpression),
                typeof(IHavingRouter), typeof(IJoinOn), typeof(IMetaExpression), typeof(IPropertyExpression), 
                typeof(ISelectResultAs), typeof(ISelectResultFrom)

            }) {
                retVal[t.Name] = t.GetMethods(BindingFlags.Public | BindingFlags.Instance).Select(x => x.Name)
                    .ToImmutableArray();
            }

            return retVal;
        }

        #endregion

        #region Constructors

        public QueryModel(Database db)
        {
            _db = db;
        }

        #endregion

        #region Public Methods

        public void Clear()
        {
            _clear = true;
        }

        public async Task<IResultSet> Interpret(string source, uint position, uint skip = 0U, uint limit = UInt32.MaxValue)
        {
            if (_clear) {
                var globals = new Globals { Db = _db };
                var options = ScriptOptions.Default
                    .WithReferences(typeof(IQuery).GetTypeInfo().Assembly)
                    .AddImports("Couchbase.Lite")
                    .AddImports("Couchbase.Lite.Query");
                
                var compiled = CSharpScript.Create<IQuery>($"return {source}", options, typeof(Globals));
                compiled.Compile();
                var state = await compiled.RunAsync(globals);
                Interlocked.Exchange(ref _lastQuery, state.ReturnValue)?.Dispose();
                switch (_lastQuery)
                {
                    case ILimitRouter lr:
                        _lastQuery = lr.Limit(Expression.Parameter("l"), Expression.Parameter("s"));
                        break;
                }

                _lastQuery.Parameters.SetValue("l", limit).SetValue("s", skip);
                var tmpResults = _lastQuery.Execute();
                    Count = tmpResults.Count();

                _clear = false;
            }

            _lastQuery.Parameters.SetValue("l", Math.Min(100U, limit - position)).SetValue("s", skip + position);
            return _lastQuery.Execute();
        }

        #endregion
    }

    public class Globals
    {
        #region Variables

        public Database Db;

        #endregion
    }
}