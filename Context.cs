using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        /// https://www.w3.org/TR/xmlschema-2/#boolean
        /// </summary>
        public static string W3boolFalseString => bool.FalseString.ToLower();

        /// <summary>
        /// https://www.w3.org/TR/xmlschema-2/#boolean
        /// </summary>
        public static string W3boolTrueString => bool.TrueString.ToLower();

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
        /// This static function validates the given XML file against an XSD format specification that is defined by <see cref="Cj.Fca.Context.XsdMarkup"/> as an embedded resource (Context.xsd).
        /// </summary>
        /// <param name="Protocol">Contains error messages if format errors are detected.</param>
        /// <param name="XmlFile">XML file to be checked.</param>
        /// <returns>True if there are no errors, otherwise false. Errors can be looked up in the protocol back from this procedure.</returns>
        public static bool Validate(out string Protocol, string XmlFile)
        {
            _ = new Context(out Protocol, XmlFile);

            return string.IsNullOrEmpty(Protocol);
        }

        /// <summary>
        /// This static function validates the given XML file against an XSD format specification that is defined by <see cref="Cj.Fca.Context.XsdMarkup"/> as an embedded resource (Context.xsd).
        /// </summary>
        /// <param name="Protocol">Contains error messages if format errors are detected.</param>
        /// <param name="XmlData">XML data to be checked.</param>
        /// <returns>True if there are no errors, otherwise false. Errors can be looked up in the protocol back from this procedure.</returns>
        public static bool Validate(out string Protocol, XDocument XmlData)
        {
            return new Context(out Protocol, XmlData) != null && string.IsNullOrEmpty(Protocol);
        }

        /// <summary>
        /// Generates an empty collection.
        /// </summary>
        /// <returns>Returns an empty collection.</returns>
        public static XElement[] Empty => Array.Empty<XElement>();

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Context()
        {
            ContextDocument = null;
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

                    if (false == Context.Validate(out string Protocol, this))
                    {
                        Debug.WriteLine($"{XmlFile}: {Protocol}");

                        ContextDocument = null;
                    }
                }
                catch (Exception AnyException)
                {
                    Debug.WriteLine($"{XmlFile}: {AnyException.Message}");

                    ContextDocument = null;
                }
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
        /// Creates a document by default with one context item.
        /// </summary>
        /// <param name="Title">Title of context.</param>
        /// <param name="SubTitle">Subtitle of context.</param>
        /// <param name="ObjectLabel">Object label of context item.</param>
        /// <param name="AttributeLabel">Attribute label of context item.</param>
        /// <param name="Value">Binary value of context item.</param>
        public Context(string Title, string SubTitle, string ObjectLabel, string AttributeLabel, bool Value)
        {
            XNamespace Cj = "https://github.com/formalconceptanalysis/Cj.Fca";

            ContextDocument = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement(new XElement("Data", new XAttribute("Format", "1.0.0"), new XAttribute(XNamespace.Xmlns + "cj", Cj), new XElement("Header", new XElement("Title", Title), new XElement("Subtitle", SubTitle)), new XElement("Context", new XElement("Declarations", new XElement("Attributes", new XElement("Attribute", new XAttribute("Index", 1), new XAttribute("Label", AttributeLabel))), new XElement("Objects", new XElement("Object", new XAttribute("Index", 1), new XAttribute("Label", ObjectLabel)))), new XElement("Items", new XElement("Item", new XElement("Object", new XAttribute("Index", 1)), new XElement("Attribute", new XAttribute("Index", 1)), new XElement("Value", Value ? W3boolTrueString : W3boolFalseString)))))));

            if (false == Context.Validate(out _, this))
                ContextDocument = null;
        }

        /// <summary>
        /// Checks whether context document is defined. 
        /// </summary>
        /// <returns>Returns true if context document is null, otherwise false.</returns>
        public bool IsNull() => ContextDocument == null;

        /// <summary>
        /// Returns the intended XML document.
        /// </summary>
        /// <returns>Returns the intended XML document.</returns>
        public override string ToString()
        {
            return ContextDocument?.ToString();
        }

        /// <summary>
        /// Saves context data to disk.
        /// </summary>
        /// <param name="XmlFile">File name.</param>
        public void Save(string XmlFile)
        {
            ContextDocument?.Save(XmlFile, SaveOptions.None);
        }

        /// <summary>
        /// XSD major version.
        /// </summary>
        /// <returns>Returns XSD major version.</returns>
        public int? XsdMajorVersion() => ContextDocument?.Element("Data")?.Attribute("Format")?.Value?.Split('.')?.Select(Value => int.Parse(Value))?.ElementAt(0);

        /// <summary>
        /// XSD minor version.
        /// </summary>
        /// <returns>Returns XSD minor version.</returns>
        public int? XsdMinorVersion() => ContextDocument?.Element("Data")?.Attribute("Format")?.Value?.Split('.')?.Select(Value => int.Parse(Value))?.ElementAt(1);

        /// <summary>
        /// XSD patch version.
        /// </summary>
        /// <returns>Returns XSD patch version.</returns>
        public int? XsdPatchVersion() => ContextDocument?.Element("Data")?.Attribute("Format")?.Value?.Split('.')?.Select(Value => int.Parse(Value))?.ElementAt(2);

        /// <summary>
        /// Base URI will be set by constructor if XML file is read.
        /// </summary>
        /// <returns>Document URI belonging to formal context if exists.</returns>
        public Uri DocumentUri() => string.IsNullOrEmpty(ContextDocument?.BaseUri) ? null : new Uri(ContextDocument.BaseUri ?? "");

        /// <summary>
        /// Path will be set by constructor if XML file is read.
        /// </summary>
        /// <returns>Document path in its unescaped representation belonging to formal context if exists.</returns>
        public string DocumentPath() => Uri.UnescapeDataString(DocumentUri()?.AbsolutePath ?? string.Empty);

        /// <summary>
        /// Calculates the maximum number of formal concepts.
        /// </summary>
        /// <returns>Maximum number of formal concepts.</returns>
        public ulong MaxCountOfConcepts() => (ulong)Math.Pow(2, GetAttributes().Length < GetObjects().Length ? GetAttributes().Length : GetObjects().Length);

        /// <summary>
        /// Calculates the maximum number of implications.
        /// </summary>
        /// <returns>Maximum number of implications.</returns>
        public ulong MaxCountOfImplications() => (ulong)Math.Pow(2, 2 * GetAttributes().Length);

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
            return await Task.Run(async () =>
            {
                var Concepts = new List<(XElement[] Extent, XElement[] Intent)>();

                if (Assert() is false)
                    return Concepts.ToArray();

                if (Kind == ItemKind.Attribute || (Kind == ItemKind.Generic && GetAttributes().Length <= GetObjects().Length))
                {
                    Concepts.Add(new ValueTuple<XElement[], XElement[]>(GetObjects(), await PrimeAsync(GetObjects(), ItemKind.Object)));

                    for (int OuterIndex = 1; OuterIndex <= GetAttributes().Length; OuterIndex++)
                    {
                        XElement[] A = await PrimeAsync(GetAttributes(OuterIndex), ItemKind.Attribute); // attribute extent

                        for (int InnerIndex = 0; InnerIndex < Concepts.Count; InnerIndex++)
                        {
                            XElement[] Extent = Concepts[InnerIndex].Extent.OrderBy(Item => Item.Element("Object"), new XElementComparer()).Intersect(A.OrderBy(Item => Item.Element("Object"), new XElementComparer()), new XElementEqualityComparer()).OrderBy(Item => Item.Element("Object"), new XElementComparer()).ToArray();

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
                                Concepts.Add(new ValueTuple<XElement[], XElement[]>(Extent, await PrimeAsync(Extent, ItemKind.Object)));

                                InnerIndex = -1;
                            }
                        }
                    }
                }

                if (Kind == ItemKind.Object || (Kind == ItemKind.Generic && GetAttributes().Length > GetObjects().Length))
                {
                    Concepts.Add(new ValueTuple<XElement[], XElement[]>(await PrimeAsync(GetAttributes(), ItemKind.Attribute), GetAttributes()));

                    for (int OuterIndex = 1; OuterIndex <= GetObjects().Length; OuterIndex++)
                    {
                        XElement[] B = await PrimeAsync(GetObjects(OuterIndex), ItemKind.Object); // object intent

                        for (int InnerIndex = 0; InnerIndex < Concepts.Count; InnerIndex++)
                        {
                            XElement[] Intent = Concepts[InnerIndex].Intent.OrderBy(Item => Item.Element("Attribute"), new XElementComparer()).Intersect(B.OrderBy(Item => Item.Element("Attribute"), new XElementComparer()), new XElementEqualityComparer()).OrderBy(Item => Item.Element("Attribute"), new XElementComparer()).ToArray();

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
                                Concepts.Add(new ValueTuple<XElement[], XElement[]>(await PrimeAsync(Intent, ItemKind.Attribute), Intent));

                                InnerIndex = -1;
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
            return await Task.Run(async () =>
            {
                var Concepts = new List<(XElement[] Extent, XElement[] Intent)>();

                if (Assert() is false)
                    return Concepts.ToArray();

                if (Kind == ItemKind.Attribute || (Kind == ItemKind.Generic && GetAttributes().Length <= GetObjects().Length))
                {
                    int[][] IntArray = await PowerSetAsync(GetAttributes().Length);

                    for (int Index = 0; Index < IntArray.Length; Index++)
                    {
                        XElement[] B = GetAttributes(IntArray[Index]);

                        XElement[] A = await PrimeAsync(B, ItemKind.Attribute);

                        if (B.OrderBy(Item => Item, new XElementComparer()).SequenceEqual(await PrimeAsync(A, ItemKind.Object), new XElementEqualityComparer()))
                            Concepts.Add(new ValueTuple<XElement[], XElement[]>(A, B));
                    }
                }

                if (Kind == ItemKind.Object || (Kind == ItemKind.Generic && GetAttributes().Length > GetObjects().Length))
                {
                    int[][] IntArray = await PowerSetAsync(GetObjects().Length);

                    for (int Index = 0; Index < IntArray.Length; Index++)
                    {
                        XElement[] A = GetObjects(IntArray[Index]);

                        XElement[] B = await PrimeAsync(A, ItemKind.Object);

                        if (A.OrderBy(Item => Item, new XElementComparer()).SequenceEqual(await PrimeAsync(B, ItemKind.Attribute), new XElementEqualityComparer()))
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
        /// <param name="MaxDegreeOfParallelism">Degree of parallelism to be used to find concepts concurrently.</param>
        /// <returns>Sorted list of concepts.</returns>
        public async Task<(XElement[] Extent, XElement[] Intent)[]> FindConceptsByPowerSetParallelAsync(CancellationTokenSource Cancellation = null, ItemKind Kind = ItemKind.Generic, int MaxDegreeOfParallelism = -1)
        {
            return await Task.Run(async () =>
            {
                var Concepts = new ConcurrentBag<(XElement[] Extent, XElement[] Intent)>();

                if (Assert() is false)
                    return Concepts.ToArray();

                if (Kind == ItemKind.Attribute || (Kind == ItemKind.Generic && GetAttributes().Length <= GetObjects().Length))
                {
                    int[][] IntArray = await PowerSetAsync(GetAttributes().Length);

                    try
                    {
                        Parallel.ForEach(

                            Partitioner.Create(0, IntArray.Length), Cancellation != null ? new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = Cancellation.Token } : new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, () => { return new Context(ContextDocument.Root); },

                            (Range, LoopState, Context) =>
                            {
                                if (Cancellation?.IsCancellationRequested == true)
                                    return Context;

                                for (int Index = Range.Item1; Index < Range.Item2; Index++)
                                {
                                    XElement[] B = Context.GetAttributes(IntArray[Index]);

                                    XElement[] A = Context.Prime(B, ItemKind.Attribute);

                                    if (B.OrderBy(Item => Item, new XElementComparer()).SequenceEqual(Context.Prime(A, ItemKind.Object), new XElementEqualityComparer()))
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
                    int[][] IntArray = await PowerSetAsync(GetObjects().Length);

                    try
                    {
                        Parallel.ForEach(

                            Partitioner.Create(0, IntArray.Length), Cancellation != null ? new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = Cancellation.Token } : new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, () => { return new Context(ContextDocument.Root); },

                            (Range, LoopState, Context) =>
                            {
                                if (Cancellation?.IsCancellationRequested == true)
                                    return Context;
                                try
                                {
                                    for (int Index = Range.Item1; Index < Range.Item2; Index++)
                                    {
                                        XElement[] A = Context.GetObjects(IntArray[Index]);

                                        XElement[] B = Context.Prime(A, ItemKind.Object);

                                        if (A.OrderBy(Item => Item, new XElementComparer()).SequenceEqual(Context.Prime(B, ItemKind.Attribute), new XElementEqualityComparer()))
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
        /// This method performs a comparison for all given items.
        /// </summary>
        /// <param name="ItemsToBeChecked">Items to be checked.</param>
        /// <param name="Items">Items in which is searched.</param>
        /// <returns>Returns true if item could be found, otherwise false.</returns>
        public static async Task<bool> ContainsAllAsync(XElement[] Items, params XElement[] ItemsToBeChecked)
        {
            return await Task.Run(() =>
            {
                if (ItemsToBeChecked == null || Items == null)
                    return false;

                foreach (XElement Item in ItemsToBeChecked)
                    if (!Items.Contains(Item, new XElementEqualityComparer()))
                        return false;

                return true; // Empty set is subset of every set.
            });
        }

        /// <summary>
        /// This method performs a comparison for all given concepts.
        /// </summary>
        /// <param name="Concept">Concept to be checked.</param>
        /// <param name="Concepts">Concept lattice in which is searched.</param>
        /// <returns>Returns true if formal concept could be found, otherwise false.</returns>
        public static async Task<bool> ContainsAllAsync((XElement[] Extent, XElement[] Intent)[] Concepts, params (XElement[] Extent, XElement[] Intent)[] Concept)
        {
            return await Task.Run(() =>
            {
                if (Concept == null || Concepts == null)
                    return false;

                foreach ((XElement[] Extent, XElement[] Intent) Item in Concept)
                    if (!Concepts.Contains(Item, new ConceptEqualityComparer()))
                        return false;

                return true;
            });
        }

        /// <summary>
        /// Compares to a list of formal concepts.
        /// </summary>
        /// <param name="ConceptLattice">Right list of formal concepts to be compared.</param>
        /// <returns>True if concept lattice equals right concept lattice based on the same context data, otherwise false.</returns>
        public async Task<bool> CompareConceptLatticeToAsync((XElement[] Extent, XElement[] Intent)[] ConceptLattice)
        {
            if (ConceptLattice == null)
                return false;

            (XElement[] Extent, XElement[] Intent)[] This = await FindConceptsByIterationAsync();

            if (This == null || This.Length != ConceptLattice.Length)
                return false;

            Array.Sort(This, Context.CompareConceptBySize);

            Array.Sort(ConceptLattice, Context.CompareConceptBySize);

            for (int Index = 0; Index < This.Length; Index++)
                if (await IsEqualAsync(This[Index], ConceptLattice[Index]) != true)
                    return false;

            return true;
        }

        /// <summary>
        /// Checks whether context document is valid.
        /// </summary>
        /// <returns>Returns true if context document is valid, otherwise false.</returns>
        public bool IsValid() => ContextDocument != null;

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
            return ContextDocument?.Element("Data").Element("Context").Element("Declarations").Element("Attributes").Elements("Attribute").OrderBy(Item => Item, new XElementComparer())?.ToArray();
        }

        /// <summary>
        /// Selected attribute declarations of given context document.
        /// </summary>
        /// <returns>Returns a sorted set of attribute declarations defined by an array of selected index positions. If an index does not exist, the return value is an empty array.</returns>
        public XElement[] GetAttributes(params int[] Indices)
        {
            SortedSet<XElement> Attributes = new(new XElementComparer());

            try
            {
                foreach (int Index in Indices)
                    Attributes.Add(ContextDocument?.Element("Data").Element("Context").Element("Declarations").Element("Attributes").Elements("Attribute").Where(Item => int.Parse(Item.Attribute("Index").Value) == Index).Single());
            }
            catch (Exception)
            {
                return null;
            }

            return Attributes.ToArray();
        }

        /// <summary>
        /// Object declarations of given context document.
        /// </summary>
        /// <returns>Returns the array of object declarations.</returns>
        public XElement[] GetObjects()
        {
            return ContextDocument?.Element("Data").Element("Context").Element("Declarations").Element("Objects").Elements("Object").OrderBy(Item => Item, new XElementComparer())?.ToArray();
        }

        /// <summary>
        /// Selected object declarations of given context document.
        /// </summary>
        /// <returns>Returns a sorted set of object declarations defined by an array of selected index positions. If an index does not exist, the return value is an empty array.</returns>
        public XElement[] GetObjects(params int[] Indices)
        {
            SortedSet<XElement> Objects = new(new XElementComparer());

            try
            {
                foreach (int Index in Indices)
                    Objects.Add(ContextDocument?.Element("Data").Element("Context").Element("Declarations").Element("Objects").Elements("Object").Where(Item => int.Parse(Item.Attribute("Index").Value) == Index).Single());
            }
            catch (Exception)
            {
                return null;
            }

            return Objects.ToArray();
        }

        /// <summary>
        /// Object declaration of given object by index position.
        /// </summary>
        /// <param name="Index">Requested index position.</param>
        /// <returns>Returns object declaration at index position.</returns>
        public XElement GetObject(int Index) => GetObjects(Index).Single();

        /// <summary>
        /// Attribute declaration of given attribute by index position.
        /// </summary>
        /// <param name="Index">Requested index position.</param>
        /// <returns>Returns attribute declaration at index position.</returns>
        public XElement GetAttribute(int Index) => GetAttributes(Index).Single();

        /// <summary>
        /// Row at given index position.
        /// </summary>
        /// <param name="Index">Index to be set in the range from 1 to n.</param>
        /// <returns>Returns the row that consists of all columns in an ascending order of column index that belong to the row position.</returns>
        public XElement[] Row(int Index)
        {
            return ContextDocument?.Element("Data").Element("Context").Element("Items").Elements("Item")?.Where(Item => int.Parse(Item.Element("Object").Attribute("Index").Value) == Index)?.OrderBy(Item => Item.Element("Attribute"), new XElementComparer()).ToArray();
        }

        /// <summary>
        /// Column at given index position.
        /// </summary>
        /// <param name="Index">Index to be set in the range from 1 to n.</param>
        /// <returns>Returns the column that consists of all rows in an ascending order of row index that belong to the row position.</returns>
        public XElement[] Column(int Index)
        {
            return ContextDocument?.Element("Data").Element("Context").Element("Items").Elements("Item")?.Where(Item => int.Parse(Item.Element("Attribute").Attribute("Index").Value) == Index)?.OrderBy(Item => Item.Element("Object"), new XElementComparer()).ToArray();
        }

        /// <summary>
        /// Cell at given row and column index position.
        /// </summary>
        /// <param name="RowIndex">Row index to be set in the range from 1 to n.</param>
        /// <param name="ColumnIndex">Column index to be set in the range from 1 to n.</param>
        /// <returns>Returns the cell at the specified row and column index position.</returns>
        public XElement Cell(int RowIndex, int ColumnIndex)
        {
            return ContextDocument?.Element("Data").Element("Context").Element("Items").Elements("Item")?.Where(Item => int.Parse(Item.Element("Object").Attribute("Index").Value) == RowIndex)?.OrderBy(Item => Item.Element("Attribute"), new XElementComparer()).Where(Item => int.Parse(Item.Element("Attribute").Attribute("Index").Value) == ColumnIndex).SingleOrDefault();
        }

        /// <summary>
        /// Boolean value at given cell position.
        /// </summary>
        /// <param name="RowIndex">Row index to be set in the range from 1 to n.</param>
        /// <param name="ColumnIndex">Column index to be set in the range from 1 to n.</param>
        /// <returns>Returns boolean value.</returns>
        /// <remarks>Throws an exception if cell is not defined.</remarks>
        public bool CellValue(int RowIndex, int ColumnIndex) => bool.Parse(Cell(RowIndex, ColumnIndex)?.Element("Value")?.Value);

        /// <summary>
        /// Boolean value at given cell position.
        /// </summary>
        /// <param name="RowIndex">Row index to be set in the range from 1 to n.</param>
        /// <param name="ColumnIndex">Column index to be set in the range from 1 to n.</param>
        /// <returns>Returns true if the value at the specified cell position is true, otherwise false.</returns>
        public bool IsTrue(int RowIndex, int ColumnIndex) => Cell(RowIndex, ColumnIndex)?.Element("Value")?.Value == W3boolTrueString;

        /// <summary>
        /// Boolean value at given cell position.
        /// </summary>
        /// <param name="RowIndex">Row index to be set in the range from 1 to n.</param>
        /// <param name="ColumnIndex">Column index to be set in the range from 1 to n.</param>
        /// <returns>Returns true if the value at the specified cell position is false, otherwise false.</returns>
        public bool IsFalse(int RowIndex, int ColumnIndex) => !IsTrue(RowIndex, ColumnIndex);

        /// <summary>
        /// Checks primary keys if there are exist.
        /// </summary>
        /// <param name="Kind">Kind of context item to be checked.</param>
        /// <returns>True if successful, otherwise false in case of unresolved or dangling keys.</returns>
        public async Task<bool> CheckPrimaryKeysAsync(ItemKind Kind = ItemKind.Generic)
        {
            if (Kind == ItemKind.Object)
                return await CheckPrimaryKeysForObjectsAsync() == 0;

            if (Kind == ItemKind.Attribute)
                return await CheckPrimaryKeysForAttributesAsync() == 0;

            if (Kind == ItemKind.Generic)
                return await CheckPrimaryKeysForObjectsAsync() + await CheckPrimaryKeysForAttributesAsync() == 0;

            return true;
        }

        private Context(out string Protocol, string XmlFile)
        {
            try
            {
                Validate(out Protocol, new Context(XDocument.Load(XmlFile, LoadOptions.SetBaseUri).Root));
            }
            catch (Exception AnyException)
            {
                Protocol = AnyException.Message;
            }
        }

        private Context(out string Protocol, XDocument Data)
        {
            Validate(out Protocol, new Context(Data.Root));
        }

        private Context(XElement ContextRoot)
        {
            if (ContextRoot != null)
            {
                /* BaseUri will not be set if available. */

                ContextDocument = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), ContextRoot);
            }
            else
                ContextDocument = null;
        }

        private Context(Uri BaseUri)
        {
            if (BaseUri.IsFile)
            {
                if (System.IO.File.Exists(BaseUri.LocalPath))
                    ContextDocument = XDocument.Load(BaseUri.LocalPath, LoadOptions.SetBaseUri);
                else
                    ContextDocument = new XDocument();
            }
        }

        private static bool Validate(out string Protocol, Context Document)
        {
            var SchemaSet = new XmlSchemaSet();

            Protocol = null;

            try
            {
                SchemaSet.Add("", XmlReader.Create(new System.IO.StringReader(XsdMarkup())));
            }
            catch (Exception AnyException)
            {
                Protocol = AnyException.Message;

                return false;
            }

            var Log = new StringBuilder();

            Document.ContextDocument.Validate(SchemaSet, (Item, AnyException) => { Log.AppendLine(AnyException.Message); });

            if (Log.Length > 0)
                Protocol = Log.ToString();

            return string.IsNullOrEmpty(Protocol);
        }

        private bool IsStandard()
        {
            if (!Assert())
                return false;

            if (ContextDocument?.Element("Data").Element("Context").Attribute("Status")?.Value == "Standard")
                return true;
            else
                return false;
        }

        private bool CheckIndex(int Index, ItemKind Kind)
        {
            if (Index <= 0)
                return false;
            else if (ItemKind.Object == Kind && Index <= GetObjects().Length)
                return true;
            else if (ItemKind.Attribute == Kind && Index <= GetAttributes().Length)
                return true;
            else
                return false;
        }

        private async Task<(XElement[] Extent, XElement[] Intent)> ClosureAsync(XElement[] Items, ItemKind ObjectOrAttribute)
        {
            if (Items.Length == 0 && ObjectOrAttribute == ItemKind.Generic)
                return (null, null);

            if ((Items.All(Item => Item.Name.LocalName == "Attribute") && Items.Length != 0) || (Items.Length == 0 && ObjectOrAttribute == ItemKind.Attribute))
                return (await PrimeAsync(Items, ItemKind.Attribute), await PrimeAsync(await PrimeAsync(Items, ItemKind.Attribute), ItemKind.Object));

            if ((Items.All(Item => Item.Name.LocalName == "Object") && Items.Length != 0) || (Items.Length == 0 && ObjectOrAttribute == ItemKind.Object))
                return (await PrimeAsync(await PrimeAsync(Items, ItemKind.Object), ItemKind.Attribute), await PrimeAsync(Items, ItemKind.Object));

            return (null, null);
        }

        private async Task<XElement[]> ClosureAsync(XElement[] Union, XElement Item)
        {
            if (Item.Name.LocalName == "Object")
                return (await ClosureAsync(Union, ItemKind.Generic)).Extent;

            if (Item.Name.LocalName == "Attribute")
                return (await ClosureAsync(Union, ItemKind.Generic)).Intent;

            return null;
        }

        private static int CompareConceptsBySize((XElement[] Extent, XElement[] Intent) Left, (XElement[] Extent, XElement[] Intent) Right)
        {
            if (Left.Extent.Length < Right.Extent.Length) return +1;      // x < y
            else if (Left.Extent.Length > Right.Extent.Length) return -1; // x > y
            else if (Left.Intent.Length > Right.Intent.Length) return +1; // x < y
            else if (Left.Intent.Length < Right.Intent.Length) return -1; // x > y
            else
            {
                #region Compared by index position

                for (int Index = 0; Index < Left.Intent.Length; Index++)
                    if (int.Parse(Left.Intent[Index].Attribute("Index").Value) < int.Parse(Right.Intent[Index].Attribute("Index").Value)) return -1;
                    else if (int.Parse(Left.Intent[Index].Attribute("Index").Value) > int.Parse(Right.Intent[Index].Attribute("Index").Value)) return +1;

                for (int Index = 0; Index < Left.Extent.Length; Index++)
                    if (int.Parse(Left.Extent[Index].Attribute("Index").Value) < int.Parse(Right.Extent[Index].Attribute("Index").Value)) return +1;
                    else if (int.Parse(Left.Extent[Index].Attribute("Index").Value) > int.Parse(Right.Extent[Index].Attribute("Index").Value)) return -1;

                #endregion

                return 0;
            }
        }

        private static bool IsLessThanOrEqual((XElement[] Extent, XElement[] Intent) Concept1, (XElement[] Extent, XElement[] Intent) Concept2)
        {
            return IsSubset(Concept1.Extent, Concept2.Extent) || IsSubset(Concept2.Intent, Concept1.Intent);
        }

        private static string XsdMarkup()
        {
            var ResourceAssembly = Assembly.GetExecutingAssembly();

            string ResourceString = ResourceAssembly.GetName().Name + ".Assets.Context.xsd";

            using var ResourceStream = ResourceAssembly.GetManifestResourceStream(ResourceString);

            using var Reader = new StreamReader(ResourceStream);

            string XsdMarkup = Reader.ReadToEnd();

            // Manual of XSD regular expressions https://www.w3.org/TR/xmlschema-2/#regexs
            return XsdMarkup;
        }

        private bool Assert()
        {
            if (!IsValid() || GetAttributes() == null || GetObjects() == null)
                return false;

            return Validate(out _, ContextDocument) && CheckXsdVersion() && CheckStatusConsistency() && CheckDocumentIndexOrder();
        }

        private static Context Assert(Context ToBeChecked)
        {
            if (!ToBeChecked.IsValid() || ToBeChecked.GetAttributes() == null || ToBeChecked.GetObjects() == null)
                return null;

            if (Validate(out _, ToBeChecked) && ToBeChecked.CheckXsdVersion() && ToBeChecked.CheckStatusConsistency() && ToBeChecked.CheckDocumentIndexOrder())
                return ToBeChecked;
            else
                return null;
        }

        private static IEnumerable<XElement> ToEnumerable<XElement>(XElement Item)
        {
            return new XElement[] { Item };
        }

        private async Task<int> CheckPrimaryKeysForObjectsAsync()
        {
            return await Task.Run(() =>
            {
                for (int Index = 1; Index < GetObjects().Length; Index++)
                {
                    bool? CheckCondition = GetObjectSource(Index)?.Element("KeyWords")?.Elements("KeyWord")?.Elements("PrimaryKey").Any();

                    if (CheckCondition == null || CheckCondition == false)
                        return 0;

                    var PrimaryKeys = GetObjectSource(Index).Element("KeyWords").Elements("KeyWord").Select(Item => Item.Element("PrimaryKey")?.Value);

                    XElement[] Columns = Row(Index);

                    foreach (var Column in Columns)
                    {
                        string PrimaryKey = GetAttributes(int.Parse(Column.Element("Attribute").Attribute("Index").Value)).Single().Element("PrimaryKey")?.Value;

                        if (string.IsNullOrEmpty(PrimaryKey))
                            continue;

                        if (Column.Element("Value").Value == W3boolTrueString)
                        {
                            if (PrimaryKeys.Contains(PrimaryKey) is false)
                                return Index;
                        }
                        else
                        {
                            if (PrimaryKeys.Contains(PrimaryKey) is true)
                                return Index;
                        }
                    }
                }

                return 0;
            });
        }

        private async Task<int> CheckPrimaryKeysForAttributesAsync()
        {
            return await Task.Run(() =>
            {
                for (int Index = 1; Index < GetAttributes().Length; Index++)
                {
                    bool? CheckCondition = GetAttributeSource(Index)?.Element("KeyWords")?.Elements("KeyWord")?.Elements("PrimaryKey").Any();

                    if (CheckCondition == null || CheckCondition == false)
                        return 0;

                    var PrimaryKeys = GetAttributeSource(Index).Element("KeyWords").Elements("KeyWord").Select(Item => Item.Element("PrimaryKey")?.Value);

                    XElement[] Rows = Column(Index);

                    foreach (var Row in Rows)
                    {
                        string PrimaryKey = GetObjects(int.Parse(Row.Element("Object").Attribute("Index").Value)).Single().Element("PrimaryKey")?.Value;

                        if (string.IsNullOrEmpty(PrimaryKey))
                            continue;

                        if (Row.Element("Value").Value == W3boolTrueString)
                        {
                            if (PrimaryKeys.Contains(PrimaryKey) is false)
                                return Index;
                        }
                        else
                        {
                            if (PrimaryKeys.Contains(PrimaryKey) is true)
                                return Index;
                        }
                    }
                }

                return 0;
            });
        }

        private XElement GetObjectSource(int Index) => ContextDocument?.Element("Data").Element("Context").Element("Declarations").Element("Objects").Elements("Object").Where(Item => int.Parse(Item.Attribute("Index").Value) == Index).Single().Element("Source");

        private XElement GetAttributeSource(int Index) => ContextDocument?.Element("Data").Element("Context").Element("Declarations").Element("Attributes").Elements("Attribute").Where(Item => int.Parse(Item.Attribute("Index").Value) == Index).Single().Element("Source");

        private static bool IsProperSubset(XElement[] Left, XElement[] Right)
        {
            if (new SortedSet<XElement>(Left, new XElementComparer()).IsProperSubsetOf(new SortedSet<XElement>(Right, new XElementComparer())))
                return true;
            else
                return false;
        }

        private static bool IsSubset(XElement[] Left, XElement[] Right)
        {
            if (new SortedSet<XElement>(Left, new XElementComparer()).IsSubsetOf(new SortedSet<XElement>(Right, new XElementComparer())))
                return true;
            else
                return false;
        }

        private static XElement[] FindSubset(XElement[] Intent, params int[] Indices)
        {
            SortedSet<XElement> Attributes = new(new XElementComparer());

            foreach (int Index in Indices)
                Attributes.Add(Intent[Index - 1]);

            return Attributes.ToArray();
        }

        private bool CheckStatusConsistency()
        {
            if (ContextDocument.Element("Data").Element("Context").Attribute("Status")?.Value == "Standard")
            {
                if (ContextDocument.Element("Data").Element("Context").Element("Status")?.Element("Clarified") != null)
                    return false;

                if (ContextDocument.Element("Data").Element("Context").Element("Status")?.Element("Reduced") != null)
                    return false;
            }

            return true;
        }

        private bool CheckXsdVersion()
        {
            int? XsdMajorVersion = this.XsdMajorVersion();

            if (XsdMajorVersion.HasValue && 1 == XsdMajorVersion)
                return true;
            else
                return false;
        }

        private bool CheckDocumentIndexOrder()
        {
            if (CheckObjectDeclarationIndexOrder() is false || CheckAttributeDeclarationIndexOrder() is false)
                return false;

            return CheckItemIndexOrder();
        }

        private bool CheckAttributeDeclarationIndexOrder()
        {
            XNode Node = ContextDocument.Element("Data").Element("Context").Element("Declarations").Element("Attributes")?.FirstNode;

            int CheckIndex = 0;

            while (Node != null)
            {
                if (Node.NodeType == XmlNodeType.Element)
                {
                    XElement Item = XElement.Parse(Node.ToString());

                    if (Item.Name == "Attribute")
                    {
                        if (int.Parse(Item.Attribute("Index").Value) != ++CheckIndex)
                            return false;
                    }
                }

                Node = Node.NextNode;
            }

            return CheckIndex == GetAttributes().Length;
        }

        private bool CheckObjectDeclarationIndexOrder()
        {
            XNode Node = ContextDocument.Element("Data").Element("Context").Element("Declarations").Element("Objects")?.FirstNode;

            int CheckIndex = 0;

            while (Node != null)
            {
                if (Node.NodeType == XmlNodeType.Element)
                {
                    XElement Item = XElement.Parse(Node.ToString());

                    if (Item.Name == "Object")
                    {
                        if (int.Parse(Item.Attribute("Index").Value) != ++CheckIndex)
                            return false;
                    }
                }

                Node = Node.NextNode;
            }

            return CheckIndex == GetObjects().Length;
        }

        private bool CheckItemIndexOrder()
        {
            XNode Node = ContextDocument.Element("Data").Element("Context").Element("Items")?.FirstNode;

            int CheckObjectIndex = 1;

            int CheckAttributeIndex = 0;

            int CountAttributes = GetAttributes().Length;

            while (Node != null)
            {
                if (Node.NodeType == XmlNodeType.Element)
                {
                    XElement Item = XElement.Parse(Node.ToString());

                    if (Item.Name == "Item")
                    {
                        if (int.Parse(Item.Element("Object").Attribute("Index").Value) != CheckObjectIndex)
                            return false;

                        if (int.Parse(Item.Element("Attribute").Attribute("Index").Value) != ++CheckAttributeIndex)
                            return false;

                        /* Reset attribute counter for each row. */

                        if (CountAttributes == CheckAttributeIndex)
                        {
                            CheckAttributeIndex = 0;

                            CheckObjectIndex++;
                        }
                    }
                }

                Node = Node.NextNode;
            }

            return true;
        }

        private static async Task<int[][]> PowerSetAsync(int Count)
        {
            return await Task.Run(() =>
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

                    int Index = Count;

                    while (Set.Contains(Index))
                        Set.Remove(Index--);

                    Set.Add(Index);

                } while (Set.Count != Count);

                #endregion

                IntList.Add(Set.ToArray());

                return IntList.Count == 1 << Count ? IntList.ToArray() : null;
            });
        }

        private static async Task<int[][]> SubsetsAsync(int N, int K)
        {
            int[] Coefficients;

            return await Task.Run(async () =>
            {
                List<int[]> IntList = new();

                Coefficients = new int[N + 1];

                Coefficients[0] = await BinominalCoefficientsAsync(N, K);

                for (int Index = 1; Index <= N; Index++)
                    Coefficients[Index] = Index;

                for (int Index = 1; Index <= Coefficients[0]; Index++)
                {
                    int[] Subset = new int[K];

                    Array.ConstrainedCopy(Coefficients, 1, Subset, 0, K);

                    IntList.Add(Subset);

                    if (IntList.Count == Coefficients[0])
                        return IntList.ToArray();

                    NextSubset(N, K);
                }

                return null;
            });

            void NextSubset(int LocalN, int LocalK)
            {
                int Size = K;

                while (Coefficients[Size] == LocalN - LocalK + Size)
                    Size--;

                Coefficients[Size] = Coefficients[Size] + 1;

                for (int Index = Size + 1; Index <= K; Index++)
                    Coefficients[Index] = Coefficients[Index - 1] + 1;
            }
        }

        private static async Task<int> BinominalCoefficientsAsync(int N, int K)
        {
            return await Task.Run(() =>
            {
                int[] PascalsTriangle = new int[N + 1];

                PascalsTriangle[0] = 1;

                for (int Row = 1; Row <= N; Row++)
                {
                    PascalsTriangle[Row] = 1;

                    for (int Index = Row - 1; Index >= 1; Index--)
                        PascalsTriangle[Index] = PascalsTriangle[Index] + PascalsTriangle[Index - 1];
                }

                return PascalsTriangle[K];
            });
        }

        private async Task<XElement[]> PrimeAsync(XElement[] Items, ItemKind ObjectOrAttribute)
        {
            return await Task.Run(() =>
            {
                return Prime(Items, ObjectOrAttribute);
            });
        }

        private XElement[] Prime(XElement[] Items, ItemKind ObjectOrAttribute)
        {
            /* All returns true if set is empty. */
            if ((Items.All(Item => Item.Name.LocalName == "Attribute") && Items.Length != 0) || (Items.Length == 0 && ObjectOrAttribute == ItemKind.Attribute))
            {
                XElement[] Objects = GetObjects();

                var Extent = new SortedSet<XElement>(new XElementComparer());

                foreach (var Object in Objects)
                    if (Row(int.Parse(Object.Attribute("Index").Value)).Where(Item => Item.Element("Value").Value == W3boolTrueString).OrderBy(Item => Item.Element("Attribute"), new XElementComparer()).Select(Item => Item.Element("Attribute")).Intersect(Items.OrderBy(Item => Item, new XElementComparer()), new XElementEqualityComparer()).OrderBy(Item => Item, new XElementComparer()).SequenceEqual(Items.OrderBy(Item => Item, new XElementComparer()).ToArray(), new XElementEqualityComparer()))
                        Extent.Add(Object);

                return Extent.ToArray();
            }

            /* All returns true if set is empty. */
            if ((Items.All(Item => Item.Name.LocalName == "Object") && Items.Length != 0) || (Items.Length == 0 && ObjectOrAttribute == ItemKind.Object))
            {
                XElement[] Attributes = GetAttributes();

                var Intent = new SortedSet<XElement>(new XElementComparer());

                foreach (var Attribute in Attributes)
                    if (Column(int.Parse(Attribute.Attribute("Index").Value)).Where(Item => Item.Element("Value").Value == W3boolTrueString).OrderBy(Item => Item.Element("Object"), new XElementComparer()).Select(Item => Item.Element("Object")).Intersect(Items.OrderBy(Item => Item, new XElementComparer()), new XElementEqualityComparer()).OrderBy(Item => Item, new XElementComparer()).SequenceEqual(Items.OrderBy(Item => Item, new XElementComparer()).ToArray(), new XElementEqualityComparer()))
                        Intent.Add(Attribute);

                return Intent.ToArray();
            }

            return null;
        }

        private async Task<bool> IsEqualAsync((XElement[] Extent, XElement[] Intent) Left, (XElement[] Extent, XElement[] Intent) Right)
        {
            return await Task.Run(() =>
            {
                if (ToBinaryValue(Left.Extent, ItemKind.Object) != ToBinaryValue(Right.Extent, ItemKind.Object))
                    return false;

                if (ToBinaryValue(Left.Intent, ItemKind.Attribute) != ToBinaryValue(Right.Intent, ItemKind.Attribute))
                    return false;

                return true;
            });
        }

        private class XElementEqualityComparer : EqualityComparer<XElement>
        {
            public override bool Equals(XElement Left, XElement Right)
            {
                if (Object.ReferenceEquals(Left, Right)) return true;

                return Left.Name.LocalName == Right.Name.LocalName && int.Parse(Left.Attribute("Index").Value) == int.Parse(Right.Attribute("Index").Value);
            }

            public override int GetHashCode(XElement Item)
            {
                return Item.Attribute("Index").Value.GetHashCode();
            }
        }

        private class ConceptEqualityComparer : EqualityComparer<(XElement[] Extent, XElement[] Intent)>
        {
            public override bool Equals((XElement[] Extent, XElement[] Intent) Left, (XElement[] Extent, XElement[] Intent) Right)
            {
                if (Left == (null, null) && Right == (null, null))
                    return true;
                else if (Left == (null, null) || Right == (null, null))
                    return false;
                else if (Left.Extent.Length != Right.Extent.Length)
                    return false;
                else if (Left.Intent.Length != Right.Intent.Length)
                    return false;
                else if (!Left.Extent.OrderBy(Item => Item, new XElementComparer()).SequenceEqual(Right.Extent.OrderBy(Item => Item, new XElementComparer()), new XElementEqualityComparer()))
                    return false;
                else if (!Left.Intent.OrderBy(Item => Item, new XElementComparer()).SequenceEqual(Right.Intent.OrderBy(Item => Item, new XElementComparer()), new XElementEqualityComparer()))
                    return false;
                else
                    return true;
            }

            public override int GetHashCode([DisallowNull] (XElement[] Extent, XElement[] Intent) Item)
            {
                throw new NotImplementedException();
            }
        }

        private class XElementComparer : Comparer<XElement>
        {
            public override int Compare(XElement Left, XElement Right)
            {
                return Context.Compare(Left, Right);
            }
        }

        private static int Compare(XElement Left, XElement Right)
        {
            if (object.Equals(Left, Right)) // If the current instance is a reference type, the Equals(Object) method tests for reference equality, and a call to the Equals(Object) method is equivalent to a call to the ReferenceEquals method.
                return 0;
            else if (Left is null || Right is null)
                return 0;
            else
                return int.Parse(Left.Attribute("Index").Value).CompareTo(int.Parse(Right.Attribute("Index").Value));
        }

        private static int CompareConceptBySize((XElement[] Extent, XElement[] Intent) Left, (XElement[] Extent, XElement[] Intent) Right)
        {
            if (Left.Extent.Length < Right.Extent.Length) return +1;      // x < y
            else if (Left.Extent.Length > Right.Extent.Length) return -1; // x > y
            else if (Left.Intent.Length > Right.Intent.Length) return +1; // x < y
            else if (Left.Intent.Length < Right.Intent.Length) return -1; // x > y
            else
            {
                #region Compared by index position

                for (int Index = 0; Index < Left.Intent.Length; Index++)
                    if (int.Parse(Left.Intent[Index].Attribute("Index").Value) < int.Parse(Right.Intent[Index].Attribute("Index").Value)) return -1;
                    else if (int.Parse(Left.Intent[Index].Attribute("Index").Value) > int.Parse(Right.Intent[Index].Attribute("Index").Value)) return +1;

                for (int Index = 0; Index < Left.Extent.Length; Index++)
                    if (int.Parse(Left.Extent[Index].Attribute("Index").Value) < int.Parse(Right.Extent[Index].Attribute("Index").Value)) return +1;
                    else if (int.Parse(Left.Extent[Index].Attribute("Index").Value) > int.Parse(Right.Extent[Index].Attribute("Index").Value)) return -1;

                #endregion

                return 0;
            }
        }

        private static bool IsEqual((XElement[] Extent, XElement[] Intent) Left, (XElement[] Extent, XElement[] Intent) Right)
        {
            return CompareConceptBySize(Left, Right) == 0;
        }

        private int CompareByBinaryOrder(XElement[] Left, XElement[] Right)
        {
            if (Left == null)
            {
                return Right == null ? 0 : -1;
            }
            else
            {
                if (Right == null)
                {
                    return +1;
                }
                else
                {
                    if (ToBinaryValue(Left) < ToBinaryValue(Right)) return -1;
                    else if (ToBinaryValue(Left) > ToBinaryValue(Right)) return +1;
                    else return 0;
                }
            }
        }

        private ulong ToBinaryValue(XElement[] Items, ItemKind Kind)
        {
            if (!Assert() || Items == null || Items.Length == 0 || ItemKind.Generic == Kind) return 0;
            else
            {
                ulong Value = 0; int Length = 0;

                switch (Kind)
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
                    Value += (1ul << (Length - int.Parse(Item.Attribute("Index").Value)));

                // 1 << 1 = 2
                // 1 << 2 = 4
                // 1 << 3 = 8
                // 1 << 4 = ...

                return Value;
            }
        }

        private ulong ToBinaryValue(XElement[] Items)
        {
            if (Items == null || Items.Length == 0) return 0;
            else
            {
                ulong Value = 0;

                foreach (XElement Item in Items)
                    Value += (1ul << (GetAttributes().Length - int.Parse(Item?.Attribute("Index")?.Value)));

                // 1 << 1 = 2
                // 1 << 2 = 4
                // 1 << 3 = 8
                // 1 << 4 = ...

                return Value;
            }
        }

        private int CompareByCardinality(XElement[] Left, XElement[] Right)
        {
            if (Left == null)
            {
                return Right == null ? 0 : -1;
            }
            else
            {
                if (Right == null)
                {
                    return +1;
                }
                else
                {
                    if (Left.Length < Right.Length) return -1;
                    else if (Left.Length > Right.Length) return +1;
                    else
                    {
                        /* partially inverted order */
                        return -CompareByBinaryOrder(Left, Right);
                    }
                }
            }
        }

        private readonly XDocument ContextDocument = null;
    }
}
