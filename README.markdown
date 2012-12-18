.NET Deep Cloner and Object Comparer
=====================================

**important -- the following is not true (yet)**

Features
---------

- Supports
  * Deep cloning (full copies)
  * Object comparison
  * Hashcode
- Generates IEqualityComparer\<T> 



Usage
-------

```csharp

// static usage
var copy = Ditto.DeepClone(myObject);
Assert(!Object.ReferenceEquals(copy, myObject), "copy is not the same object");
Assert(Ditto.Equals(copy, myObject), "copy is equivalent");
Assert(Ditto.GetHashCode(copy) == Ditto.GetHashCode(myObject));

// instance usage
var comparer = Ditto.GetComparer<MyType>();
Assert(comparer is IEqualityComparer<MyType>);

var copy2 = comparer.DeepClone(myObject);
Assert(!Object.ReferenceEquals(copy2, myObject), "copy2 is not the same object");
Assert(comparer.Equals(copy2, myObject), "copy2 is equivalent");
Assert(comparer.GetHashCode(copy2) == Ditto.GetHashCode(myObject));

```

Future
-------

- Customize behavior with DittoAttribute
- Provide a base class that automatically implements IEquatable\<T>
- Support Ditto.DeepClone(sourceObject, destinationObject)


Documentation TODO
-------------------

- Discuss value types
- Discuss immutable reference types (string, anonymous types)