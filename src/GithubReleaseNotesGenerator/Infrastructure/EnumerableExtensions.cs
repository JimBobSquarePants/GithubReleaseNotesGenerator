using System;
using System.Collections.Generic;

namespace GithubReleaseNotesGenerator.Infrastructure
{
    /// <summary>
    /// Extension methods for enumerable types.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Returns all distinct elements of the given source, where "distinctness"
        /// is determined via a projection.
        /// </summary>
        /// <typeparam name="TSource">The type of the source sequence.</typeparam>
        /// <typeparam name="TKey">The type of the projected element.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">The projection for determining "distinctness".</param>
        /// <returns></returns>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var knownKeys = new HashSet<TKey>();
            foreach (var element in source)
            {
                if (knownKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}
