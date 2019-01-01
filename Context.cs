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
        public enum ItemKind
        {
            ///<summary>Set is interpreted as an object or attribute belonging to given item.</summary>
            Generic = 0,

            ///<summary>Empty set is interpreted as an attribute item.</summary>
            Attribute = 1,

            ///<summary>Empty set is interpreted as an object item.</summary>
            Object = 2
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

            Context Ctx = new Context(out Protocol, XmlData);

            return string.IsNullOrEmpty(Protocol) ? true : false;
        }

        /// <summary>
        /// Creates a standard context with two elements.
        /// </summary>
        /// <returns>A context document that contains a two dimensional data structure.</returns>
        public static XDocument CreateStandardContextWithTwoElements()
        {
            /*
               An illustration of the given standard formal context with two elements:

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
        /// Converts protocol strings to html.
        /// </summary>
        /// <param name="Protocol">Text to be converted.</param>
        /// <param name="Title">Title of html page.</param>
        /// <param name="Wrap">Determines if text should be wrapped.</param>
        /// <returns>Html text that can be shown by a browser.</returns>
        public static string[] ToHtml(string[] Protocol, string Title = "", bool Wrap = false)
        {
            #region HTML // http://www.scriptingmaster.com/html/basic-structure-HTML-document.asp

            List<string> ToHtml = new List<string>(Protocol);

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
        public Uri DocumentUri()
        {
            return string.IsNullOrEmpty(ContextDocument?.BaseUri) ? null : new Uri(ContextDocument.BaseUri);
        }

        /// <summary>
        /// Creates a deep copy of given context document.
        /// </summary>
        /// <returns>Returns a copy of the underlying XML document.</returns>
        public XDocument GetContextDocument()
        {
            return ContextDocument != null ? new XDocument(ContextDocument) : null;
        }

        /// <summary>
        /// Computes all formal concepts of given context by naive algorithm, i.e., each item of the power set of
        /// attribute or object items is checked with the help of the derivation operator whether A = A'' is true.
        /// </summary>
        /// <param name="Kind">The kind parameter is used to start the computation over attributes or objects. If
        /// set to generic, the computation starts with the smallest size of given context items.</param>
        /// <returns>Sorted list of concepts.</returns>
        public async Task<(XElement[] Extent, XElement[] Intent)[]> FindConceptsAsync(ItemKind Kind = ItemKind.Generic)
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
        public async Task<(XElement[] Extent, XElement[] Intent)[]> FindConceptsParallel(CancellationTokenSource Cancellation, ItemKind Kind = ItemKind.Generic)
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
        /// Checks whether context document is valid.
        /// </summary>
        /// <returns>Returns true if context document is valid, otherwise false.</returns>
        public bool IsValid()
        {
            return (ContextDocument != null) ? true : false;
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
        /// Converts the XML data structure of formal context to a HTML table.
        /// </summary>
        /// <returns>Lines of a HTML document.</returns>
        public string[] CrossTable(bool AsHtml = false)
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

        private static string XsdMarkup()
        {
            string XsdMarkup = string.Empty;

            var ResourceAssembly = Assembly.GetExecutingAssembly();

            string ResourceString = ResourceAssembly.GetName().Name + ".Context.xsd";

            using (var ResourceStream = ResourceAssembly.GetManifestResourceStream(ResourceString))
            using (var Reader = new StreamReader(ResourceStream))
                XsdMarkup = Reader.ReadToEnd();

            return XsdMarkup;
        }

        private bool Assert()
        {
            if (!IsValid() || GetAttributes() == null || GetObjects() == null)
                return false;

            return true;
        }

        private static int[][] PowerSet(int Count)
        {
            List<int[]> IntList = new List<int[]>();

            SortedSet<int> Set = new SortedSet<int>();

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
            // Defines derivation operator

            // All returns true if set is empty.
            if ((Items.All(x => x.Name.LocalName == "Attribute") && Items.Length != 0) || (Items.Length == 0 && ObjectOrAttribute == ItemKind.Attribute))
            {
                XElement[] Objects = GetObjects();

                SortedSet<XElement> Extent = new SortedSet<XElement>(new XElementComparer());

                foreach (XElement Object in Objects)
                    if (Row(int.Parse(Object.Attribute("Index").Value)).Where(x => bool.Parse(x.Element("Value").Value) == true).OrderBy(x => x.Element("Attribute"), new XElementComparer()).Select(x => x.Element("Attribute")).Intersect(Items.OrderBy(x => x, new XElementComparer()), new XElementEqualityComparer()).OrderBy(x => x, new XElementComparer()).SequenceEqual(Items.OrderBy(x => x, new XElementComparer()).ToArray(), new XElementEqualityComparer()))
                        Extent.Add(Object);

                return Extent.ToArray();
            }

            // All returns true if set is empty.
            if ((Items.All(x => x.Name.LocalName == "Object") && Items.Length != 0) || (Items.Length == 0 && ObjectOrAttribute == ItemKind.Object))
            {
                XElement[] Attributes = GetAttributes();

                SortedSet<XElement> Intent = new SortedSet<XElement>(new XElementComparer());

                foreach (XElement Attribute in Attributes)
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

        private readonly XDocument ContextDocument = null;
    }
}
