using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;

namespace Cameronism
{
	internal static class Delegates
	{
		public static void Create<T>(out Func<T, T> deepClone, out Func<T, int> getHashCode, out Func<T, T, bool> equals)
		{
			var type = typeof(T);

			deepClone = type.IsClass ? GetClassCloneMethod<T>(type) : GetStructCloneMethod<T>();

			getHashCode = delegate { return 0; };
			equals = delegate { return true; };
		}

		public static bool IsImmutable(Type type)
		{
			// TODO add test for anonymous types
			return type == typeof(string);
		}

		static readonly Func<int, int> _DittoCloneReference = Ditto.DeepClone<int>;
		static readonly MethodInfo _DittoClone = _DittoCloneReference.Method.GetGenericMethodDefinition();

		static Func<T, T> GetClassCloneMethod<T>(Type type)
		{
			var body = new List<Expression>();
			var sourceEx = Expression.Parameter(type, "source");
			var destinationEx = Expression.Variable(type, "destination");

			// destination = new {T}();
			body.Add(Expression.Assign(destinationEx, Expression.New(type)));

			foreach (var fi in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
			{
				Expression rhs = Expression.Field(sourceEx, fi);
				if (fi.FieldType.IsClass && !IsImmutable(fi.FieldType))
				{
					rhs = Expression.Condition(
						Expression.Equal(rhs, Expression.Constant(null, fi.FieldType)),
						Expression.Constant(null, fi.FieldType),
						Expression.Call(_DittoClone.MakeGenericMethod(fi.FieldType), rhs),
						fi.FieldType);
				}

				// destination.{field} = source.{field};
				body.Add(
					Expression.Assign(
						Expression.Field(destinationEx, fi),
						rhs));
			}


			// if (source == null) 
			// { destination = null; }
			// else
			// { body }
			body = new List<Expression>
			{
				Expression.IfThenElse(
					Expression.Equal(sourceEx, Expression.Constant(null, type)),
					Expression.Assign(destinationEx, Expression.Constant(null, type)),
					Expression.Block(body))
			};

			// return destination;
			body.Add(destinationEx);

			var deepLambda = Expression.Lambda<Func<T, T>>(Expression.Block(type, new[]{ destinationEx }, body), sourceEx);

			return deepLambda.Compile();
		}

		static Func<T, T> GetStructCloneMethod<T>()
		{
			// is this overkill or not enough?
			return source => { T destination = source; return destination; };
		}
	}
}
