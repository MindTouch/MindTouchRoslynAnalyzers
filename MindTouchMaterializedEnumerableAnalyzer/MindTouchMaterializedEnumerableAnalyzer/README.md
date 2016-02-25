MindTouchMaterializedEnumerableAnalyzer
=======================================

We follow the convention that whenever an IEnumerable type is passed into a function or returned from a function, it must be materialized to a concrete type. For example:

```
var myArray = new [] { 1, 2, 3, 4 };
var squares = myArray.Select(x => x*x);
var someMethod(squares);
```

We would write this instead:

```
var myArray = new [] { 1, 2, 3, 4 };
var squares = myArray.Select(x => x*x);
var someMethod(squares.ToArray());
```

In the same spirit, whenever a collection is returned from a method it should be materialized as well:

Instead of:

```
IEnumerable<T> Squares() {
	...
	return someList.Select(x*x);
}
```

we write:

```
IEnumerable<T> Squares() {
	...
	return someList.Select(x*x).ToArray();
}
```
