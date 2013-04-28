using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;

namespace Cameronism
{
	internal class MutableTypeCache
	{
		readonly Dictionary<Type, bool> _Immutable = new Dictionary<Type, bool>
		{
			{ typeof(Boolean), true },
			{ typeof(Char), true },
			{ typeof(SByte), true },
			{ typeof(Byte), true },
			{ typeof(Int16), true },
			{ typeof(UInt16), true },
			{ typeof(Int32), true },
			{ typeof(UInt32), true },
			{ typeof(Int64), true },
			{ typeof(UInt64), true },
			{ typeof(Single), true },
			{ typeof(Double), true },
			{ typeof(Decimal), true },
			{ typeof(DateTime), true },
			{ typeof(String), true },
		};
		readonly ReaderWriterLockSlim _Lock = new ReaderWriterLockSlim();


		public bool IsMutable(Type type)
		{
			bool result;
			_Lock.EnterReadLock ();
			try
			{
				if (_Immutable.TryGetValue(type, out result)) return !result;
			}
			finally
			{
				_Lock.ExitReadLock();
			}

			return IsMutable (type, null) == true;
		}


		private bool? IsMutable(Type type, List<Type> path)
		{
			// TODO figure out a way to not consider all interfaces mutable
			if (type.IsArray || type.IsInterface) {
				return SetValue(type, true);
			}

			if (type.IsEnum) {
				return SetValue(type, false);
			}

			if (path != null && path.Contains (type))
				return null;

			bool? result = null;
			var memberTypes = new List<Type> ();

			foreach (var fi in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
				// mutable field
				if (!fi.IsInitOnly) {
					result = true;
					break;
				}

				if (fi.FieldType != type) {
					memberTypes.Add(fi.FieldType);
				}
			}

			// no members -- immutable
			if (result == null && memberTypes.Count == 0) {
				result = false;
			}

			if (result != null) {
				return SetValue(type, result.GetValueOrDefault ());
			}

			// check if any members are known to be mutable
			_Lock.EnterReadLock ();
			try
			{
				bool immutable;
				for (int i = 0; i < memberTypes.Count; i++) {
					if (_Immutable.TryGetValue(memberTypes[i], out immutable)) {
						if (!immutable) {
							// a member is mutable
							result = true;
							break;
						}

						// do not recurse for this member
						memberTypes[i] = null;
					}
				}
			}
			finally
			{
				_Lock.ExitReadLock();
			}

			// check if we figured it out
			if (result != null) {
				return SetValue(type, result.GetValueOrDefault());
			}

			// now we recurse
			path = path == null ? new List<Type> () : new List<Type> (path);
			path.Add (type);
			foreach (var memberType in memberTypes) {
				if (memberType != null && IsMutable(memberType, path) == true) {
					result = true;
					break;
				}
			}

			if (result == null) {
				// we didn't find anything mutable
				result = false;
			}

			return SetValue (type, result.GetValueOrDefault ());
		}
		
		private bool SetValue(Type type, bool isMutable)
		{
			_Lock.EnterWriteLock ();
			try
			{
				_Immutable[type] = !isMutable;
			}
			finally
			{
				_Lock.ExitWriteLock();
			}
			return isMutable;
		}
	}
}