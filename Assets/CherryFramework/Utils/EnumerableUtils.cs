using System;
using System.Collections.Generic;

namespace CherryFramework.Utils
{
    public static class EnumerableUtils
    {
        public static TEnum[] GetAllEnumValues<TEnum>(this TEnum _) where TEnum : Enum
            => GetAllEnumValues<TEnum>();

        public static TEnum[] GetAllEnumValues<TEnum>() where TEnum : Enum
            => (TEnum[]) Enum.GetValues(typeof(TEnum));

        public static bool InRange<T>(this IReadOnlyCollection<T> source, int? index)
            => index.HasValue && source != null && index >= 0 && index < source.Count;

        /// <summary>
        /// Projects each element of a sequence into a new form, including only elements
        /// for which the selector returns a valid result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="source">The sequence of values to process.</param>
        /// <param name="selector">
        /// A transform function that takes an input element and returns a tuple containing:
        /// a boolean indicating whether to include the result, and the projected result.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> whose elements are the result of invoking the transform
        /// function on each element of the input sequence for which the function returns true.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="source"/> or <paramref name="selector"/> is null.
        /// </exception>
        public static IEnumerable<TResult> SelectWhere<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, (bool isValid, TResult result)> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            foreach (var item in source)
            {
                var (isValid, result) = selector(item);
                if (isValid)
                    yield return result;
            }
        }

        /// <summary>
        /// Projects each element of a sequence into a new form by incorporating the element's index,
        /// including only elements for which the selector returns a valid result.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of the result elements.</typeparam>
        /// <param name="source">The sequence of values to process.</param>
        /// <param name="selector">
        /// A transform function that takes an input element and its index, and returns a tuple containing:
        /// a boolean indicating whether to include the result, and the projected result.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> whose elements are the result of invoking the transform
        /// function on each element of the input sequence (with index) for which the function returns true.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="source"/> or <paramref name="selector"/> is null.
        /// </exception>
        public static IEnumerable<TResult> SelectWhere<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, int, (bool isValid, TResult result)> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            var index = 0;
            foreach (var item in source)
            {
                var (isValid, result) = selector(item, index);
                index++;
                if (isValid)
                    yield return result;
            }
        }

        public static bool TryGet<T>(this IEnumerable<T> source, Predicate<T> predicate, out T result)
        {
            if (source == null)
            {
                result = default;
                return false;
            }

            foreach (var obj in source)
            {
                if (!predicate(obj))
                    continue;

                result = obj;
                return true;
            }

            result = default;
            return false;
        }

        public static int IndexOf<T>(this IEnumerable<T> source, T value)
        {
            var index = 0;
            var comparer = EqualityComparer<T>.Default;

            foreach (var item in source)
            {
                if (comparer.Equals(item, value))
                    return index;
                index++;
            }

            return -1;
        }

        public static int IndexOf<T>(this IEnumerable<T> source, Predicate<T> predicate)
        {
            var index = 0;
            foreach (var item in source)
            {
                if (predicate(item))
                    return index;

                index++;
            }

            return -1;
        }

        public static int IndexOfLast<T>(this IEnumerable<T> source, T value)
        {
            var index = 0;
            var comparer = EqualityComparer<T>.Default;
            var bestIndex = -1;
            foreach (var item in source)
            {
                if (comparer.Equals(value, item))
                    bestIndex = index;

                index++;
            }

            return bestIndex;
        }

        public static void Each<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var obj in source)
                action(obj);
        }

        public static void Each<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            var index = 0;
            foreach (var obj in source)
                action(obj, index++);
        }
    }
}