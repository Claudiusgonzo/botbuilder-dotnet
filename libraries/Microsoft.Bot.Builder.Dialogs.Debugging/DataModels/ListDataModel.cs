﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Bot.Builder.Dialogs.Debugging
{
#pragma warning disable CA1812 // Supressing error due to internal being used as intended
    internal sealed class ListDataModel<T> : DataModelBase<IList<T>, int, T>
#pragma warning restore CA1812 // Supressing error due to internal being used as intended
    {
        public ListDataModel(ICoercion coercion)
            : base(coercion)
        {
        }

        public override int Rank => 4;

        public override T this[IList<T> context, int name]
        {
            get => context[name];
            set => context[name] = value;
        }

        public override IEnumerable<int> Names(IList<T> context) => Enumerable.Range(0, context.Count);
    }
}
