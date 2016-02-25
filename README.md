RoslynAnalyzers
===============

C# code analyzers built on the Roslyn compiler.

[![Build status](https://ci.appveyor.com/api/projects/status/lj3g8340v18oewap?svg=true)](https://ci.appveyor.com/project/yurigorokhov/roslynanalyzers)

MindTouchEnumSwitchAnalyzer
---------------------------

Roslyn analyzer that ensures that all enum cases are handled when used inside a switch statement. [Read more.](MindTouchEnumSwitchAnalyzer/MindTouchEnumSwitchAnalyzer/README.md)

MindTouchMaterializedEnumerableAnalyzer
---------------------------------------

Analyzer that ensures that any collections passed in as parameters to a function, or returned from a function are materialized. [Read more.](MindTouchMaterializedEnumerableAnalyzer/MindTouchMaterializedEnumerableAnalyzer/README.md)

System Requirements
-------------------

-	To run
	-	Microsoft Visual Studio 2015 with .Net 4.0 or later
-	To build
	-	Microsoft Visual Studio 2015 with .Net 4.5 or later

License
-------

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at

```
http://www.apache.org/licenses/LICENSE-2.0
```

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
