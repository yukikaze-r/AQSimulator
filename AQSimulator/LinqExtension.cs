using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AQSimulator {
	public static class LinqExtension {
		public static void ForEach<T>(this IEnumerable<T> source, System.Action<T> action) {
			foreach (T item in source) action(item);
		}

		public static void ForEach<T>(this IEnumerable<T> source, System.Action<T, int> action) {
			int i = 0;
			foreach (T item in source) action(item, i++);
		}

		public static IList<T> WhereMin<T, V>(this IEnumerable<T> source, System.Func<T, V> eval) where V : IComparable<V> {
			return WhereMinMax(source, eval, true);
		}

		public static IList<T> WhereMax<T, V>(this IEnumerable<T> source, System.Func<T, V> eval) where V : IComparable<V> {
			return WhereMinMax(source, eval, false);
		}

		private static IList<T> WhereMinMax<T, V>(this IEnumerable<T> source, System.Func<T, V> eval, bool isMin) where V : IComparable<V> {
			List<T> ret = null;
			T maxElem = default(T);
			V maxVal = default(V);
			var enumerator = source.GetEnumerator();

			if (enumerator.MoveNext()) {
				maxElem = enumerator.Current;
				maxVal = eval(maxElem);
			} else {
				return new T[] { };
			}

			if (isMin) {
				while (enumerator.MoveNext()) {
					var curElem = enumerator.Current;
					var curVal = eval(curElem);
					int comp = curVal.CompareTo(maxVal);
					// 圧倒的に一番よくあるケースなので、早めに判定したい
					if (comp > 0) {
						continue;
					}

					if (comp < 0) {
						ret = null;
						maxElem = curElem;
						maxVal = curVal;
					} else { // comp == 0
						if (ret == null) {
							ret = new List<T>();
							ret.Add(maxElem);
						}

						ret.Add(curElem);
					}
				}
			} else {
				while (enumerator.MoveNext()) {
					var curElem = enumerator.Current;
					var curVal = eval(curElem);
					int comp = curVal.CompareTo(maxVal);
					if (comp < 0) {
						continue;
					}

					if (comp > 0) {
						ret = null;
						maxElem = curElem;
						maxVal = curVal;
					} else { // comp == 0
						if (ret == null) {
							ret = new List<T>();
							ret.Add(maxElem);
						}

						ret.Add(curElem);
					}
				}
			}

			return ret != null ? ret : (IList<T>)new T[] { maxElem };
		}

		public static bool IsEqual<T>(this IEnumerable<T> a, IEnumerable<T> b) {
			var aEnum = a.GetEnumerator();
			var bEnum = b.GetEnumerator();
			bool aOk, bOk;
			while ((aOk = aEnum.MoveNext()) & (bOk = bEnum.MoveNext())) {
				if (!aEnum.Current.Equals(bEnum.Current)) {
					return false;
				}
			}

			return !(aOk ^ bOk);
		}

		public static T SafeFirstOrDefault<T>(this IEnumerable<T> source) {
			IEnumerator<T> enumerator = source.GetEnumerator();
			if (enumerator.MoveNext()) {
				return enumerator.Current;
			} else {
				return default(T);
			}
		}

		// 適当に作りました
		public static T SafeFirstOrDefault<T>(this IEnumerable<T> source, Func<T, bool> func) {

			foreach (T t in source) {
				if (func(t)) {
					return t;
				}
			}

			return default(T);
		}

		public static int SafeIntSum<T>(this IEnumerable<T> source, Func<T, int> func) {
			int result = 0;
			foreach (T t in source) {
				result += func(t);
			}

			return result;
		}


		public static long SafeLongSum<T>(this IEnumerable<T> source, Func<T, long> func) {
			long result = 0;
			foreach (T t in source) {
				result += func(t);
			}

			return result;
		}

		public static IEnumerable<T> SafeSelect<T, U>(this IEnumerable<U> source, Func<U, T> func) {
			foreach (U u in source) {
				yield return func(u);
			}
		}

		// http://extensionmethod.net/csharp/ienumerable-t/indexof-t

		/// <summary>
		/// Returns the index of the first occurrence in a sequence by using the default equality comparer.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of source.</typeparam>
		/// <param name="list">A sequence in which to locate a value.</param>
		/// <param name="value">The object to locate in the sequence</param>
		/// <returns>The zero-based index of the first occurrence of value within the entire sequence, if found; otherwise, –1.</returns>
		public static int IndexOf<TSource>(this IEnumerable<TSource> list, TSource value) where TSource : IEquatable<TSource> {
			return list.IndexOf<TSource>(value, EqualityComparer<TSource>.Default);

		}

		/// <summary>
		/// Returns the index of the first occurrence in a sequence by using a specified IEqualityComparer.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of source.</typeparam>
		/// <param name="list">A sequence in which to locate a value.</param>
		/// <param name="value">The object to locate in the sequence</param>
		/// <param name="comparer">An equality comparer to compare values.</param>
		/// <returns>The zero-based index of the first occurrence of value within the entire sequence, if found; otherwise, –1.</returns>
		public static int IndexOf<TSource>(this IEnumerable<TSource> list, TSource value, IEqualityComparer<TSource> comparer) {
			int index = 0;
			foreach (var item in list) {
				if (comparer.Equals(item, value)) {
					return index;
				}
				index++;
			}
			return -1;
		}

		private static IEnumerable<T> Take<T>(IEnumerator<T> enumerator, int count) {
			while (--count >= 0 && enumerator.MoveNext()) {
				yield return enumerator.Current;
			}
		}

		public static IEnumerable<IEnumerable<T>> Divide<T>(this ICollection<T> collection, int count) {
			int c = collection.Count / count;
			int r = collection.Count - c * count;

			using (var enumerator = collection.GetEnumerator()) {
				for (int i = 0; i < c; i++) {
					yield return Take(enumerator, count);
				}
				if (r > 0) {
					yield return Take(enumerator, r);
				}
			}
		}
	}
}