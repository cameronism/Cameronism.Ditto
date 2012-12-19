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
		public class MemberDetail
		{
			public readonly string Name;
			public readonly Type Type;
			public readonly PropertyInfo Property;
			public readonly FieldInfo Field;

			private MemberDetail (PropertyInfo property)
			{
				Name = property.Name;
				Type = property.PropertyType;
				Property = property;
			}

			private MemberDetail (FieldInfo field)
			{
				Name = field.Name;
				Type = field.FieldType;
				Field = field;
			}

			public MemberExpression GetExpression(Expression instance)
			{
				return Field != null ?
					Expression.Field(instance, Field) :
					Expression.Property(instance, Property);
			}

			public static List<MemberDetail> FindInstanceMembers(Type type)
			{
				var members = new List<MemberDetail>();
				members.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.Public).Select(fi => new MemberDetail(fi)));
				members.AddRange(type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(pi => new MemberDetail(pi)));
				return members;
			}
		}

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

			foreach (var mi in MemberDetail.FindInstanceMembers(type))
			{
				Expression rhs = mi.GetExpression(sourceEx);
				if (mi.Type.IsClass && !IsImmutable(mi.Type))
				{
					rhs = Expression.Condition(
						Expression.Equal(rhs, Expression.Constant(null, mi.Type)),
						Expression.Constant(null, mi.Type),
						Expression.Call(_DittoClone.MakeGenericMethod(mi.Type), rhs),
						mi.Type);
				}

				// destination.{field} = source.{field};
				body.Add(
					Expression.Assign(
						mi.GetExpression(destinationEx),
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
