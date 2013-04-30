using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Cameronism.DittoNUnit
{
	[TestFixture]
	public class Basics
	{
		class MyType
		{
			public int A;
			public string B;
		}

		[Test]
		public void Readme()
		{
			var myObject = new MyType();
			
			// static usage
			var copy = Ditto.DeepClone(myObject);
			Assert.False(Object.ReferenceEquals(copy, myObject), "copy is not the same object");
			Assert.True(Ditto.Equals(copy, myObject), "copy is equivalent");
			Assert.True(Ditto.GetHashCode(copy) == Ditto.GetHashCode(myObject));
			
			// instance usage
			var comparer = Ditto.GetComparer<MyType>();
			Assert.True(comparer is IEqualityComparer<MyType>);
			
			var copy2 = comparer.DeepClone(myObject);
			Assert.False(Object.ReferenceEquals(copy2, myObject), "copy2 is not the same object");
			Assert.True(comparer.Equals(copy2, myObject), "copy2 is equivalent");
			Assert.True(comparer.GetHashCode(copy2) == Ditto.GetHashCode(myObject));
		}
		
		[Test]
		public void RealStuff()
		{
			var myObject = new MyType { A = 42 };
			
			var copy = Ditto.DeepClone(myObject);
			Assert.NotNull(copy);
			Assert.AreEqual(42, copy.A);
			
			copy = Ditto.DeepClone<MyType>(null);
			Assert.Null(copy);
		}
		
		class Outer
		{
			public MyType Instance;
		}
		
		[Test]
		public void Deeper()
		{
			var obj = new Outer { Instance = new MyType { B = "the answer", A = 42 } };
			var copy = Ditto.DeepClone(obj);
			Assert.AreNotSame(obj.Instance, copy.Instance);
			Assert.AreSame(obj.Instance.B, copy.Instance.B);

			
			Assert.Null(Ditto.DeepClone(new Outer()).Instance);
		}
		
		class PropContainer<T>
		{
			public T Prop { get; set; }
		}
		
		[Test]
		public void Properties()
		{
			var obj = new PropContainer<int> { Prop = 42 };
			var clone = Ditto.DeepClone(obj);
			Assert.AreEqual(obj.Prop, clone.Prop);
		}
		
		[Test]
		public void CommonInterfaces()
		{
			var obj = new PropContainer<IEnumerable<int>> { Prop = Enumerable.Range(0, 10) };
			var clone = Ditto.DeepClone(obj);
			Assert.AreEqual(obj.Prop, clone.Prop);
			Assert.AreNotSame(obj.Prop, clone.Prop);
			
			obj = new PropContainer<IEnumerable<int>> { Prop = null };
			clone = Ditto.DeepClone(obj);
			Assert.Null(clone.Prop);
		}

		[Test]
		public void AnonymousType_With_MutableMembers()
		{
			var obj = new { m = new MyType () };
			var clone = Ditto.DeepClone (obj);
			Assert.AreNotSame (obj, clone);
		}

		
		[Test]
		public void AnonymousType_Without_MutableMembers()
		{
			var obj = new { a = 1, b = 0.5, c = "stringy" };
			var clone = Ditto.DeepClone (obj);

			//Assert.ReferenceEquals (obj, clone);

			Assert.True(Object.ReferenceEquals(obj, clone), "immutable type should return same reference");
		}
	}
}

