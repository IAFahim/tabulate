// <copyright file="TypeSearchProvider.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Search
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.Internal;
    using BovineLabs.Core.Utility;
    using UnityEditor;
    using UnityEditor.Search;

    public static class TypeSearchProvider
    {
        public const string SearchProviderType = "typeProvider";

        private static TypeDescriptor[]? descriptors;

        private static QueryEngine<TypeDescriptor>? queryEngine;

        private static QueryEngine<TypeDescriptor> QueryEngine => queryEngine ??= SetupQueryEngine();

        [SearchItemProvider]
        private static SearchProvider CreateProvider()
        {
            return new SearchProvider(SearchProviderType, "Types")
            {
                filterId = "tt:",
                isExplicitProvider = true,
                active = true,
                showDetails = true,
                fetchItems = FetchItems,
                fetchPropositions = FetchPropositions,
                toObject = (_, _) => null, // Types are not UnityEngine.Object instances
            };
        }

        private static IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            foreach (var p in SearchBridge.GetPropositions(QueryEngine))
            {
                yield return p;
            }

            foreach (var l in SearchBridge.GetPropositionsFromListBlockType(typeof(InheritTypeBlock)))
            {
                yield return l;
            }
        }

        private static QueryEngine<TypeDescriptor> SetupQueryEngine()
        {
            var query = new QueryEngine<TypeDescriptor>();
            query.SetSearchDataCallback(GetWords);

            SearchBridge
                .SetFilter(query, "n", data => data.Name, new[] { "=", ":" })
                .AddOrUpdateProposition(category: null, label: "Name", replacement: "n:Name", help: "Search Entry by Type Name");

            query.AddFilter<string>("inherit", OnTypeFilter, /*Transformer,*/ new[] { "=", ":" });
            query.TryGetFilter("inherit", out var inherit);
            inherit.AddOrUpdateProposition(category: null, label: "Inherit", replacement: "inherit:", help: "Search Entry by Inheritance");

            return query;
        }

        private static bool OnTypeFilter(TypeDescriptor descriptor, string operatorToken, string filterValue)
        {
            var type = Type.GetType(filterValue); // this is awful but i can't seem to figure it out
            return type != null && type.IsAssignableFrom(descriptor.Type);
        }

        private static IEnumerable<SearchItem> FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            var searchQuery = context.searchQuery;
            var score = 0;

            ParsedQuery<TypeDescriptor>? query = null;

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = QueryEngine.ParseQuery(context.searchQuery);
                if (!query.valid)
                {
                    query = null;
                }
            }

            descriptors ??= ReflectionUtility.GetAllImplementations<UnityEngine.Object>().Select(s => new TypeDescriptor(s)).ToArray();

            foreach (var data in query?.Apply(descriptors) ?? descriptors)
            {
                yield return provider.CreateItem(context, data.SimplifiedQualifiedName, score++, data.SimplifiedQualifiedName, null, null, data.SimplifiedQualifiedName);
            }
        }

        [MenuItem("Window/Search/Type", priority = 1392)]
        private static void OpenProviderMenu()
        {
            OpenProvider();
        }

        private static void OpenProvider()
        {
            SearchService.ShowContextual(SearchProviderType);
        }

        private static IEnumerable<string> GetWords(TypeDescriptor desc)
        {
            yield return desc.Name;
        }

        [QueryListBlock("Inherit", "inherit", "inherit")]
        private class InheritTypeBlock : QueryListBlock
        {
            public InheritTypeBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
                : base(source, id, value, attr)
            {
            }

            public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags = SearchPropositionFlags.None)
            {
                var c = flags.HasFlag(SearchPropositionFlags.NoCategory) ? null : this.category;

                foreach (var type in ReflectionUtility.GetAllImplementations<UnityEngine.Object>())
                {
                    var simplifiedName = $"{type.FullName}, {type.Assembly.GetName().Name}";

                    yield return new SearchProposition(c, simplifiedName, simplifiedName, type: this.GetType(), data: simplifiedName);
                }
            }
        }

        internal readonly struct TypeDescriptor
        {
            public TypeDescriptor(Type type)
            {
                this.Type = type;
                this.Name = type.Name;

                var namespacePath = string.IsNullOrEmpty(type.Namespace) ? "Global" : type.Namespace.Replace('.', '/');
                this.DisplayName = $"{namespacePath}/{type.Name}";
                this.SimplifiedQualifiedName = $"{type.FullName}, {type.Assembly.GetName().Name}";
            }

            public Type Type { get; }

            public string Name { get; }

            public string DisplayName { get; }

            public string SimplifiedQualifiedName { get; }
        }
    }
}