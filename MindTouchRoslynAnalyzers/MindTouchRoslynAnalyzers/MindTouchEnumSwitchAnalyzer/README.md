MindTouchEnumSwitchAnalysis
===========================

This analyzer detects when not all Enum values are accounted for inside a switch statement. For example:

```
    enum MyEnum { A, B, C, D, E, F }
    switch (e) {
        case MyEnum.A:
        case MyEnum.B:
        case MyEnum.C:
        case MyEnum.D:
        case MyEnum.E:
            break;
    }
```

The value `MyEnum.F` is not accounted for. This analyzer also provides a quick fix that will add the following fix for you:

```
switch (e) {
    case MyEnum.A:
    case MyEnum.B:
    case MyEnum.C:
    case MyEnum.D:
    case MyEnum.E:
        break;
    case MyEnum.F:
        throw new NotImplementedException();
}
```
