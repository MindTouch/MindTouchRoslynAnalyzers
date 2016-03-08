/**
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

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace MindTouchEnumSwitchAnalyzer.Test {

    [TestClass]
    public class UnitTest : CodeFixVerifier {

        //--- Class Methods ---
        private static string GetValidatedSourceCode(string source, string diagnosticIdToIgnore) {
            var syntaxTree = CSharpSyntaxTree.ParseText(source, options: new CSharpParseOptions());
            var firstDiagnostic = syntaxTree.GetDiagnostics().FirstOrDefault(diagnostic => diagnostic.Id != diagnosticIdToIgnore && diagnostic.Severity == DiagnosticSeverity.Error);
            if(firstDiagnostic != null) {
                throw new Exception($"C# code failed to compile: '{firstDiagnostic.GetMessage()}'");
            }
            return source;
        }

        //--- Methods ---
        [TestMethod]
        public void No_diagnostics_when_all_cases_covered() {
            var test = GetValidatedSourceCode(@"
    using System;
    namespace Application {
        
        enum MyEnum { A, B, C, D, E, F };

        class MyClass {
            public static void Function() {
                MyEnum e;
                switch(e) {
                    case MyEnum.A:
                    case MyEnum.B:
                    case MyEnum.C:
                    case MyEnum.D:
                    case MyEnum.E:
                    case MyEnum.F:
                        break;
                }
            }
        }    
    }", EnumSwitchAnalyzerAnalyzer.DIAGNOSTIC_ID);
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Diagnostics_and_code_fix_when_enum_value_missing() {
            var test = GetValidatedSourceCode(@"
    using System;
    namespace Application {

    enum MyEnum { A, B, C, D, E, F }

    class MyClass {
        public static void Function() {
            MyEnum e = 0;
            switch (e) 
            {
                case MyEnum.A:
                    break;
                case MyEnum.B:
                    break;
                case MyEnum.C:
                case MyEnum.D:
                case MyEnum.E:
                    break;
            }
        }
    }
}", EnumSwitchAnalyzerAnalyzer.DIAGNOSTIC_ID);
            var expected = new DiagnosticResult {
                Id = "EnumSwitchAnalyzer",
                Message = string.Format("switch on enum 'MyEnum' is missing the following members: 'F'"),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 13)
                        }
            };
            VerifyCSharpDiagnostic(test, expected);
            var fixtest = GetValidatedSourceCode(@"
    using System;
    namespace Application {

    enum MyEnum { A, B, C, D, E, F }

    class MyClass {
        public static void Function() {
            MyEnum e = 0;
            switch (e)
            {
                case MyEnum.A:
                    break;
                case MyEnum.B:
                    break;
                case MyEnum.C:
                case MyEnum.D:
                case MyEnum.E:
                    break;
                case MyEnum.F:
                    throw new NotImplementedException();
            }
        }
    }
}", EnumSwitchAnalyzerAnalyzer.DIAGNOSTIC_ID);
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void Diagnostics_and_code_fix_when_enum_value_missing_with_default() {
            var test = GetValidatedSourceCode(@"
    using System;
    namespace Application {

    enum MyEnum { A, B, C, D, E, F }

    class MyClass {
        public static void Function() {
            MyEnum e = 0;
            switch (e) 
            {
                case MyEnum.A:
                case MyEnum.B:
                case MyEnum.C:
                case MyEnum.D:
                case MyEnum.E:
                    break;
                default:
                    throw new Exception();
            }
        }
    }
}", EnumSwitchAnalyzerAnalyzer.DIAGNOSTIC_ID);
            var expected = new DiagnosticResult {
                Id = "EnumSwitchAnalyzer",
                Message = string.Format("switch on enum 'MyEnum' is missing the following members: 'F'"),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 13)
                        }
            };
            VerifyCSharpDiagnostic(test, expected);
            var fixtest = GetValidatedSourceCode(@"
    using System;
    namespace Application {

    enum MyEnum { A, B, C, D, E, F }

    class MyClass {
        public static void Function() {
            MyEnum e = 0;
            switch (e)
            {
                case MyEnum.A:
                case MyEnum.B:
                case MyEnum.C:
                case MyEnum.D:
                case MyEnum.E:
                    break;
                case MyEnum.F:
                    throw new NotImplementedException();
                default:
                    throw new Exception();
            }
        }
    }
}", EnumSwitchAnalyzerAnalyzer.DIAGNOSTIC_ID);
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void Nested_enum_diagnostic() {
            var test = GetValidatedSourceCode(@"
    using System;
    namespace Application {
        
        enum MyEnum { A, B, C, D };

        class MyClass {
            public static void Function() {
                MyEnum e = 0;
                MyEnum f = 0;
                switch(e) 
                {
                    case MyEnum.A:
                    case MyEnum.B:
                        switch(f) 
                        {
                            case MyEnum.C:
                            case MyEnum.D:
                            break;
                        }
                    break;
                }
            }
        }    
    }", EnumSwitchAnalyzerAnalyzer.DIAGNOSTIC_ID);
            var expected = new[] {
                new DiagnosticResult {
                    Id = "EnumSwitchAnalyzer",
                    Message = string.Format("switch on enum 'MyEnum' is missing the following members: 'C, D'"),
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] {
                        new DiagnosticResultLocation("Test0.cs", 11, 17)
                    }
                },
                new DiagnosticResult {
                    Id = "EnumSwitchAnalyzer",
                    Message = string.Format("switch on enum 'MyEnum' is missing the following members: 'A, B'"),
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] {
                        new DiagnosticResultLocation("Test0.cs", 15, 25)
                    }
                }
            };
            VerifyCSharpDiagnostic(test, expected);

            var fixtest = GetValidatedSourceCode(@"
    using System;
    namespace Application {
        
        enum MyEnum { A, B, C, D };

        class MyClass {
            public static void Function() {
                MyEnum e = 0;
                MyEnum f = 0;
            switch (e)
            {
                case MyEnum.A:
                    case MyEnum.B:
                    switch (f)
                    {
                        case MyEnum.C:
                            case MyEnum.D:
                            break;
                        case MyEnum.A:
                            throw new NotImplementedException();
                        case MyEnum.B:
                            throw new NotImplementedException();
                    }
                    break;
                case MyEnum.C:
                    throw new NotImplementedException();
                case MyEnum.D:
                    throw new NotImplementedException();
            }
        }
        }    
    }", EnumSwitchAnalyzerAnalyzer.DIAGNOSTIC_ID);
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void Diagnostics_and_code_fix_when_switch_is_empty() {
            var test = GetValidatedSourceCode(@"
    using System;
    namespace Application {

    enum MyEnum { A, B, C, D, E, F }

    class MyClass {
        public static void Function() {
            MyEnum e = 0;
            switch (e)
            {
            }
        }
    }
}", EnumSwitchAnalyzerAnalyzer.DIAGNOSTIC_ID);
            var expected = new DiagnosticResult {
                Id = "EnumSwitchAnalyzer",
                Message = string.Format("switch on enum 'MyEnum' is missing the following members: 'A, B, C, D, E, F'"),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 13)
                        }
            };
            VerifyCSharpDiagnostic(test, expected);
            var fixtest = GetValidatedSourceCode(@"
    using System;
    namespace Application {

    enum MyEnum { A, B, C, D, E, F }

    class MyClass {
        public static void Function() {
            MyEnum e = 0;
            switch (e)
            {
                case MyEnum.A:
                    throw new NotImplementedException();
                case MyEnum.B:
                    throw new NotImplementedException();
                case MyEnum.C:
                    throw new NotImplementedException();
                case MyEnum.D:
                    throw new NotImplementedException();
                case MyEnum.E:
                    throw new NotImplementedException();
                case MyEnum.F:
                    throw new NotImplementedException();
            }
        }
    }
}", EnumSwitchAnalyzerAnalyzer.DIAGNOSTIC_ID);
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider() {
            return new EnumSwitchAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() {
            return new EnumSwitchAnalyzerAnalyzer();
        }
    }
}