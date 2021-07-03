using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Cj.Fca
{
    /// <summary>
    /// Some helper functions.
    /// </summary>
    public static class ContextExtensions
    {
        /// <summary>
        /// Converts protocol strings to simple HTML page with fixed font face set to Courier New.
        /// </summary>
        /// <param name="Protocol">Text to be converted.</param>
        /// <param name="Title">Title of HTML page.</param>
        /// <param name="Wrap">Determines if text should be wrapped.</param>
        /// <returns>HTML text that can be shown by a browser.</returns>
        public static string[] ToHtml(string[] Protocol, string Title = "", bool Wrap = false)
        {
            #region HTML // http://www.w3schools.com

            var ToHtml = new List<string>(Protocol);

            for (int Index = 0; Index < ToHtml.Count; Index++)
                ToHtml[Index] = ToHtml[Index] + "<br>";

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
        /// Returns a string representation of an item according to the XSD standard.
        /// </summary>
        /// <param name="Item">Attribute or object item to be converted.</param>
        /// <param name="Kind">Kind of context item.</param>
        /// <returns></returns>
        public static string FormatItem(XElement Item, Context.ItemKind Kind)
        {
            return Kind switch
            {
                Cj.Fca.Context.ItemKind.Attribute => FormatAttribute(Item),
                Cj.Fca.Context.ItemKind.Object => FormatObject(Item),
                _ => string.Empty
            };
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

            ItemString.Append('{');

            foreach (var Item in Items)
            {
                if (WithLabel && Item.Attribute("Label") != null)
                    ItemString.Append($"{Item.Attribute("Label").Value},");
                else
                    ItemString.Append($"{Item.Attribute("Index").Value},");
            }

            if (ItemString.ToString().Last() == ',')
                ItemString.Remove(ItemString.Length - 1, 1);

            ItemString.Append('}');

            return ItemString.ToString().Replace(",", ", ");
        }

        /// <summary>
        /// Serializes an XML item to a string representation. It can be used to convert labels to a string object.
        /// </summary>
        /// <see cref="ToHtml(string[], string, bool)"/>
        /// <param name="Item">Item to be converted to string representation.</param>
        /// <param name="WithLabel">True if label should be written instead of index.</param>
        /// <returns>Serialized XML item to string.</returns>
        public static string ToString(XElement Item, bool WithLabel = false)
        {
            var ItemString = new StringBuilder();

            ItemString.Append('{');

            if (Item != null)
            {
                if (WithLabel && Item.Attribute("Label") != null)
                    ItemString.Append($"{Item.Attribute("Label").Value}");
                else
                    ItemString.Append($"{Item.Attribute("Index").Value}");
            }

            ItemString.Append('}');

            return ItemString.ToString();
        }

        /// <summary>
        /// Serializes an XML vector tuple to a string representation. It can be used to convert concept items to a string object.
        /// </summary>
        /// <param name="Extent">Extent items to be converted to string representation.</param>
        /// <param name="Intent">Intent items to be converted to string representation.</param>
        /// <param name="WithLabel">True if label should be written instead of index.</param>
        /// <returns>Serialized XML tuple to string.</returns>
        public static string ToString(XElement[] Extent, XElement[] Intent, bool WithLabel = false)
        {
            return ContextExtensions.ToString(Extent) + ' ' + ContextExtensions.ToString(Intent, WithLabel);
        }

        /// <summary>
        /// Serializes an XML tuple to a string representation. It can be used to convert concept items to a string object.
        /// </summary>
        /// <param name="DataContext">Given formal context.</param>
        /// <param name="ConceptWithLabels">Given concept lattice with reduced labeling.</param>
        /// <returns></returns>
        public static string ToString(this Context DataContext, ((XElement[] Extent, XElement[] Intent) Concept, (int? ObjectIndexLabel, int? AttributeIndexLabel) Labels) ConceptWithLabels)
        {
            var ConceptString = new StringBuilder();

            ConceptString.Append(ContextExtensions.ToString(ConceptWithLabels.Concept.Extent));
            ConceptString.Append(' ');
            ConceptString.Append(ContextExtensions.ToString(ConceptWithLabels.Concept.Intent, true));

            ConceptString.Append(" Labels: ");

            ConceptString.Append(ToString(ConceptWithLabels.Labels.ObjectIndexLabel != null ? DataContext.GetObject((int)ConceptWithLabels.Labels.ObjectIndexLabel) : null));
            ConceptString.Append(' ');
            ConceptString.Append(ToString(ConceptWithLabels.Labels.AttributeIndexLabel != null ? DataContext.GetAttribute((int)ConceptWithLabels.Labels.AttributeIndexLabel) : null, true));

            return ConceptString.ToString();
        }

        /// <summary>
        /// Serializes an implication to a string representation. An implication consists of two sets where A' is a subset ob B'.
        /// </summary>
        /// <param name="Implication">Implication to be converted to string representation.</param>
        /// <param name="WithSupportItems">True if support items should be enumerated, otherwise false.</param>
        /// <returns>Serialized XML tuple to string.</returns>
        public static string ToString((XElement[] Premise, XElement[] Conclusion, XElement[] Support) Implication, bool WithSupportItems = false)
        {
            var ImplicationString = new StringBuilder();

            ImplicationString.Append($"<{Implication.Support.Length}>");

            if (Implication.Premise != null && Implication.Conclusion != null)
            {
                ImplicationString.Append(' ');
                ImplicationString.Append('{');

                foreach (var Item in Implication.Premise)
                    if (Item.Attribute("Label") != null)
                        ImplicationString.Append($"{Item.Attribute("Label").Value},");
                    else
                        ImplicationString.Append($"{Item.Attribute("Index").Value},");

                if (ImplicationString.ToString().Last() == ',')
                    ImplicationString.Remove(ImplicationString.Length - 1, 1);

                ImplicationString.Append("} => {");

                foreach (var Item in Implication.Conclusion)
                    if (Item.Attribute("Label") != null)
                        ImplicationString.Append($"{Item.Attribute("Label").Value},");
                    else
                        ImplicationString.Append($"{Item.Attribute("Index").Value},");

                if (ImplicationString.ToString().Last() == ',')
                    ImplicationString.Remove(ImplicationString.Length - 1, 1);

                if (2 <= ImplicationString.Length)
                    ImplicationString.Append('}');
            }

            if (Implication.Premise != null && Implication.Conclusion == null)
            {
                ImplicationString.Append(' ');
                ImplicationString.Append('{');

                foreach (var Item in Implication.Premise)
                    if (Item.Attribute("Label") != null)
                        ImplicationString.Append($"{Item.Attribute("Label").Value},");
                    else
                        ImplicationString.Append($"{Item.Attribute("Index").Value},");

                if (ImplicationString.ToString().Last() == ',')
                    ImplicationString.Remove(ImplicationString.Length - 1, 1);

                ImplicationString.Append("} => ⊥");
            }

            if (WithSupportItems)
            {
                ImplicationString.Append(' ');
                ImplicationString.Append('{');

                foreach (var Item in Implication.Support)
                    ImplicationString.Append($"{Item.Attribute("Index").Value},");

                if (ImplicationString.ToString().Last() == ',')
                    ImplicationString.Remove(ImplicationString.Length - 1, 1);

                ImplicationString.Append('}');
            }

            return ImplicationString.ToString().Replace(",", ", ");
        }

        /// <summary>
        /// Serializes an implication to a string representation. An implication consists of two sets where A' is a subset ob B'.
        /// </summary>
        /// <param name="Implication">Implication to be converted to string representation.</param>
        /// <returns>Serialized XML tuple to string.</returns>
        public static string ToString((XElement[] Premise, XElement[] Consequence) Implication)
        {
            var ImplicationString = new StringBuilder();

            if (Implication.Premise != null && Implication.Consequence != null)
            {
                ImplicationString.Append('{');

                foreach (var Item in Implication.Premise)
                    if (Item.Attribute("Label") != null)
                        ImplicationString.Append($"{Item.Attribute("Label").Value},");
                    else
                        ImplicationString.Append($"{Item.Attribute("Index").Value},");

                if (ImplicationString.ToString().Last() == ',')
                    ImplicationString.Remove(ImplicationString.Length - 1, 1);

                ImplicationString.Append("} => {");

                foreach (var Item in Implication.Consequence)
                    if (Item.Attribute("Label") != null)
                        ImplicationString.Append($"{Item.Attribute("Label").Value},");
                    else
                        ImplicationString.Append($"{Item.Attribute("Index").Value},");

                if (ImplicationString.ToString().Last() == ',')
                    ImplicationString.Remove(ImplicationString.Length - 1, 1);

                if (2 <= ImplicationString.Length)
                    ImplicationString.Append('}');
            }

            if (Implication.Premise != null && Implication.Consequence == null)
            {
                ImplicationString.Append('{');

                foreach (var Item in Implication.Premise)
                    if (Item.Attribute("Label") != null)
                        ImplicationString.Append($"{Item.Attribute("Label").Value},");
                    else
                        ImplicationString.Append($"{Item.Attribute("Index").Value},");

                if (ImplicationString.ToString().Last() == ',')
                    ImplicationString.Remove(ImplicationString.Length - 1, 1);

                ImplicationString.Append("} => ⊥");
            }

            return ImplicationString.ToString().Replace(",", ", ");
        }

        /// <summary>
        /// Writes context data to disc.
        /// </summary>
        /// <param name="DataContext">Given formal context.</param>
        public static async Task SaveAsHtmlAsync(this Context DataContext)
        {
            if (File.Exists(DataContext.DocumentUri().LocalPath))
                await File.WriteAllLinesAsync(Path.ChangeExtension(DataContext.DocumentUri().LocalPath, ".html"), DataContext.ToHtml(), Encoding.Unicode);
        }

        /// <summary>
        /// Writes context data to disc.
        /// </summary>
        /// <param name="DataContext">Given formal context.</param>
        public static async Task SaveAsTextAsync(this Context DataContext)
        {
            if (File.Exists(DataContext.DocumentUri().LocalPath))
                await File.WriteAllLinesAsync(Path.ChangeExtension(DataContext.DocumentUri().LocalPath, ".txt"), DataContext.ToText(), Encoding.Unicode);
        }

        /// <summary>
        /// Converts the XML data structure of formal context to HTML table.
        /// </summary>
        /// <returns>Lines of HTML document.</returns>
        public static string[] ToHtml(this Context DataContext) => ToStringArray(DataContext, true);

        /// <summary>
        /// Converts the XML data structure of formal context to text table.
        /// </summary>
        /// <returns>Lines of text document.</returns>
        public static string[] ToText(this Context DataContext) => ToStringArray(DataContext, false);

        /// <summary>
        /// Converts the XML data structure of formal context to an ASCII or HTML table.
        /// </summary>
        /// <param name="DataContext">Given formal context.</param>
        /// <param name="AsHtml">Should be true if HTML format is required otherwise false.</param>
        /// <returns>Lines of ASCII or HTML document.</returns>
        private static string[] ToStringArray(this Context DataContext, bool AsHtml = false)
        {
            if (!DataContext.IsValid())
                return Array.Empty<string>();

            char Separator = '|';

            var Table = new List<string>();

            string Heading = Head(DataContext.GetAttributes());

            Table.Add(Heading);

            XElement[] Objects = DataContext.GetObjects();

            for (int Index = 1; Index <= Objects.Length; Index++)
                Table.Add(Body(Index, DataContext.Row(Index)));

            return AsHtml ? ContextExtensions.ToHtml(Table.ToArray(), DataContext.DocumentPath()) : Table.ToArray();

            string Head(XElement[] Items)
            {
                var TextLine = new StringBuilder();

                string FstColumnWitdh = new string('-', DataContext.GetObjects().Length.ToString().Length);

                TextLine.Append(Separator + FstColumnWitdh + Separator);

                foreach (var Item in Items)
                {
                    if (Item.Attribute("Label") != null)
                        TextLine.Append($"{Separator}{Item.Attribute("Label").Value.DefaultIfEmpty('?').Last()}" + Separator);
                    else
                        TextLine.Append($"{Separator}{Item.Attribute("Index").Value.DefaultIfEmpty('?').Last()}" + Separator);
                }

                return TextLine.ToString();
            }

            string Body(int Index, XElement[] Items)
            {
                var TextLine = new StringBuilder();

                string FstColumnWitdh = Index.ToString().PadLeft(DataContext.GetObjects().Length.ToString().Length, '0');

                TextLine.Append(Separator + FstColumnWitdh + Separator);

                foreach (var Item in Items)
                {
                    string Line = string.Empty;

                    if (Item.Element("Value").Value == Context.W3boolTrueString) Line = Separator + "X";
                    else
                    {
                        if (!Item.HasAttributes)
                            Line = Separator + " ";
                        else
                        {
                            if (Item.Attribute("Arrow") == null || string.IsNullOrEmpty(Item.Attribute("Arrow").Value))
                                Line = Separator + " ";
                            else
                            {
                                if (Item.Attribute("Arrow").Value == "+")
                                    Line = Separator + "+";

                                if (Item.Attribute("Arrow").Value == "-")
                                    Line = Separator + "-";

                                if (Item.Attribute("Arrow").Value == ":")
                                    Line = Separator + ":";

                                if (Item.Attribute("Arrow").Value == "!") // Arrow relation is non applicable
                                    Line = Separator + " ";
                            }
                        }

                    }

                    TextLine.Append(Line + Separator);
                }

                return TextLine.ToString();
            }
        }

        private static string FormatObject(XElement Item)
        {
            string Abbreviation, Descriptor;

            Abbreviation = Item.Attribute("Index").Value;

            if (Item.Attribute("Label") != null)
            {
                Descriptor = Item.Attribute("Label").Value;
            }
            else
            {
                if (Item.Element("Memo") != null)
                {
                    if (Item.Element("Memo").Element("Text") != null)
                    {
                        Descriptor = Item.Element("Memo").Element("Text").Value;
                    }
                    else
                    {
                        if (Item.Element("Memo").Attribute("Text") != null)
                        {
                            Descriptor = Item.Element("Memo").Attribute("Text").Value;
                        }
                        else
                        {
                            Descriptor = Item.Element("Memo").Value;
                        }
                    }
                }
                else
                {
                    Descriptor = "-";
                }
            }

            return $"{Abbreviation}: {Descriptor}";
        }

        private static string FormatAttribute(XElement Item)
        {
            string Abbreviation, Descriptor;

            if (Item.Attribute("Label") != null) // optional
                Abbreviation = Item.Attribute("Label").Value;
            else
                Abbreviation = Item.Attribute("Index").Value;

            if (Item.Element("PrimaryKey") != null)
            {
                Descriptor = Item.Element("PrimaryKey").Value;
            }
            else
            {
                if (Item.Element("Memo") != null)
                {
                    if (Item.Element("Memo").Element("Text") != null)
                    {
                        Descriptor = Item.Element("Memo").Element("Text").Value;
                    }
                    else
                    {
                        if (Item.Element("Memo").Attribute("Text") != null)
                        {
                            Descriptor = Item.Element("Memo").Attribute("Text").Value;
                        }
                        else
                        {
                            Descriptor = Item.Element("Memo").Value;
                        }
                    }
                }
                else
                {
                    Descriptor = "-";
                }
            }
            return $"{Abbreviation}: {Descriptor}";
        }
    }
}
