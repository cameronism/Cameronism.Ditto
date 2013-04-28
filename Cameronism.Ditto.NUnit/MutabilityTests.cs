using System;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Cameronism.DittoNUnit
{
	[TestFixture]
	public class MutabilityTests
	{
		#region custom types
		class A_Mutable
		{
			public int A;
			public string B;

			public A_Mutable (int a, string b)
			{
				A = a;
				B = b;
			}
		}

		class B_NotMutable
		{
			public readonly int A;
			public readonly string B;
			
			public B_NotMutable (int a, string b)
			{
				A = a;
				B = b;
			}
		}

		class CyclicalNotMutable_A
		{
			public readonly CyclicalNotMutable_B Other;
			public CyclicalNotMutable_A (CyclicalNotMutable_B other = null)
			{
				Other = other;
			}
		}

		class CyclicalNotMutable_B
		{
			public readonly CyclicalNotMutable_A Other;
			public CyclicalNotMutable_B (CyclicalNotMutable_A other = null)
			{
				Other = other;
			}
		}
		#endregion


		static TestCaseData T<T>(T sample, bool expectMutable, string name = null)
		{
			return T<T>(expectMutable, name);
		}

		static TestCaseData T<T>(bool expectMutable, string name = null)
		{
			var tc = new TestCaseData (typeof(T)).Returns (expectMutable);
			if (!String.IsNullOrWhiteSpace (name))
				tc.SetName (name);
			return tc;
		}

		public static IEnumerable MutabilityCases = new[] {
			T<string>(false),
			T<int>(false),
			T<A_Mutable>(true),
			T(new { a = 1}, false, "simple anonymous type"),
			T(new { a = 1, b = new int[0] }, true, "anonymous type with array"),
			T(new { a = 1, b = (A_Mutable)null }, true, "anonymous with custom mutable member"),
			T(new { a = 1, b = new { c = (A_Mutable)null } }, true),
			T<string[]>(true, "array"),
			T<List<string>>(true),
			T<B_NotMutable>(false, "custom immutable type"),
			T<CyclicalNotMutable_A>(false, "cyclical immutable types"),
			T<CyclicalNotMutable_B>(false, "cyclical immutable types"),
			T<IList<int>>(true, "generic collection interface"),
			T<System.TypeCode>(false, "an enum"),
		};

		[Test, TestCaseSource("MutabilityCases")]
		public bool Mutable(Type type)
		{
			return Ditto.IsMutable (type);
		}
	}
}