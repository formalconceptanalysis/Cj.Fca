using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Cj.Fca
{
    /// <summary>
    /// Represents a binary formal context document. A formal context consists of objects and attributes and a incidence relation
    /// between objects and attributes. For the structure and usage see context markup description (context.xsd).
    /// </summary>
    public partial class Context
    {
        /// <summary>
        /// Defines the kind of context items for the well defined context document.
        /// </summary>
        public enum ItemKind : byte
        {
            ///<summary>Set is interpreted as an object or attribute belonging to given item.</summary>
            Generic = 0,

            ///<summary>Empty set is interpreted as an attribute item.</summary>
            Attribute = 1,

            ///<summary>Empty set is interpreted as an object item.</summary>
            Object = 2
        }

        /// <summary>
        /// Context manipulations which do not alter the structure of a concept lattice.
        /// </summary>
        [Flags]
        public enum Status : int
        {
            /// <summary>
            /// The context status is unknown or not defined.
            /// </summary>
            None = 0,

            /// <summary>
            /// The context is clarified by objects that means there are no duplicates of objects with the same intent.
            /// </summary>
            ClarifiedByObject = 1,

            /// <summary>
            /// The context is clarified by attributes that means there are no duplicates of attributes with the same extent.
            /// </summary>
            ClarifiedByAttribute = 2,

            /// <summary>
            /// The context is clarified by objects and attributes.
            /// </summary>
            Clarified = ClarifiedByObject | ClarifiedByAttribute,

            /// <summary>
            /// The context is clarified, i. e. purified by an object that has all attributes.
            /// </summary>
            PurifiedByObject = 4,

            /// <summary>
            /// The context is clarified, i. e. purified by an attribute that belongs to all objects.
            /// </summary>
            PurifiedByAttribute = 8,

            /// <summary>
            /// The context is clarified, i. e. purified by so called full rows and columns of a binary context.
            /// </summary>
            Purified = PurifiedByObject | PurifiedByAttribute,

            /// <summary>
            /// The context is reduced by reducible objects.
            /// </summary>
            ReducedByObject = 16,

            /// <summary>
            /// The context is reduced by reducible attributes.
            /// </summary>
            ReducedByAttribute = 32,

            /// <summary>
            /// The context is reduced by reducible objects and reducible attributes.
            /// </summary>
            Reduced = ReducedByObject | ReducedByAttribute,

            /// <summary>
            /// The context is clarified, purifed and reduced.
            /// </summary>
            Standard = Clarified | Purified | Reduced
        }

        /// <summary>
        /// 
        /// </summary>
        public class BinaryItem
        {
            /// <summary>
            /// Is true if there is no row label defined and all row index values are treated as label.
            /// </summary>
            public static bool HorizontalRightAlignmentByRowLabelEnabled { get; set; } = false;

            /// <summary>
            /// Is true if there is a column label greater than one character. 
            /// </summary>
            public static bool VerticalLeftRotationByColumnLabelEnabled { get; set; } = false;

            /// <summary>
            /// Is null if there are not all row memos defined, otherwise false.
            /// </summary>
            public static bool? HorizontalRightAlignmentByRowMemoEnabled { get; set; } = null;

            /// <summary>
            /// Is null if there are not all column memos defined and true if there is any column memo greater than one character, otherwise false. 
            /// </summary>
            public static bool? VerticalLeftRotationByColumnMemoEnabled { get; set; } = null;

            /// <summary>
            /// Status of binary context.
            /// </summary>
            public static Status Status { get; set; } = Status.None;

            /// <summary>
            /// Title of formal context definition. The title is mandatory according to XSD format specification that is defined by <see cref="Cj.Fca.Context.XsdMarkup"/> as an embedded resource (Context.xsd).
            /// </summary>
            public static string Title { get; set; } = null;

            /// <summary>
            /// Incidence relation is defined by row and column index.
            /// </summary>
            public class Incidence
            {
                /// <summary>
                /// The index of column.
                /// </summary>
                public int Column { get; set; }

                /// <summary>
                /// The index of row.
                /// </summary>
                public int Row { get; set; }
            }

            /// <summary>
            /// Row and column label of incidence releation.
            /// </summary>
            public class Labels
            {
                /// <summary>
                /// Column labels are defined by attribute labels.
                /// </summary>
                public string Column { get; set; }

                /// <summary>
                /// Row labels are defined by object labels.
                /// </summary>
                public string Row { get; set; }
            }

            /// <summary>
            /// Row and column memo of incidence releation.
            /// </summary>
            public class Memos
            {
                /// <summary>
                /// Column memos are defined by attribute memos.
                /// </summary>
                public string Column { get; set; }

                /// <summary>
                /// Row memos are defined by object memos.
                /// </summary>
                public string Row { get; set; }
            }

            /// <summary>
            /// Contains clarified, purified or reduced items.
            /// </summary>
            public class RedundancyItems
            {
                /// <summary>
                /// List of attributes of given context.
                /// </summary>
                public string[] Attributes { get; set; }

                /// <summary>
                /// List of objects of given context.
                /// </summary>
                public string[] Objects { get; set; }
            }

            /// <summary>
            /// Pair of column and row index.
            /// </summary>
            public Incidence Index { get; set; }

            /// <summary>
            /// Pair of column and row memo.
            /// </summary>
            public Memos Memo { get; set; }

            /// <summary>
            /// Pair of column and row label.
            /// </summary>
            public Labels Label { get; set; }

            /// <summary>
            /// This property can be used to change the effective label by data bindings. The default is set to the label property.
            /// </summary>
            public Labels LabelView { get; set; }

            /// <summary>
            /// Pair of clarified items.
            /// </summary>
            public RedundancyItems ClarifiedBy { get; set; }

            /// <summary>
            /// The binary context value of an item.
            /// </summary>
            public bool Value { get; set; }

            /// <summary>
            /// The arrow relation indicator in the range of -1, 0, +1, and null (non applicable). 
            /// </summary>
            public int? ArrowRelation { get; set; }
        }

        /// <summary>
        /// Set degree of parallelism to be used to find concepts concurrently.
        /// </summary>
        public static int MaxDegreeOfParallelism = -1;

        /// <summary>
        /// This static function validates the given XML file against an XSD format specification that is defined by <see cref="Cj.Fca.Context.XsdMarkup"/> as an embedded resource (Context.xsd).
        /// </summary>
        /// <param name="Protocol">Contains error messages if format errors are detected.</param>
        /// <param name="XmlFile">XML file to be checked.</param>
        /// <returns>True if there are no errors, otherwise false. Errors can be looked up in the protocol back from this procedure.</returns>
        public static bool Validate(out string Protocol, string XmlFile)
        {
            Protocol = string.Empty;

            var Ctx = new Context(out Protocol, XmlFile);

            return string.IsNullOrEmpty(Protocol) ? true : false;
        }

        /// <summary>
        /// This static function validates the given XML file against an XSD format specification that is defined by <see cref="Cj.Fca.Context.XsdMarkup"/> as an embedded resource (Context.xsd).
        /// </summary>
        /// <param name="Protocol">Contains error messages if format errors are detected.</param>
        /// <param name="XmlData">XML data to be checked.</param>
        /// <returns>True if there are no errors, otherwise false. Errors can be looked up in the protocol back from this procedure.</returns>
        public static bool Validate(out string Protocol, XDocument XmlData)
        {
            Protocol = string.Empty;

            return new Context(out Protocol, XmlData) != null && string.IsNullOrEmpty(Protocol) ? true : false;
        }

        /// <summary>
        /// Creates a standard context with two elements.
        /// </summary>
        /// <returns>A context document that contains a two dimensional data structure.</returns>
        public static XDocument CreateStandardContextWithTwoElements()
        {
            /*
               An illustration of the given standard formal context with two elements of objects (1, 2) and attributes (a, b):

               | - || a || b |
               | 1 || X ||   |
               | 2 ||   || X |
            */

            return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement(

            new XElement("Data", new XAttribute("Format", "1.0.0.0"),
            new XElement("Header", new XElement("Title", "Sample"), new XElement("Subtitle", "Standard context with two elements.")),
            new XElement("Context", new XAttribute("Status", "Standard"),

            new XElement("Declarations",

            new XElement("Attributes",
            new XElement("Attribute", new XAttribute("Index", "1"), new XAttribute("Label", "a")),
            new XElement("Attribute", new XAttribute("Index", "2"), new XAttribute("Label", "b"))),

            new XElement("Objects",
            new XElement("Object", new XAttribute("Index", "1")),
            new XElement("Object", new XAttribute("Index", "2")))

            ), new XElement("Items",

            new XComment("1st row"),
            new XElement("Item", new XElement("Object", new XAttribute("Index", "1")), new XElement("Attribute", new XAttribute("Index", "1")), new XElement("Value", new XText(bool.TrueString.ToLower()))),
            new XElement("Item", new XElement("Object", new XAttribute("Index", "1")), new XElement("Attribute", new XAttribute("Index", "2")), new XElement("Value", new XText(bool.FalseString.ToLower()))),
            new XComment("2nd row"),
            new XElement("Item", new XElement("Object", new XAttribute("Index", "2")), new XElement("Attribute", new XAttribute("Index", "1")), new XElement("Value", new XText(bool.FalseString.ToLower()))),
            new XElement("Item", new XElement("Object", new XAttribute("Index", "2")), new XElement("Attribute", new XAttribute("Index", "2")), new XElement("Value", new XText(bool.TrueString.ToLower())))

            )))));
        }

        /// <summary>
        /// Converts protocol strings to simple html page with fixed font face set to Courier New.
        /// </summary>
        /// <param name="Protocol">Text to be converted.</param>
        /// <param name="Title">Title of html page.</param>
        /// <param name="Wrap">Determines if text should be wrapped.</param>
        /// <returns>Html text that can be shown by a browser.</returns>
        public static string[] ToHtml(string[] Protocol, string Title = "", bool Wrap = false)
        {
            #region HTML // http://www.w3schools.com

            var ToHtml = new List<string>(Protocol);

            for (int i = 0; i < ToHtml.Count; i++)
                ToHtml[i] = ToHtml[i] + "<br>";

            ToHtml.Insert(0, "<html>");
            ToHtml.Insert(1, "<head>");
            ToHtml.Insert(2, "<title>");
            ToHtml.Insert(3, string.IsNullOrEmpty(Title) ? "" : Title);
            ToHtml.Insert(4, "</title>");
            ToHtml.Insert(5, "</head>");
            ToHtml.Insert(6, "<font face=\"Courier New\">");

            if (!Wrap)
                ToHtml.Insert(7, "<body style=\"white-space:nowrap;\">");

            ToHtml.Add("</body>");
            ToHtml.Add("</font>");
            ToHtml.Add("</html>");

            #endregion

            return ToHtml.ToArray();
        }

        public static string[] ToJson(Context.BinaryItem[,] BinaryContext)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Serializes an XML vector to a string representation. It can be used to convert extents and/or intents to a string object.
        /// </summary>
        /// <see cref="ToHtml(string[], string, bool)"/>
        /// <param name="Items">Items to be converted to string representation.</param>
        /// <param name="WithLabel">True if label should be written instead of index.</param>
        /// <returns>Serialized XML item to string.</returns>
        public static string ToString(XElement[] Items, bool WithLabel = false)
        {
            var ItemString = new StringBuilder();

            ItemString.Append("{");

            foreach (var Item in Items)
            {
                if (WithLabel && Item.Attribute("Label") != null)
                    ItemString.Append($"{Item.Attribute("Label").Value},");
                else
                    ItemString.Append($"{Item.Attribute("Index").Value},");
            }

            if (ItemString.ToString().Last() == ',')
                ItemString.Remove(ItemString.Length - 1, 1);

            ItemString.Append("}");

            return ItemString.ToString();
        }

        /// <summary>
        /// Serializes an implication to a string representation. An implication consists of two sets where A' is a subset ob B'.
        /// </summary>
        /// <param name="Implication">Implication to be converted to string representation.</param>
        /// <param name="WithSupport">Offers the fraction of objects where the intents which contain the union of A and B.</param>
        /// <returns>Serialized XML tuple to string.</returns>
        public static string ToString((XElement[] Left, XElement[] Right, XElement[] Support) Implication, bool WithSupport = false)
        {
            var sImplication = new StringBuilder();

            if (WithSupport)
                sImplication.Append($"<{Implication.Support.Length}> {Context.ToString(Implication.Support)}: ");

            if (Implication.Left != null && Implication.Right != null)
            {
                if (WithSupport)
                    sImplication.Append(" ");

                sImplication.Append("{");

                foreach (var Item in Implication.Left)
                    if (Item.Attribute("Label") != null)
                        sImplication.Append($"{Item.Attribute("Label").Value},");
                    else
                        sImplication.Append($"{Item.Attribute("Index").Value},");

                if (sImplication.ToString().Last() == ',')
                    sImplication.Remove(sImplication.Length - 1, 1);

                sImplication.Append("} => {");

                foreach (var Item in Implication.Right)
                    if (Item.Attribute("Label") != null)
                        sImplication.Append($"{Item.Attribute("Label").Value},");
                    else
                        sImplication.Append($"{Item.Attribute("Index").Value},");

                if (sImplication.ToString().Last() == ',')
                    sImplication.Remove(sImplication.Length - 1, 1);

                if (2 <= sImplication.Length)
                    sImplication.Append("}");
            }

            if (Implication.Left != null && Implication.Right == null)
            {
                if (WithSupport)
                    sImplication.Append(" ");

                sImplication.Append("{");

                foreach (var Item in Implication.Left)
                    if (Item.Attribute("Label") != null)
                        sImplication.Append($"{Item.Attribute("Label").Value},");
                    else
                        sImplication.Append($"{Item.Attribute("Index").Value},");

                if (sImplication.ToString().Last() == ',')
                    sImplication.Remove(sImplication.Length - 1, 1);

                sImplication.Append("} => ⊥");
            }

            return sImplication.ToString();
        }

        /// <summary>
        /// Creates a valid context document.
        /// </summary>
        /// <param name="XmlFile">XML file to be read.</param>
        public Context(string XmlFile)
        {
            if (!string.IsNullOrEmpty(XmlFile) && System.IO.File.Exists(XmlFile))
            {
                try
                {
                    ContextDocument = XDocument.Load(XmlFile, LoadOptions.SetBaseUri);

                    if (false == Context.Validate(out _, this))
                        ContextDocument = null;
                }
                catch (Exception) { ContextDocument = null; }
            }
            else
                ContextDocument = null;
        }

        /// <summary>
        /// Creates a valid context document.
        /// </summary>
        /// <param name="XmlData">XML data to be read.</param>
        public Context(XDocument XmlData)
        {
            ContextDocument = XmlData;

            if (false == Context.Validate(out _, this))
                ContextDocument = null;
        }

        /// <summary>
        /// Base uri will be set by constructor if XML file is read.
        /// </summary>
        /// <returns>Document uri belonging to formal context if exists.</returns>
        public Uri DocumentUri() => string.IsNullOrEmpty(ContextDocument?.BaseUri) ? null : new Uri(ContextDocument.BaseUri ?? "");

        /// <summary>
        /// Creates a deep copy of given context document.
        /// </summary>
        /// <returns>Returns a copy of the underlying XML document.</returns>
        public XDocument GetContextDocument() => ContextDocument != null ? new XDocument(ContextDocument) : null;

        /// <summary>
        /// Computes all formal concepts of given context by iteration, i.e., every extent resp. intent is the intersection of
        /// attribute extents resp. object intents.
        /// </summary>
        /// <param name="Kind">The kind parameter is used to start the computation over attributes or objects. If
        /// set to generic, the computation starts with the smallest size of given context items.</param>
        /// <returns>List of concepts according to their order of computation.</returns>
        public async Task<(XElement[] Extent, XElement[] Intent)[]> FindConceptsByIterationAsync(ItemKind Kind = ItemKind.Generic)
        {
            return await Task.Run(() =>
            {
                var Concepts = new List<(XElement[] Extent, XElement[] Intent)>();

                if (Assert() is false)
                    return Concepts.ToArray();

                if (Kind == ItemKind.Attribute || (Kind == ItemKind.Generic && GetAttributes().Length <= GetObjects().Length))
                {
                    Concepts.Add(new ValueTuple<XElement[], XElement[]>(GetObjects(), Prime(GetObjects(), ItemKind.Object)));

                    for (int Index = 1; Index <= GetAttributes().Length; Index++)
                    {
                        XElement[] A = Prime(GetAttributes(Index), ItemKind.Attribute); // attribute extent

                        for (int j = 0; j < Concepts.Count(); j++)
                        {
                            XElement[] Extent = Concepts[j].Extent.OrderBy(x => x.Element("Object"), new XElementComparer()).Intersect(A.OrderBy(x => x.Element("Object"), new XElementComparer()), new XElementEqualityComparer()).OrderBy(x => x.Element("Object"), new XElementComparer()).ToArray();

                            bool Found = false;

                            foreach ((XElement[] Extent, XElement[] Intent) Concept in Concepts)
                            {
                                if (ToBinaryValue(Extent, ItemKind.Object) == ToBinaryValue(Concept.Extent, ItemKind.Object))
                                {
                                    Found = true; break;
                                }
                            }

                            if (Found is false)
                            {
                                Concepts.Add(new ValueTuple<XElement[], XElement[]>(Extent, Prime(Extent, ItemKind.Object)));

                                j = -1;
                            }
                        }
                    }
                }

                if (Kind == ItemKind.Object || (Kind == ItemKind.Generic && GetAttributes().Length > GetObjects().Length))
                {
                    Concepts.Add(new ValueTuple<XElement[], XElement[]>(Prime(GetAttributes(), ItemKind.Attribute), GetAttributes()));

                    for (int Index = 1; Index <= GetObjects().Length; Index++)
                    {
                        XElement[] B = Prime(GetObjects(Index), ItemKind.Object); // object intent

                        for (int j = 0; j < Concepts.Count(); j++)
                        {
                            XElement[] Intent = Concepts[j].Intent.OrderBy(x => x.Element("Attribute"), new XElementComparer()).Intersect(B.OrderBy(x => x.Element("Attribute"), new XElementComparer()), new XElementEqualityComparer()).OrderBy(x => x.Element("Attribute"), new XElementComparer()).ToArray();

                            bool Found = false;

                            foreach ((XElement[] Extent, XElement[] Intent) Concept in Concepts)
                            {
                                if (ToBinaryValue(Intent, ItemKind.Attribute) == ToBinaryValue(Concept.Intent, ItemKind.Attribute))
                                {
                                    Found = true; break;
                                }
                            }

                            if (Found is false)
                            {
                                Concepts.Add(new ValueTuple<XElement[], XElement[]>(Prime(Intent, ItemKind.Attribute), Intent));

                                j = -1;
                            }
                        }
                    }
                }

                return Concepts.ToArray();
            });
        }

        /// <summary>
        /// Computes all formal concepts of given context by naive algorithm, i.e., each item of the power set of
        /// attribute or object items is checked with the help of the derivation operator whether A = A'' is true.
        /// </summary>
        /// <param name="Kind">The kind parameter is used to start the computation over attributes or objects. If
        /// set to generic, the computation starts with the smallest size of given context items.</param>
        /// <returns>Sorted list of concepts.</returns>
        public async Task<(XElement[] Extent, XElement[] Intent)[]> FindConceptsByPowerSetAsync(ItemKind Kind = ItemKind.Generic)
        {
            return await Task.Run(() =>
            {
                var Concepts = new List<(XElement[] Extent, XElement[] Intent)>();

                if (Assert() is false)
                    return Concepts.ToArray();

                if (Kind == ItemKind.Attribute || (Kind == ItemKind.Generic && GetAttributes().Length <= GetObjects().Length))
                {
                    int[][] IntArray = PowerSet(GetAttributes().Length);

                    for (int i = 0; i < IntArray.Length; i++)
                    {
                        XElement[] B = GetAttributes(IntArray[i]);

                        XElement[] A = Prime(B, ItemKind.Attribute);

                        if (B.OrderBy(j => j, new XElementComparer()).SequenceEqual(Prime(A, ItemKind.Object), new XElementEqualityComparer()))
                            Concepts.Add(new ValueTuple<XElement[], XElement[]>(A, B));
                    }
                }

                if (Kind == ItemKind.Object || (Kind == ItemKind.Generic && GetAttributes().Length > GetObjects().Length))
                {
                    int[][] IntArray = PowerSet(GetObjects().Length);

                    for (int i = 0; i < IntArray.Length; i++)
                    {
                        XElement[] A = GetObjects(IntArray[i]);

                        XElement[] B = Prime(A, ItemKind.Object);

                        if (A.OrderBy(j => j, new XElementComparer()).SequenceEqual(Prime(B, ItemKind.Attribute), new XElementEqualityComparer()))
                            Concepts.Add(new ValueTuple<XElement[], XElement[]>(A, B));
                    }
                }

                (XElement[] Extent, XElement[] Intent)[] SortedConcepts = Concepts.ToArray();

                Array.Sort(SortedConcepts, Context.CompareConceptBySize);

                return SortedConcepts;
            });
        }

        /// <summary>
        /// Computes all formal concepts of given context by naive algorithm concurrently, i.e., each item of the power set of
        /// attribute or object items is checked with the help of the derivation operator whether A = A'' is true.
        /// </summary>
        /// <param name="Cancellation">Signals cancellation.</param>
        /// <param name="Kind">The kind parameter is used to start the computation over attributes or objects. If
        /// set to generic, the computation starts with the smallest size of given context items.</param>
        /// <returns>Sorted list of concepts.</returns>
        public async Task<(XElement[] Extent, XElement[] Intent)[]> FindConceptsByPowerSetParallel(CancellationTokenSource Cancellation, ItemKind Kind = ItemKind.Generic)
        {
            return await Task.Run(() =>
            {
                var Concepts = new ConcurrentBag<(XElement[] Extent, XElement[] Intent)>();

                if (Assert() is false)
                    return Concepts.ToArray();

                if (Kind == ItemKind.Attribute || (Kind == ItemKind.Generic && GetAttributes().Length <= GetObjects().Length))
                {
                    int[][] IntArray = PowerSet(GetAttributes().Length);

                    try
                    {
                        Parallel.ForEach(

                            Partitioner.Create(0, IntArray.Length),

                            new ParallelOptions() { MaxDegreeOfParallelism = Context.MaxDegreeOfParallelism, CancellationToken = Cancellation.Token }, () => { return new Context(ContextDocument.Root); },

                            (Range, LoopState, Context) =>
                            {
                                if (Cancellation.IsCancellationRequested)
                                    return Context;

                                for (int i = Range.Item1; i < Range.Item2; i++)
                                {
                                    XElement[] B = Context.GetAttributes(IntArray[i]);

                                    XElement[] A = Context.Prime(B, ItemKind.Attribute);

                                    if (B.OrderBy(j => j, new XElementComparer()).SequenceEqual(Context.Prime(A, ItemKind.Object), new XElementEqualityComparer()))
                                        Concepts.Add(new ValueTuple<XElement[], XElement[]>(A, B));
                                }

                                return Context;

                            }, _ => { }

                        );
                    }
                    catch (OperationCanceledException)
                    {
                        return Array.Empty<(XElement[] Extent, XElement[] Intent)>();
                    }
                }

                if (Kind == ItemKind.Object || (Kind == ItemKind.Generic && GetAttributes().Length > GetObjects().Length))
                {
                    int[][] IntArray = PowerSet(GetObjects().Length);

                    try
                    {
                        Parallel.ForEach(

                            Partitioner.Create(0, IntArray.Length),

                            new ParallelOptions() { MaxDegreeOfParallelism = Context.MaxDegreeOfParallelism, CancellationToken = Cancellation.Token }, () => { return new Context(ContextDocument.Root); },

                            (Range, LoopState, Context) =>
                            {
                                if (Cancellation.IsCancellationRequested)
                                    return Context;
                                try
                                {
                                    for (int i = Range.Item1; i < Range.Item2; i++)
                                    {
                                        XElement[] A = Context.GetObjects(IntArray[i]);

                                        XElement[] B = Context.Prime(A, ItemKind.Object);

                                        if (A.OrderBy(j => j, new XElementComparer()).SequenceEqual(Context.Prime(B, ItemKind.Attribute), new XElementEqualityComparer()))
                                            Concepts.Add(new ValueTuple<XElement[], XElement[]>(A, B));
                                    }
                                }
                                catch (InvalidOperationException)
                                {
                                    throw;
                                }

                                return Context;

                            }, _ => { }
                        );
                    }
                    catch (OperationCanceledException)
                    {
                        return Array.Empty<(XElement[] Extent, XElement[] Intent)>();
                    }
                }

                (XElement[] Extent, XElement[] Intent)[] SortedConcepts = Concepts.ToArray();

                Array.Sort(SortedConcepts, Context.CompareConceptBySize);

                return SortedConcepts;

            });
        }

        /// <summary>
        /// Attributes with the same extent and/or objects with the same intent will be merged. This kind of context manipulation
        /// does not alter the structure of the concept lattice.
        /// </summary>
        /// <returns>The clarified context.</returns>
        public async Task<Context> ClarifyContextAsync(Status CheckStatus = Status.Clarified)
        {
            return await Task.Run(() =>
            {
                var ClarifiedContext = new Context(ContextDocument?.Root);

                if (!Assert())
                    return ClarifiedContext;

                if (IsClarified(CheckStatus))
                    return ClarifiedContext;

                #region Clarify identical items

                if ((CheckStatus & Status.ClarifiedByObject) != 0)
                    ClarifiedContext.ClarifyObjects();

                if ((CheckStatus & Status.ClarifiedByAttribute) != 0)
                    ClarifiedContext.ClarifyAttributes();

                #endregion

                return ClarifiedContext;
            });
        }

        /// <summary>
        /// Checks whether context document is valid.
        /// </summary>
        /// <returns>Returns true if context document is valid, otherwise false.</returns>
        public bool IsValid() => ContextDocument != null ? true : false;

        /// <summary>
        /// Title of context document that is required by XML schema.
        /// </summary>
        /// <returns>Title of context document.</returns>
        public string GetTitle()
        {
            return ContextDocument?.Element("Data")?.Element("Header")?.Element("Title")?.Value ?? string.Empty;
        }

        /// <summary>
        /// Attribute declarations of given context document.
        /// </summary>
        /// <returns>Returns the array of attribute declarations.</returns>
        public XElement[] GetAttributes()
        {
            return ContextDocument?.Element("Data").Element("Context").Element("Declarations").Element("Attributes").Elements("Attribute").OrderBy(i => i, new XElementComparer())?.ToArray();
        }

        /// <summary>
        /// Selected attribute declarations of given context document.
        /// </summary>
        /// <returns>Returns a sorted set of attribute declarations defined by an array of selected index positions. If an index does not exist, the return value is an empty array.</returns>
        public XElement[] GetAttributes(params int[] Indices)
        {
            SortedSet<XElement> Attributes = new SortedSet<XElement>(new XElementComparer());

            try
            {
                foreach (int Index in Indices)
                    Attributes.Add(ContextDocument?.Element("Data").Element("Context").Element("Declarations").Element("Attributes").Elements("Attribute").Where(i => int.Parse(i.Attribute("Index").Value) == Index).Single());
            }
            catch (Exception) { return Array.Empty<XElement>(); }

            return Attributes.ToArray();
        }

        /// <summary>
        /// Object declarations of given context document.
        /// </summary>
        /// <returns>Returns the array of object declarations.</returns>
        public XElement[] GetObjects()
        {
            return ContextDocument?.Element("Data").Element("Context").Element("Declarations").Element("Objects").Elements("Object").OrderBy(i => i, new XElementComparer())?.ToArray();
        }

        /// <summary>
        /// Selected object declarations of given context document.
        /// </summary>
        /// <returns>Returns a sorted set of object declarations defined by an array of selected index positions. If an index does not exist, the return value is an empty array.</returns>
        public XElement[] GetObjects(params int[] Indices)
        {
            SortedSet<XElement> Objects = new SortedSet<XElement>(new XElementComparer());

            try
            {
                foreach (int Index in Indices)
                    Objects.Add(ContextDocument?.Element("Data").Element("Context").Element("Declarations").Element("Objects").Elements("Object").Where(i => int.Parse(i.Attribute("Index").Value) == Index).Single());
            }
            catch (Exception) { return Array.Empty<XElement>(); }

            return Objects.ToArray();
        }

        /// <summary>
        /// Row at given index position.
        /// </summary>
        /// <param name="Index">Index to be set in the range from 1 to n.</param>
        /// <returns>Returns the row that consists of all columns in an ascending order of column index that belong to the row position.</returns>
        public XElement[] Row(int Index)
        {
            return ContextDocument?.Element("Data").Element("Context").Element("Items").Elements("Item")?.Where(i => int.Parse(i.Element("Object").Attribute("Index").Value) == Index)?.OrderBy(i => i.Element("Attribute"), new XElementComparer()).ToArray();
        }

        /// <summary>
        /// Column at given index position.
        /// </summary>
        /// <param name="Index">Index to be set in the range from 1 to n.</param>
        /// <returns>Returns the column that consists of all rows in an ascending order of row index that belong to the row position.</returns>
        public XElement[] Column(int Index)
        {
            return ContextDocument?.Element("Data").Element("Context").Element("Items").Elements("Item")?.Where(i => int.Parse(i.Element("Attribute").Attribute("Index").Value) == Index)?.OrderBy(i => i.Element("Object"), new XElementComparer()).ToArray();
        }

        /// <summary>
        /// Computes an attribute concept.
        /// </summary>
        /// <param name="Index">Index of an attribute starting from 1 to n.</param>
        /// <returns>Returns attribute concept that belongs to given attribute index.</returns>
        public (XElement[] Extent, XElement[] Intent) AttributeConcept(int Index)
        {
            var Attribute = ContextDocument?.Element("Data").Element("Context").Element("Declarations").Element("Attributes").Elements("Attribute").Where(i => int.Parse(i.Attribute("Index").Value) == Index).Single();

            return (Prime(ToEnumerable(Attribute).ToArray(), ItemKind.Attribute), Prime(Prime(ToEnumerable(Attribute).ToArray(), ItemKind.Attribute), ItemKind.Object));
        }

        /// <summary>
        /// Computes an object concept.
        /// </summary>
        /// <param name="Index">Index of an object starting from 1 to n.</param>
        /// <returns>Returns object concept that belongs to given object index.</returns>
        public (XElement[] Extent, XElement[] Intent) ObjectConcept(int Index)
        {
            var Object = ContextDocument?.Element("Data").Element("Context").Element("Declarations").Element("Objects").Elements("Object").Where(i => int.Parse(i.Attribute("Index").Value) == Index).Single();

            return (Prime(Prime(ToEnumerable(Object).ToArray(), ItemKind.Object), ItemKind.Attribute), Prime(ToEnumerable(Object).ToArray(), ItemKind.Object));
        }

        /// <summary>
        /// This function extracts the formal context data from the given XML document to a memory representation that can be used to bound the data to a view.
        /// </summary>
        /// <returns>Binary context of given formal context.</returns>
        public Context.BinaryItem[,] ExtractBinaryContext()
        {
            BinaryItem[,] Items = new BinaryItem[GetObjects().Length, GetAttributes().Length];

            for (int i = 0; i < GetObjects().Length; i++)
                for (int j = 0; j < GetAttributes().Length; j++)
                    Items[i, j] = ExtractBinaryItem(i + 1, j + 1); // Treat index as label if no specific label is defined 

            #region Check labels

            BinaryItem.HorizontalRightAlignmentByRowLabelEnabled = GetObjects().All(i => string.IsNullOrEmpty(i.Attribute("Label")?.Value));

            BinaryItem.VerticalLeftRotationByColumnLabelEnabled = Enumerable.Range(0, Items.GetLength(1)).Select(x => Items[0, x].Label.Column).Any(x => x.Length > 1);

            #endregion

            #region Check memos

            if (GetObjects().Any(i => string.IsNullOrEmpty(i.Element("Memo")?.Element("Text")?.Value)))
                BinaryItem.HorizontalRightAlignmentByRowMemoEnabled = null;
            else
                BinaryItem.HorizontalRightAlignmentByRowMemoEnabled = false;

            if (GetAttributes().Any(i => string.IsNullOrEmpty(i.Element("Memo")?.Element("Text")?.Value)))
                BinaryItem.VerticalLeftRotationByColumnMemoEnabled = null;
            else
                BinaryItem.VerticalLeftRotationByColumnMemoEnabled = GetAttributes().Any(i => i.Element("Memo")?.Element("Text")?.Value.Length > 1);

            #endregion

            BinaryItem.Title = GetTitle();

            BinaryItem.Status = Status.None;

            if (IsClarifiedByObject())
                BinaryItem.Status = BinaryItem.Status | Status.ClarifiedByObject;

            if (IsClarifiedByAttribute())
                BinaryItem.Status = BinaryItem.Status | Status.ClarifiedByAttribute;

            return Items;
        }

        /// <summary>
        /// A binary row with respect to the given context data by means of the set of objects.
        /// </summary>
        /// <param name="Data">Formal context definition.</param>
        /// <param name="Index">The 1-based index of row.</param>
        /// <returns>A binary row with respect to the given context data.</returns>
        public static Context.BinaryItem[] BinaryRow(Context.BinaryItem[,] Data, int Index) => Enumerable.Range(0, Data.GetLength(1)).Select(x => Data[Index - 1, x]).ToArray();

        /// <summary>
        /// A binary column with respect to the given context data by means of the set of attributes.
        /// </summary>
        /// <param name="Data">Formal context definition.</param>
        /// <param name="Index">The 1-based index of column.</param>
        /// <returns>A binary column with respect to the given context data.</returns>
        public static Context.BinaryItem[] BinaryColumn(Context.BinaryItem[,] Data, int Index) => Enumerable.Range(0, Data.GetLength(0)).Select(x => Data[x, Index - 1]).ToArray();

        /// <summary>
        /// Converts the XML data structure of formal context to an ASCII or HTML table.
        /// </summary>
        /// <param name="AsHtml">Should be true if html format is required otherwise false.</param>
        /// <returns>Lines of ASCII or HTML document.</returns>
        public string[] ToStringArray(bool AsHtml = false)
        {
            if (ContextDocument == null)
                return Array.Empty<string>();

            char Separator = '|';

            var Table = new List<string>();

            string Head = Header(GetAttributes());

            Table.Add(Head);

            XElement[] Objects = GetObjects();

            for (int i = 1; i <= Objects.Length; i++)
                Table.Add(Content(i, Row(i)));

            return AsHtml ? ToHtml(Table.ToArray(), ContextDocument.BaseUri) : Table.ToArray();

            string Header(XElement[] Items)
            {
                var TextLine = new StringBuilder();

                string FstColumnWitdh = new string('-', GetObjects().Length.ToString().Length);

                TextLine.Append(Separator + FstColumnWitdh + Separator);

                foreach (var Item in Items)
                {
                    string sLine;

                    if (Item.Attribute("Label") != null)
                        sLine = $"{Separator}{Item.Attribute("Label").Value.DefaultIfEmpty('?').Last()}";
                    else
                        sLine = $"{Separator}{Item.Attribute("Index").Value.DefaultIfEmpty('?').Last()}";

                    TextLine.Append(sLine + Separator);
                }

                return TextLine.ToString();
            }

            string Content(int i, XElement[] Items)
            {
                var TextLine = new StringBuilder();

                string FstColumnWitdh = i.ToString().PadLeft(GetObjects().Length.ToString().Length, '0');

                TextLine.Append(Separator + FstColumnWitdh + Separator);

                foreach (var Item in Items)
                {
                    string sLine = string.Empty;

                    if (bool.Parse(Item.Element("Value").Value)) sLine = Separator + "X";
                    else
                    {
                        if (!Item.HasAttributes)
                            sLine = Separator + " ";
                        else
                        {
                            if (Item.Attribute("Arrow") == null || string.IsNullOrEmpty(Item.Attribute("Arrow").Value))
                                sLine = Separator + " ";
                            else
                            {
                                if (Item.Attribute("Arrow").Value == "+")
                                    sLine = Separator + "+";

                                if (Item.Attribute("Arrow").Value == "-")
                                    sLine = Separator + "-";

                                if (Item.Attribute("Arrow").Value == "0")
                                    sLine = Separator + "0";

                                if (Item.Attribute("Arrow").Value == "na") // Arrow relation is non applicable
                                    sLine = Separator + " ";
                            }
                        }

                    }

                    TextLine.Append(sLine + Separator);
                }

                return TextLine.ToString();
            }
        }

        /// <summary>
        /// Computes arrow relations of the given context.
        /// </summary>
        /// <returns>False if the given context is not at least clarified.</returns>
        public bool ComputeArrowRelations()
        {
            if (!IsClarified()) // Current context must be clarified by objects and attributes.
                return false;

            var AttributeConcepts = new Dictionary<int, (XElement[] Extent, XElement[] Intent)>();

            var ObjectConcepts = new Dictionary<int, (XElement[] Extent, XElement[] Intent)>();

            int Objects = GetObjects().Length;

            for (int i = 1; i <= Objects; i++) // Traverse through the context
            {
                XElement[] Row = this.Row(i);

                if (Row.Any())
                {
                    int Attributes = GetAttributes().Length;

                    (XElement[] Extent, XElement[] Intent) ObjectConcept;

                    if (false == ObjectConcepts.TryGetValue(i, out ObjectConcept))
                    {
                        ObjectConcept = this.ObjectConcept(i);

                        ObjectConcepts.Add(i, ObjectConcept);
                    }

                    for (int j = 1; j <= Attributes; j++)
                    {
                        XElement[] Column = this.Column(j);

                        if (Column.Any())
                        {
                            int k = 0;

                            while (k < Row.Length)
                            {
                                if (j == int.Parse(Row[k].Element("Attribute").Attribute("Index").Value))
                                {
                                    if (bool.Parse(Row[k].Element("Value").Value) is false)
                                    {
                                        (XElement[] Extent, XElement[] Intent) AttributeConcept;

                                        if (false == AttributeConcepts.TryGetValue(j, out AttributeConcept))
                                        {
                                            AttributeConcept = this.AttributeConcept(j);

                                            AttributeConcepts.Add(j, AttributeConcept);
                                        }

                                        if (Row[k].Attribute("Arrow") is null)
                                            Row[k].Add(new XAttribute("Arrow", ArrowRelation(Row[k], AttributeConcept, ObjectConcept)));
                                        else
                                            Row[k].Attribute("Arrow").SetValue(ArrowRelation(Row[k], AttributeConcept, ObjectConcept));
                                    }

                                    break;
                                }

                                k++;
                            }
                        }
                    }
                }
            }

            return true;

            string ArrowRelation(XElement Item, (XElement[] Extent, XElement[] Intent) AttributeConceptByCell, (XElement[] Extent, XElement[] Intent) ObjectConceptByCell)
            {
                bool? pArrow = null;
                bool? nArrow = null;

                for (int i = 1; i <= GetAttributes().Length; i++)
                {
                    XElement[] Column = this.Column(i);

                    if (Column.Any())
                    {
                        (XElement[] Extent, XElement[] Intent) AttributeConcept;

                        if (false == AttributeConcepts.TryGetValue(i, out AttributeConcept))
                        {
                            AttributeConcept = this.AttributeConcept(i);

                            AttributeConcepts.Add(i, AttributeConcept);
                        }

                        if (IsProperSubset(AttributeConceptByCell.Extent, AttributeConcept.Extent))
                        {
                            var Extent = new SortedSet<XElement>(AttributeConcept.Extent, new XElementComparer());

                            if (Extent.Contains(Item.Element("Object")))
                            {
                                pArrow = true;
                            }
                            else
                            {
                                pArrow = false;

                                break;
                            }

                        }
                    }
                }

                for (int i = 1; i <= GetObjects().Length; i++)
                {
                    XElement[] Row = this.Row(i);

                    if (Row.Any())
                    {
                        (XElement[] Extent, XElement[] Intent) ObjectConcept;

                        if (false == ObjectConcepts.TryGetValue(i, out ObjectConcept))
                        {
                            ObjectConcept = this.ObjectConcept(i);

                            ObjectConcepts.Add(i, ObjectConcept);
                        }

                        if (IsProperSubset(ObjectConceptByCell.Intent, ObjectConcept.Intent))
                        {
                            var Intent = new SortedSet<XElement>(ObjectConcept.Intent, new XElementComparer());

                            if (Intent.Contains(Item.Element("Attribute")))
                            {
                                nArrow = true;
                            }
                            else
                            {
                                nArrow = false;

                                break;
                            }

                        }
                    }
                }

                if (pArrow == null || (pArrow != null && pArrow == true))
                {
                    if (nArrow == null || (nArrow != null && nArrow == true))
                        return "0";
                    else
                        return "+";
                }

                if (nArrow == null || (nArrow != null && nArrow == true))
                {
                    if (pArrow == null || (pArrow != null && pArrow == true))
                        return "0";
                    else
                        return "-";
                }

                return "na";

                bool IsProperSubset(XElement[] L, XElement[] R)
                {
                    var _L = new SortedSet<XElement>(L, new XElementComparer());
                    var _R = new SortedSet<XElement>(R, new XElementComparer());

                    if (_L.IsProperSubsetOf(_R)) return true;
                    else return false;
                }
            }
        }

        /// <summary>
        /// Removes arrow relations of the given context.
        /// </summary>
        public void ClearArrows()
        {
            if (Assert() is false)
                return;

            var Items = ContextDocument?.Element("Data").Element("Context").Element("Items").Elements("Item");

            if (Items == null)
                return;

            foreach (XElement Item in Items)
                Item.Attribute("Arrow")?.Remove();
        }

        private Context(out string Protocol, string XmlFile)
        {
            Protocol = string.Empty;

            try
            {
                Validate(out Protocol, new Context(XDocument.Load(XmlFile, LoadOptions.SetBaseUri).Root));
            }
            catch (Exception e)
            {
                Protocol = e.Message;
            }
        }

        private Context(out string Protocol, XDocument Data)
        {
            Validate(out Protocol, new Context(Data.Root));
        }

        private Context(XElement ContextRoot)
        {
            if (ContextRoot != null)
                ContextDocument = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), ContextRoot);
            else
                ContextDocument = null;
        }

        private Context.BinaryItem ExtractBinaryItem(int RowIndex, int ColumnIndex) => new Context.BinaryItem()
        {
            Index = new Context.BinaryItem.Incidence()
            {
                Row = RowIndex,
                Column = ColumnIndex
            },

            Label = new Context.BinaryItem.Labels()
            {
                Row = GetObjects(RowIndex).First().Attribute("Label")?.Value ?? RowIndex.ToString(),
                Column = GetAttributes(ColumnIndex).First().Attribute("Label")?.Value ?? ColumnIndex.ToString()
            },

            LabelView = new Context.BinaryItem.Labels()
            {
                Row = GetObjects(RowIndex).First().Attribute("Label")?.Value ?? RowIndex.ToString(),
                Column = GetAttributes(ColumnIndex).First().Attribute("Label")?.Value ?? ColumnIndex.ToString()
            },

            Memo = new Context.BinaryItem.Memos()
            {
                Row = GetObjects(RowIndex).First().Element("Memo")?.Element("Text")?.Value ?? string.Empty,
                Column = GetAttributes(ColumnIndex).First().Element("Memo")?.Element("Text")?.Value ?? string.Empty
            },

            ClarifiedBy = new BinaryItem.RedundancyItems()
            {
                Objects = GetObjects(RowIndex).First().Element("Clarified")?.Elements("Object").Select(i => i.Attribute("Label") != null ? i.Attribute("Label").Value : i.Attribute("Index").Value).ToArray(),
                Attributes = GetAttributes(ColumnIndex).First().Element("Clarified")?.Elements("Attribute").Select( i => i.Attribute("Label") != null ? i.Attribute("Label").Value : i.Attribute("Index").Value ).ToArray()
            },

            Value = bool.Parse(Row(RowIndex)[ColumnIndex - 1].Element("Value").Value),

            ArrowRelation = EncodeArrowRelation(RowIndex, ColumnIndex)
        };

        private static bool Validate(out string Protocol, Context Document)
        {
            var SchemaSet = new XmlSchemaSet();

            Protocol = string.Empty;

            try
            {
                SchemaSet.Add("", XmlReader.Create(new System.IO.StringReader(XsdMarkup())));
            }
            catch (Exception) { return false; }

            var Log = new StringBuilder();

            Document.ContextDocument.Validate(SchemaSet, (o, e) => { Log.AppendLine(e.Message); });

            Protocol = Log.ToString();

            return string.IsNullOrEmpty(Protocol) ? true : false;
        }

        /// <summary>
        /// A standard context is clarified, purified and reduced by objects and attributes.
        /// </summary>
        /// <returns>Returns true if the context status is valid.</returns>
        private bool IsStandard()
        {
            if (!Assert())
                return false;

            if (ContextDocument?.Element("Data").Element("Context").Attribute("Status")?.Value == "Standard")
                return true;
            else
                return false;
        }

        private int? EncodeArrowRelation(int RowIndex, int ColumnIndex)
        {
            string Arrow = Row(RowIndex)[ColumnIndex - 1].Attribute("Arrow")?.Value ?? string.Empty;

            if (Arrow == "+") return +1;
                else if (Arrow == "-") return -1;
                        else if (Arrow == "0") return 0;
                                else return null;
        }

        /// <summary>
        /// This function merges objects with the same intent.
        /// </summary>
        private void ClarifyObjects()
        {
            if (IsClarifiedByObject())
                return;

            #region Register XML property

            if (ContextDocument.Element("Data").Element("Context").Element("Clarified") == null)
                ContextDocument.Element("Data").Element("Context").AddFirst(new XElement("Clarified"));

            if (ContextDocument.Element("Data").Element("Context").Element("Clarified").Attribute("ByObject") == null)
                ContextDocument.Element("Data").Element("Context").Element("Clarified").Add(new XAttribute("ByObject", bool.TrueString.ToLower()));
            else
                ContextDocument.Element("Data").Element("Context").Element("Clarified").Attribute("ByObject").SetValue(bool.TrueString.ToLower());

            #endregion

            int Items = GetObjects().Length;

            for (int i = 1; i <= Items; i++)
            {
                XElement[] RowRef = Row(i);

                if (RowRef.Any())
                {
                    for (int j = i + 1; j <= Items; j++)
                    {
                        XElement[] RowCmp = Row(j);

                        if (RowCmp.Any())
                        {
                            int k = 0;

                            while (k < RowRef.Length)
                            {
                                if (bool.Parse(RowRef[k].Element("Value").Value) != bool.Parse(RowCmp[k].Element("Value").Value))
                                    break;

                                k++;
                            }

                            if (k == RowRef.Length)
                                ClarifyObjectAt(i, j);
                        }
                    }
                }
            }

            XNode xOuterNode;

            // Replace and remove certain objects in reverse order after removable objects have been marked
            xOuterNode = ContextDocument.Element("Data").Element("Context").Element("Declarations").Element("Objects")?.LastNode;

            while (xOuterNode != null)
            {
                var xObjectToBeRemoved = XElement.Parse(xOuterNode.ToString());

                if (xObjectToBeRemoved.Attribute("Remove") == null)
                    xOuterNode = xOuterNode.PreviousNode;
                else
                {
                    // If attribute exists, attribute value equals true
                    int ToRemove = int.Parse(xObjectToBeRemoved.Attribute("Index").Value);

                    var xInnerNode = xOuterNode.PreviousNode;

                    while (xInnerNode != null)
                    {
                        var xObjectToBeClarified = XElement.Parse(xInnerNode.ToString());

                        int ToClarify = int.Parse(xObjectToBeClarified.Attribute("Index").Value);

                        if (xObjectToBeClarified.Attribute("Clarified") != null)
                        {
                            var Clarified = new SortedSet<int>(xObjectToBeClarified.Attribute("Clarified").Value.Trim(new char[] { '{', '}' }).Split(',').Select(i => int.Parse(i)));

                            if (Clarified.Contains(ToRemove))
                            {
                                if (xObjectToBeClarified.Element("Clarified") == null)
                                    xObjectToBeClarified.Add(new XElement("Clarified"));

                                xObjectToBeRemoved.Attribute("Remove").Remove();

                                xObjectToBeClarified.Element("Clarified").Add(xObjectToBeRemoved);

                                Clarified.Remove(ToRemove);

                                if (Clarified.Any())
                                    xObjectToBeClarified.Attribute("Clarified").SetValue($"{'{'}{string.Join(",", Clarified.Select(i => i.ToString()))}{'}'}");
                                else
                                    xObjectToBeClarified.Attribute("Clarified").Remove();

                                // Update XML

                                ContextDocument.Element("Data").Element("Context").Element("Declarations").Element("Objects").Elements("Object").Where(e => int.Parse(e.Attribute("Index").Value) == ToClarify).Single().ReplaceWith(xObjectToBeClarified);

                                ContextDocument.Element("Data").Element("Context").Element("Declarations").Element("Objects").Elements("Object").Where(e => int.Parse(e.Attribute("Index").Value) == ToRemove).Remove();

                                var ObjectsNode = ContextDocument.Element("Data").Element("Context").Element("Declarations").Element("Objects");

                                int j = 1;

                                while (ObjectsNode.Elements("Object").Where(e => int.Parse(e.Attribute("Index").Value) == ToRemove + j).Any())
                                {
                                    var ObjectItem = ObjectsNode.Elements("Object").Where(e => int.Parse(e.Attribute("Index").Value) == ToRemove + j).Single();

                                    ObjectItem.Add(new XElement("ReIndexedByClarifying", ToRemove + j));

                                    ObjectItem.SetAttributeValue("Index", ToRemove + j - 1);

                                    var ObjectCell = ContextDocument?.Element("Data").Element("Context").Element("Items").Elements("Item").Where(e => int.Parse(e.Element("Object").Attribute("Index").Value) == ToRemove + j);

                                    foreach (var Item in ObjectCell)
                                        Item.Element("Object").SetAttributeValue("Index", ToRemove + j - 1);

                                    j++;
                                }

                                break;
                            }
                        }

                        xInnerNode = xInnerNode.PreviousNode;
                    }

                    xOuterNode = ContextDocument.Element("Data").Element("Context").Element("Declarations").Element("Objects").LastNode;
                }
            }

            void ClarifyObjectAt(int Index, int ToDelete)
            {
                // Update object references belonging to declarations
                var Reference = ContextDocument.Element("Data").Element("Context").Element("Declarations").Element("Objects").Elements("Object").Where(e => int.Parse(e.Attribute("Index").Value) == Index).Single();

                if (Reference.Attribute("Clarified") is null) Reference.Add(new XAttribute("Clarified", $"{'{'}{ToDelete}{'}'}"));
                else
                {
                    var Clarified = new SortedSet<int>(Reference.Attribute("Clarified").Value.Trim(new char[] { '{', '}' }).Split(',').Select(i => int.Parse(i)));

                    Clarified.Add(ToDelete);

                    Reference.Attribute("Clarified").SetValue($"{'{'}{string.Join(",", Clarified.Select(i => i.ToString()))}{'}'}");
                }

                // Update object declaration (to be removed)
                var ObjectDeclaration = ContextDocument.Element("Data").Element("Context").Element("Declarations").Element("Objects").Elements("Object").Where(e => int.Parse(e.Attribute("Index").Value) == ToDelete).Single();

                if (ObjectDeclaration.Attribute("Remove") is null)
                    ObjectDeclaration.Add(new XAttribute("Remove", bool.TrueString.ToLower()));
                else
                    ObjectDeclaration.Attribute("Remove").SetValue(bool.TrueString.ToLower());

                // Remove certain object data items belonging to object index
                ContextDocument.Element("Data").Element("Context").Element("Items").Elements("Item").Where(e => int.Parse(e.Element("Object").Attribute("Index").Value) == ToDelete).Remove();
            }
        }

        /// <summary>
        /// This function merges attributes with the same extent.
        /// </summary>
        private void ClarifyAttributes()
        {
            if (IsClarifiedByAttribute())
                return;

            #region Register XML property

            if (ContextDocument.Element("Data").Element("Context").Element("Clarified") == null)
                ContextDocument.Element("Data").Element("Context").AddFirst(new XElement("Clarified"));

            if (ContextDocument.Element("Data").Element("Context").Element("Clarified").Attribute("ByAttribute") == null)
                ContextDocument.Element("Data").Element("Context").Element("Clarified").Add(new XAttribute("ByAttribute", bool.TrueString.ToLower()));
            else
                ContextDocument.Element("Data").Element("Context").Element("Clarified").Attribute("ByAttribute").SetValue(bool.TrueString.ToLower());

            #endregion

            int Items = GetAttributes().Length;

            for (int i = 1; i <= Items; i++)
            {
                XElement[] ColumnRef = Column(i);

                if (ColumnRef.Any())
                {
                    for (int j = i + 1; j <= Items; j++)
                    {
                        XElement[] ColumnCmp = Column(j);

                        if (ColumnCmp.Any())
                        {
                            int k = 0;

                            while (k < ColumnRef.Length)
                            {
                                if (bool.Parse(ColumnRef[k].Element("Value").Value) != bool.Parse(ColumnCmp[k].Element("Value").Value))
                                    break;

                                k++;
                            }

                            if (k == ColumnRef.Length)
                                ClarifyAttributeAt(i, j);
                        }
                    }
                }
            }

            XNode xOuterNode;

            // Replace and remove certain attributes after removable attributes have been remarked
            xOuterNode = ContextDocument.Element("Data").Element("Context").Element("Declarations").Element("Attributes")?.LastNode;

            while (xOuterNode != null)
            {
                var xAttributeToBeRemoved = XElement.Parse(xOuterNode.ToString());

                if (xAttributeToBeRemoved.Attribute("Remove") == null)
                    xOuterNode = xOuterNode.PreviousNode;
                else
                {
                    // If attribute exists, attribute value equals true
                    int ToRemove = int.Parse(xAttributeToBeRemoved.Attribute("Index").Value);

                    var xInnerNode = xOuterNode.PreviousNode;

                    while (xInnerNode != null)
                    {
                        var xAttributeToBeClarified = XElement.Parse(xInnerNode.ToString());

                        int ToClarify = int.Parse(xAttributeToBeClarified.Attribute("Index").Value);

                        if (xAttributeToBeClarified.Attribute("Clarified") != null)
                        {
                            var Clarified = new SortedSet<int>(xAttributeToBeClarified.Attribute("Clarified").Value.Trim(new char[] { '{', '}' }).Split(',').Select(i => int.Parse(i)));

                            if (Clarified.Contains(ToRemove))
                            {
                                if (xAttributeToBeClarified.Element("Clarified") == null)
                                    xAttributeToBeClarified.Add(new XElement("Clarified"));

                                xAttributeToBeRemoved.Attribute("Remove").Remove();

                                xAttributeToBeClarified.Element("Clarified").Add(xAttributeToBeRemoved);

                                Clarified.Remove(ToRemove);

                                if (Clarified.Any())
                                    xAttributeToBeClarified.Attribute("Clarified").SetValue($"{'{'}{string.Join(",", Clarified.Select(i => i.ToString()))}{'}'}");
                                else
                                    xAttributeToBeClarified.Attribute("Clarified").Remove();

                                // Update XML

                                ContextDocument.Element("Data").Element("Context").Element("Declarations").Element("Attributes").Elements("Attribute").Where(e => int.Parse(e.Attribute("Index").Value) == ToClarify).Single().ReplaceWith(xAttributeToBeClarified);

                                ContextDocument.Element("Data").Element("Context").Element("Declarations").Element("Attributes").Elements("Attribute").Where(e => int.Parse(e.Attribute("Index").Value) == ToRemove).Remove();

                                var AttributesNode = ContextDocument.Element("Data").Element("Context").Element("Declarations").Element("Attributes");

                                int j = 1;

                                while (AttributesNode.Elements("Attribute").Where(e => int.Parse(e.Attribute("Index").Value) == ToRemove + j).Any())
                                {
                                    var AttributeItem = AttributesNode.Elements("Attribute").Where(e => int.Parse(e.Attribute("Index").Value) == ToRemove + j).Single();

                                    AttributeItem.Add(new XElement("ReIndexedByClarifying", ToRemove + j));

                                    AttributeItem.SetAttributeValue("Index", ToRemove + j - 1);

                                    var AttributeCell = ContextDocument?.Element("Data").Element("Context").Element("Items").Elements("Item").Where(e => int.Parse(e.Element("Attribute").Attribute("Index").Value) == ToRemove + j);

                                    foreach (var Item in AttributeCell)
                                        Item.Element("Attribute").SetAttributeValue("Index", ToRemove + j - 1);

                                    j++;
                                }

                                break;
                            }
                        }

                        xInnerNode = xInnerNode.PreviousNode;
                    }

                    xOuterNode = ContextDocument.Element("Data").Element("Context").Element("Declarations").Element("Attributes").LastNode;
                }
            }

            void ClarifyAttributeAt(int Index, int ToDelete)
            {
                // Update attribute references belonging to attribute declarations
                var Reference = ContextDocument?.Element("Data").Element("Context").Element("Declarations").Element("Attributes").Elements("Attribute").Where(e => int.Parse(e.Attribute("Index").Value) == Index).Single();

                if (Reference.Attribute("Clarified") is null) Reference.Add(new XAttribute("Clarified", $"{'{'}{ToDelete}{'}'}"));
                else
                {
                    var Clarified = new SortedSet<int>(Reference.Attribute("Clarified").Value.Trim(new char[] { '{', '}' }).Split(',').Select(i => int.Parse(i)));

                    Clarified.Add(ToDelete);

                    Reference.Attribute("Clarified").SetValue($"{'{'}{string.Join(",", Clarified.Select(i => i.ToString()))}{'}'}");
                }

                // Update attribute declaration (to be removed)
                var AttributeDeclaration = ContextDocument?.Element("Data").Element("Context").Element("Declarations").Element("Attributes").Elements("Attribute").Where(e => int.Parse(e.Attribute("Index").Value) == ToDelete).Single();

                if (AttributeDeclaration.Attribute("Remove") is null)
                    AttributeDeclaration.Add(new XAttribute("Remove", bool.TrueString.ToLower()));
                else
                    AttributeDeclaration.Attribute("Remove").SetValue(bool.TrueString.ToLower());

                // Remove certain object data items
                ContextDocument.Element("Data").Element("Context").Element("Items").Elements("Item").Where(e => int.Parse(e.Element("Attribute").Attribute("Index").Value) == ToDelete).Remove();
            }
        }

        /// <summary>
        /// Determines whether the current context is clarified by attribute. 
        /// </summary>
        /// <returns>Returns true if the given status is valid.</returns>
        private bool IsClarifiedByAttribute()
        {
            bool IsPartiallyClarified = false;

            bool.TryParse(ContextDocument.Element("Data").Element("Context").Element("Clarified")?.Attribute("ByAttribute")?.Value, out IsPartiallyClarified);

            return IsPartiallyClarified;
        }

        /// <summary>
        /// Determines whether the current context is clarified by object. 
        /// </summary>
        /// <returns>Returns true if the given status is valid.</returns>
        private bool IsClarifiedByObject()
        {
            bool IsPartiallyClarified = false;

            bool.TryParse(ContextDocument.Element("Data").Element("Context").Element("Clarified")?.Attribute("ByObject")?.Value, out IsPartiallyClarified);

            return IsPartiallyClarified;
        }

        /// <summary>
        /// Determines whether the current context is clarified. 
        /// </summary>
        /// <param name="StatusToBeChecked">The status that is to be checked for the given context.</param>
        /// <returns>Returns true if the given status is valid.</returns>
        private bool IsClarified(Status StatusToBeChecked = Status.Clarified)
        {
            if (IsStandard())
                return true;
            else
            {
                switch(StatusToBeChecked)
                {
                    case Status.ClarifiedByObject: return IsClarifiedByObject();
                    case Status.ClarifiedByAttribute: return IsClarifiedByAttribute();
                    case Status.Clarified: return IsClarifiedByObject() && IsClarifiedByAttribute();
                    default: return false;
                }
            }
        }

        private static string XsdMarkup()
        {
            string XsdMarkup = string.Empty;

            var ResourceAssembly = Assembly.GetExecutingAssembly();

            string ResourceString = ResourceAssembly.GetName().Name + ".Context.xsd";

            using var ResourceStream = ResourceAssembly.GetManifestResourceStream(ResourceString);

            using var Reader = new StreamReader(ResourceStream);

            XsdMarkup = Reader.ReadToEnd();

            // Manual of XSD regular expressions https://www.w3.org/TR/xmlschema-2/#regexs
            return XsdMarkup;
        }

        private bool Assert()
        {
            if (!IsValid() || GetAttributes() == null || GetObjects() == null)
                return false;

            return true;
        }

        private static IEnumerable<XElement> ToEnumerable<XElement>(XElement Item)
        {
            return new XElement[] { Item }; // Equals to 'Enumerable.Repeat(Item,1)' or 'yield return Item'
        }

        private static int[][] PowerSet(int Count)
        {
            var IntList = new List<int[]>();

            var Set = new SortedSet<int>();

            #region Algorithm by backtracking

            // An explanation of the back tracking algorithm and Pascal versions can be found in the textbook: Dietmar Herrmann,
            // Algorithmen Arbeitsbuch, 1. rev. ed. (Addison Wesley, 1995), 250-252.

            if (Count < 0)
                Count = 0;

            do
            {
                if (Count == 0)
                    break;

                IntList.Add(Set.ToArray());

                int i = Count;

                while (Set.Contains(i))
                    Set.Remove(i--);

                Set.Add(i);

            } while (Set.Count() != Count);

            #endregion

            IntList.Add(Set.ToArray());

            return IntList.Count() == 1 << Count ? IntList.ToArray() : null;
        }

        private XElement[] Prime(XElement[] Items, ItemKind ObjectOrAttribute)
        {
            // Defines derivation operator.

            // All returns true if set is empty.
            if ((Items.All(x => x.Name.LocalName == "Attribute") && Items.Length != 0) || (Items.Length == 0 && ObjectOrAttribute == ItemKind.Attribute))
            {
                XElement[] Objects = GetObjects();

                var Extent = new SortedSet<XElement>(new XElementComparer());

                foreach (var Object in Objects)
                    if (Row(int.Parse(Object.Attribute("Index").Value)).Where(x => bool.Parse(x.Element("Value").Value) == true).OrderBy(x => x.Element("Attribute"), new XElementComparer()).Select(x => x.Element("Attribute")).Intersect(Items.OrderBy(x => x, new XElementComparer()), new XElementEqualityComparer()).OrderBy(x => x, new XElementComparer()).SequenceEqual(Items.OrderBy(x => x, new XElementComparer()).ToArray(), new XElementEqualityComparer()))
                        Extent.Add(Object);

                return Extent.ToArray();
            }

            // All returns true if set is empty.
            if ((Items.All(x => x.Name.LocalName == "Object") && Items.Length != 0) || (Items.Length == 0 && ObjectOrAttribute == ItemKind.Object))
            {
                XElement[] Attributes = GetAttributes();

                var Intent = new SortedSet<XElement>(new XElementComparer());

                foreach (var Attribute in Attributes)
                    if (Column(int.Parse(Attribute.Attribute("Index").Value)).Where(x => bool.Parse(x.Element("Value").Value) == true).OrderBy(x => x.Element("Object"), new XElementComparer()).Select(x => x.Element("Object")).Intersect(Items.OrderBy(x => x, new XElementComparer()), new XElementEqualityComparer()).OrderBy(x => x, new XElementComparer()).SequenceEqual(Items.OrderBy(x => x, new XElementComparer()).ToArray(), new XElementEqualityComparer()))
                        Intent.Add(Attribute);

                return Intent.ToArray();
            }

            return null;
        }

        private class XElementEqualityComparer : EqualityComparer<XElement>
        {
            public override bool Equals(XElement x, XElement y)
            {
                if (Object.ReferenceEquals(x, y)) return true;

                return int.Parse(x.Attribute("Index").Value) == int.Parse(y.Attribute("Index").Value) ? true : false;
            }

            public override int GetHashCode(XElement x)
            {
                return x.Attribute("Index").Value.GetHashCode();
            }
        }

        private class XElementComparer : Comparer<XElement>
        {
            public override int Compare(XElement x, XElement y)
            {
                return Context.Compare(x, y);
            }
        }

        private static int Compare(XElement x, XElement y)
        {
            if (object.Equals(x, y)) // If the current instance is a reference type, the Equals(Object) method tests for reference equality, and a call to the Equals(Object) method is equivalent to a call to the ReferenceEquals method.
                return 0;
            else if (x is null || y is null)
                return 0;
            else
                return int.Parse(x.Attribute("Index").Value).CompareTo(int.Parse(y.Attribute("Index").Value));
        }

        private static int CompareConceptBySize((XElement[] Extent, XElement[] Intent) x, (XElement[] Extent, XElement[] Intent) y)
        {
            if (x.Extent.Length < y.Extent.Length) return +1;      // x < y
            else if (x.Extent.Length > y.Extent.Length) return -1; // x > y
            else if (x.Intent.Length > y.Intent.Length) return +1; // x < y
            else if (x.Intent.Length < y.Intent.Length) return -1; // x > y
            else
            {
                #region Compared by index position

                for (int i = 0; i < x.Intent.Length; i++)
                    if (int.Parse(x.Intent[i].Attribute("Index").Value) < int.Parse(y.Intent[i].Attribute("Index").Value)) return -1;
                    else if (int.Parse(x.Intent[i].Attribute("Index").Value) > int.Parse(y.Intent[i].Attribute("Index").Value)) return +1;

                for (int i = 0; i < x.Extent.Length; i++)
                    if (int.Parse(x.Extent[i].Attribute("Index").Value) < int.Parse(y.Extent[i].Attribute("Index").Value)) return +1;
                    else if (int.Parse(x.Extent[i].Attribute("Index").Value) > int.Parse(y.Extent[i].Attribute("Index").Value)) return -1;

                #endregion

                return 0;
            }
        }

        private ulong ToBinaryValue(XElement[] Items, ItemKind Kind)
        {
            if (!Assert() || Items == null || Items.Length == 0 || ItemKind.Generic == Kind) return 0;
            else
            {
                ulong Value = 0; int Length = 0;

                switch(Kind)
                {
                    case ItemKind.Attribute:
                        {
                            Length = GetAttributes().Length;

                            break;
                        }

                    case ItemKind.Object:
                        {
                            Length = GetObjects().Length;

                            break;
                        }
                }

                foreach (XElement Item in Items)
                    Value = Value + (1ul << (Length - int.Parse(Item.Attribute("Index").Value)));

                // 1 << 1 = 2
                // 1 << 2 = 4
                // 1 << 3 = 8
                // 1 << 4 = ...

                return Value;
            }
        }

        private readonly XDocument ContextDocument = null;
    }
}
