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

			public Expression GetMemberCopy(Expression instance)
			{
				Expression rhs = GetExpression (instance);

				if ((this.Type.IsClass || this.Type.IsInterface) && !IsImmutable(this.Type)) {
					rhs = Expression.Condition(
						Expression.Equal(rhs, Expression.Constant(null, this.Type)),
						Expression.Constant(null, this.Type),
						Expression.Call(_DittoClone.MakeGenericMethod(this.Type), rhs),
						this.Type);
				}

				return rhs;
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

			if (type.IsValueType || IsImmutable(type))
			{
				deepClone = GetStructCloneMethod<T>();
			}
			else if (!TryGetListCloneMethod(type, out deepClone))
			{
				// finally
				deepClone = GetClassCloneMethod<T>(type);
			}

			getHashCode = delegate { return 0; };
			equals = delegate { return true; };
		}

		public static bool IsImmutable(Type type)
		{
			return !Ditto._MutableTypeCache.IsMutable (type);
		}

		static readonly Func<int, int> _DittoCloneReference = Ditto.DeepClone<int>;
		static readonly MethodInfo _DittoClone = _DittoCloneReference.Method.GetGenericMethodDefinition();

		static bool TryCloneConstructor(Type type, Dictionary<string, MemberDetail> members, List<Expression> body, ParameterExpression sourceEx, ParameterExpression destinationEx)
		{
			ParameterInfo[] maxParameters = null;
			ConstructorInfo maxCtor = null;

			foreach (var ctor in type.GetConstructors ()) {
				var parameters = ctor.GetParameters();

				// there's a parameterless ctor -- bail
				if (parameters.Length == 0) {
					return false;
				}

				// one of the parameter names doesn't match a public property -- skip this one
				if (!parameters.All(mi => members.ContainsKey(mi.Name))) {
					continue;
				}

				if (maxParameters == null || parameters.Length > maxParameters.Length) {
					maxParameters = parameters;
					maxCtor = ctor;
				}
			}

			if (maxCtor == null) {
				throw new Exception("No suitable constructors were found to clone type: " + type.FullName);
			}

			// destination = new {T}(...);
			body.Add(Expression.Assign(
				destinationEx, 
				Expression.New(
					maxCtor, 
					maxParameters
						.Select(param => members[param.Name].GetMemberCopy(sourceEx)))));

			return true;
		}

		static void CloneMembers(Type type, Dictionary<string, MemberDetail> members, List<Expression> body, ParameterExpression sourceEx, ParameterExpression destinationEx)
		{
			// destination = new {T}();
			body.Add(Expression.Assign(destinationEx, Expression.New(type)));
			
			foreach (var mi in members.Values) {
				
				// destination.{field} = source.{field};
				body.Add(
					Expression.Assign(
					mi.GetExpression(destinationEx),
					mi.GetMemberCopy(sourceEx)));
			}
		}

		static Func<T, T> GetClassCloneMethod<T>(Type type)
		{
			var body = new List<Expression>();
			var sourceEx = Expression.Parameter(type, "source");
			var destinationEx = Expression.Variable(type, "destination");
			var members = MemberDetail.FindInstanceMembers(type).ToDictionary(mi => mi.Name);

			if (!TryCloneConstructor (type, members, body, sourceEx, destinationEx)) {
				CloneMembers (type, members, body, sourceEx, destinationEx);
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

		/// <summary>
		/// see if List&lt;T&lt; is a good fit
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="type"></param>
		/// <param name="deepClone"></param>
		/// <returns></returns>
		static bool TryGetListCloneMethod<T>(Type type, out Func<T, T> deepClone)
		{
			var genericDefinition = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
			Type itemType = null;
			if (genericDefinition != null)
			{
				if (type.IsInterface)
				{
					if (genericDefinition == typeof(IEnumerable<>))
					{
						itemType = type.GetGenericArguments()[0];
					}
					else
					{
						Type ienumerableType = type.GetInterfaces().FirstOrDefault(ti => ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IEnumerable<>));
						if (ienumerableType != null)
						{
							itemType = ienumerableType.GetGenericArguments()[0];
						}
					}
				}
				else
				{
					if (genericDefinition == typeof(List<>))
					{
						itemType = type.GetGenericArguments()[0];
					}
				}
			}

			if (itemType == null || !type.IsAssignableFrom(typeof(List<>).MakeGenericType(itemType)))
			{
				deepClone = null;
				return false;
			}


			// nice to have
			// - grab Count if available
			// - IEqualityComparer (on list?)
			// - bypass call to GetComparer and Enumerable.Select for value and immutable types

			// simplest
			// Enumerable.ToList(Enumerable.Select(source, Ditto.GetComparer<>().DeepClone ))


			// keep it simple for now
			MethodInfo toListMethod = 
				((Func<IEnumerable<int>, List<int>>) 
					Enumerable.ToList<int>
				).Method.GetGenericMethodDefinition()
				.MakeGenericMethod(itemType);

			MethodInfo selectMethod = 
				((Func<IEnumerable<int>, Func<int, int>, IEnumerable<int>>)
					Enumerable.Select<int, int>
				).Method.GetGenericMethodDefinition()
				.MakeGenericMethod(itemType, itemType);

			MethodInfo getComparer = 
				((Func<Ditto.Comparer<int>>)
					Ditto.GetComparer<int>
				).Method.GetGenericMethodDefinition()
				.MakeGenericMethod(itemType);
			

			var sourceEx = Expression.Parameter(type, "source");

			// Ditto.GetComparer<>().DeepClone
			Expression selectorEx = Expression.Field(
				Expression.Call(getComparer),
				"DeepClone");

			// Enumerable.ToList( Enumerable.Select( source, {selector}) ) 
			Expression invokeEx = Expression.Call(
				toListMethod,
				Expression.Call(
					selectMethod,
					sourceEx,
					selectorEx));

			// return source == null ? null : ({type}){...}
			invokeEx = Expression.Condition(
				Expression.Equal(sourceEx, Expression.Constant(null, type)),
				Expression.Constant(null, type),
				Expression.Convert(invokeEx, type));

			var lambda = Expression.Lambda<Func<T, T>>(invokeEx, sourceEx);
			deepClone = lambda.Compile();
			
			return true;
		}

		static Func<T, T> GetStructCloneMethod<T>()
		{
			// is this overkill or not enough?
			return source => { T destination = source; return destination; };
		}
	}
}
