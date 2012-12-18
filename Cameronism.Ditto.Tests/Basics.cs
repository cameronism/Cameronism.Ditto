using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Cameronism;

namespace Cameronism.Tests
{
	public class Basics
	{
		class MyType
		{
			public int A;
			public string B;
		}

		[Fact]
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

		[Fact]
		public void RealStuff()
		{
			var myObject = new MyType { A = 42 };

			var copy = Ditto.DeepClone(myObject);
			Assert.NotNull(copy);
			Assert.Equal(42, copy.A);

			copy = Ditto.DeepClone<MyType>(null);
			Assert.Null(copy);
		}

		class Outer
		{
			public MyType Instance;
		}

		[Fact]
		public void Deeper()
		{
			var obj = new Outer { Instance = new MyType { B = "the answer", A = 42 } };
			var copy = Ditto.DeepClone(obj);
			Assert.NotSame(obj.Instance, copy.Instance);
			Assert.Same(obj.Instance.B, copy.Instance.B);

			Assert.Null(Ditto.DeepClone(new Outer()).Instance);
		}

	}
}
