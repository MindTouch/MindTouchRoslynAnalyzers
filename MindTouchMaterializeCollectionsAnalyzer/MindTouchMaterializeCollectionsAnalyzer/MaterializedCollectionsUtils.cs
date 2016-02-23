﻿/**
 * Copyright (c) 2016 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MindTouchMaterializeCollectionsAnalyzer {
    internal class MaterializedCollectionsUtils {

        //--- Class Methods ---
        internal static bool IsCollection(ITypeSymbol symbol) {
            return (symbol != null && symbol.Interfaces.Any(x => x.Name == typeof(IEnumerable).Name || x.Name == typeof(IEnumerable<>).Name));
        }
    }
}