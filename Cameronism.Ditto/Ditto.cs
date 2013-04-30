using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cameronism
{
	public static class Ditto
	{
		#region static comparer methods
		public static T DeepClone<T>(T obj)
		{
			return GetComparer<T>().DeepClone(obj);
		}
		
		public static int GetHashCode<T>(T obj)
		{
			return GetComparer<T>().GetHashCode(obj);
		}
		
		public static bool Equals<T>(T a, T b)
		{
			return GetComparer<T>().Equals(a, b);
		}
		#endregion

		static readonly Dictionary<Type, object> _Cache = new Dictionary<Type, object>();

		internal static readonly MutableTypeCache _MutableTypeCache = new MutableTypeCache();
		public static bool IsMutable(Type type)
		{
			return _MutableTypeCache.IsMutable (type);
		}

		public class Comparer<T> : IEqualityComparer<T>
		{
			public readonly Func<T, T> DeepClone;
			readonly Func<T, int> getHashCode;
			readonly Func<T, T, bool> equals;

			internal Comparer()
			{
				Delegates.Create(out DeepClone, out getHashCode, out equals);
			}
		
			public bool Equals(T a, T b)
			{
				return equals(a, b);
			}
			
			public int GetHashCode(T obj)
			{
				return getHashCode(obj);
			}
		}
		
		public static Comparer<T> GetComparer<T>()
		{
			Comparer<T> comparer;
			lock (_Cache)
			{
				object oComparer;
				if (_Cache.TryGetValue(typeof(T), out oComparer))
				{
					comparer = (Comparer<T>)oComparer;
				}
				else
				{
					comparer = new Comparer<T>();
					_Cache.Add(typeof(T), comparer);
				}
			}
			return comparer;
		}
	}
}
