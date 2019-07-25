﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using static Microsoft.DotNet.Interactive.Rendering.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Rendering
{
    public class HtmlFormatter<T> : TypeFormatter<T>
    {
        public HtmlFormatter(Action<T, TextWriter> format) : base("text/html", format)
        {
        }

        public static HtmlFormatter<T> Create(bool includeInternals = false)
        {
            if (typeof(IEnumerable).IsAssignableFrom(typeof(T)))
            {
                return CreateForSequence(includeInternals);
            }

            return CreateForObject(includeInternals);
        }

        private static HtmlFormatter<T> CreateForObject(bool includeInternals)
        {
            var members = typeof(T).GetAllMembers(includeInternals)
                                   .GetMemberAccessors<T>();

            return new HtmlFormatter<T>((instance, writer) =>
            {
                IEnumerable<object> headers = members.Select(m => m.Member.Name)
                                                     .Select(v => th(v));

                IEnumerable<object> values = members.Select(m => Value(m, instance))
                                                    .Select(v => td(v));

                var t =
                    table(
                        thead(
                            tr(
                                headers)),
                        tbody(
                            tr(
                                values)));

                ((PocketView) t).WriteTo(writer, HtmlEncoder.Default);
            });
        }

        private static HtmlFormatter<T> CreateForSequence(bool includeInternals)
        {
            IDestructurer destructurer = null;

            if (typeof(T).IsConstructedGenericType)
            {
                var dictionaryType = typeof(T).GetInterfaces()
                                              .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

                if (dictionaryType != null)
                {
                    var itemType = dictionaryType.GenericTypeArguments.ElementAt(1);
                    destructurer = Destructurer.Create(itemType);
                }

                if (destructurer == null)
                {
                    var ienumerableType = typeof(T).GetInterfaces()
                                                   .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

                    if (ienumerableType != null)
                    {
                        var itemType = ienumerableType.GenericTypeArguments.Single();

                        destructurer = Destructurer.Create(itemType);
                    }
                }
            }

            return new HtmlFormatter<T>((instance, writer) =>
            {
                var i = 0;

                IEnumerable sequence = instance switch {
                                           IDictionary d => d.Values,
                                           IEnumerable s => s,
                                           _ => throw new ArgumentException($"{instance.GetType()} is not IEnumerable")
                                           };

                Func<string> getIndex;

                if (instance is IDictionary dict)
                {
                    var keys = new string[dict.Keys.Count];
                    dict.Keys.CopyTo(keys, 0);
                    getIndex = () => keys[i];
                }
                else
                {
                    getIndex = () => i.ToString();
                }

                var rows = new List<IHtmlContent>();
                List<IHtmlContent> headers = null;

                foreach (var item in sequence)
                {
                    var dictionary = (destructurer ?? Destructurer.Create(item.GetType())).Destructure(item);

                    if (headers == null)
                    {
                        headers = new List<IHtmlContent>();
                        headers.Add(th);
                        headers.AddRange(dictionary.Keys
                                                   .Select(k => (IHtmlContent) th(k))
                        );
                    }

                    var cells =
                        new IHtmlContent[]
                            {
                                td(getIndex().ToHtmlContent())
                            }
                            .Concat(
                                dictionary
                                    .Values
                                    .Select(v => (IHtmlContent) td(v)));
                    rows.Add(
                        tr(
                            cells));

                    i++;
                }

                var view = Table(headers, rows);

                view.WriteTo(writer, HtmlEncoder.Default);
            });
        }

        private static PocketView Table(List<IHtmlContent> headers, List<IHtmlContent> rows)
        {
            var t = table(
                thead(
                    tr(
                        headers)),
                tbody(
                    rows));

            return t;
        }

        private static string Value(MemberAccessor<T> m, T instance)
        {
            try
            {
                var value = m.GetValue(instance);
                return value.ToDisplayString();
            }
            catch (Exception exception)
            {
                return exception.ToDisplayString(PlainTextFormatter.MimeType);
            }
        }
    }
}