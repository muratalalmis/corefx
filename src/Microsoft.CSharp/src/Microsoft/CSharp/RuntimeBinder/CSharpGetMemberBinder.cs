// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder
{
    /// <summary>
    /// Represents a dynamic property access in C#, providing the binding semantics and the details about the operation. 
    /// Instances of this class are generated by the C# compiler.
    /// </summary>
    internal sealed class CSharpGetMemberBinder : GetMemberBinder, IInvokeOnGetBinder, ICSharpBinder
    {
        public BindingFlag BindingFlags => BindingFlag.BIND_RVALUEREQUIRED;

        public EXPR DispatchPayload(RuntimeBinder runtimeBinder, ArgumentObject[] arguments, LocalVariableSymbol[] locals)
        {
            Debug.Assert(arguments.Length == 1);
            return runtimeBinder.BindProperty(this, arguments[0], locals[0], null, false);
        }

        public void PopulateSymbolTableWithName(SymbolTable symbolTable, Type callingType, ArgumentObject[] arguments)
            => symbolTable.PopulateSymbolTableWithName(Name, null, arguments[0].Type);

        public bool IsBinderThatCanHaveRefReceiver => false;

        public Type CallingContext { get; }

        public bool IsChecked => false;

        private readonly List<CSharpArgumentInfo> _argumentInfo;

        CSharpArgumentInfo ICSharpBinder.GetArgumentInfo(int index) => _argumentInfo[index];

        bool IInvokeOnGetBinder.InvokeOnGet => !ResultIndexed;

        private bool ResultIndexed { get; }

        private readonly RuntimeBinder _binder;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpGetMemberBinder" />.
        /// </summary>
        /// <param name="name">The name of the member to get.</param>
        /// <param name="resultIndexed">Determines if COM binder should return a callable object.</param>
        /// <param name="callingContext">The <see cref="System.Type"/> that indicates where this operation is defined.</param>
        /// <param name="argumentInfo">The sequence of <see cref="CSharpArgumentInfo"/> instances for the arguments to this operation.</param>
        public CSharpGetMemberBinder(
                string name,
                bool resultIndexed,
                Type callingContext,
                IEnumerable<CSharpArgumentInfo> argumentInfo) :
            base(name, false /*caseInsensitive*/)
        {
            ResultIndexed = resultIndexed;
            CallingContext = callingContext;
            _argumentInfo = BinderHelper.ToList(argumentInfo);
            _binder = RuntimeBinder.GetInstance();
        }

        /// <summary>
        /// Performs the binding of the dynamic get member operation if the target dynamic object cannot bind.
        /// </summary>
        /// <param name="target">The target of the dynamic get member operation.</param>
        /// <param name="errorSuggestion">The binding result to use if binding fails, or null.</param>
        /// <returns>The <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
#if ENABLECOMBINDER
            DynamicMetaObject com;
            if (!BinderHelper.IsWindowsRuntimeObject(target) && ComBinder.TryBindGetMember(this, target, out com, ResultIndexed))
            {
                return com;
            }
#endif
            return BinderHelper.Bind(this, _binder, new[] { target }, _argumentInfo, errorSuggestion);
        }
    }
}
