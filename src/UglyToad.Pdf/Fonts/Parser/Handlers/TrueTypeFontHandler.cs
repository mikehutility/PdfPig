﻿namespace UglyToad.Pdf.Fonts.Parser.Handlers
{
    using System;
    using System.Linq;
    using Cmap;
    using ContentStream;
    using Cos;
    using Encodings;
    using Exceptions;
    using Filters;
    using IO;
    using Parts;
    using Pdf.Parser;
    using Simple;
    using TrueType;
    using TrueType.Parser;

    internal class TrueTypeFontHandler : IFontHandler
    {
        private readonly IPdfObjectParser pdfObjectParser;
        private readonly IFilterProvider filterProvider;
        private readonly CMapCache cMapCache;
        private readonly FontDescriptorFactory fontDescriptorFactory;
        private readonly TrueTypeFontParser trueTypeFontParser;

        public TrueTypeFontHandler(IPdfObjectParser pdfObjectParser, IFilterProvider filterProvider, 
            CMapCache cMapCache,
            FontDescriptorFactory fontDescriptorFactory,
            TrueTypeFontParser trueTypeFontParser)
        {
            this.pdfObjectParser = pdfObjectParser;
            this.filterProvider = filterProvider;
            this.cMapCache = cMapCache;
            this.fontDescriptorFactory = fontDescriptorFactory;
            this.trueTypeFontParser = trueTypeFontParser;
        }

        public IFont Generate(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            var firstCharacter = FontDictionaryAccessHelper.GetFirstCharacter(dictionary);

            var lastCharacter = FontDictionaryAccessHelper.GetLastCharacter(dictionary);

            var widths = FontDictionaryAccessHelper.GetWidths(pdfObjectParser, dictionary, reader, isLenientParsing);

            var descriptor = FontDictionaryAccessHelper.GetFontDescriptor(pdfObjectParser, fontDescriptorFactory, dictionary, reader, isLenientParsing);

            var font = ParseTrueTypeFont(descriptor, reader, isLenientParsing);

            var name = FontDictionaryAccessHelper.GetName(dictionary, descriptor);

            CMap toUnicodeCMap = null;
            if (dictionary.TryGetItemOfType(CosName.TO_UNICODE, out CosObject toUnicodeObj))
            {
                var toUnicode = pdfObjectParser.Parse(toUnicodeObj.ToIndirectReference(), reader, isLenientParsing) as PdfRawStream;

                var decodedUnicodeCMap = toUnicode?.Decode(filterProvider);

                if (decodedUnicodeCMap != null)
                {
                    toUnicodeCMap = cMapCache.Parse(new ByteArrayInputBytes(decodedUnicodeCMap), isLenientParsing);
                }
            }

            Encoding encoding = null;
            if (dictionary.TryGetValue(CosName.ENCODING, out var encodingBase))
            {
                // Symbolic fonts default to standard encoding.
                if (descriptor.Flags.HasFlag(FontFlags.Symbolic))
                {
                    encoding = StandardEncoding.Instance;
                }

                if (encodingBase is CosName encodingName)
                {
                    if (!Encoding.TryGetNamedEncoding(encodingName, out encoding))
                    {
                        // TODO: PDFBox would not throw here.
                        throw new InvalidFontFormatException($"Unrecognised encoding name: {encodingName}");
                    }
                }
                else if (encodingBase is CosDictionary encodingDictionary)
                {
                    throw new NotImplementedException("No support for reading encoding from dictionary yet.");
                }
                else
                {
                    throw new NotImplementedException("No support for reading encoding from font yet.");
                }
            }

            return new TrueTypeSimpleFont(name, firstCharacter, lastCharacter, widths, descriptor, toUnicodeCMap, encoding);
        }

        private TrueTypeFont ParseTrueTypeFont(FontDescriptor descriptor, IRandomAccessRead reader,
            bool isLenientParsing)
        {
            if (descriptor?.FontFile == null)
            {
                return null;
            }

            if (descriptor.FontFile.FileType != DescriptorFontFile.FontFileType.TrueType)
            {
                throw new InvalidFontFormatException(
                    $"Expected a TrueType font in the TrueType font descriptor, instead it was {descriptor.FontFile.FileType}.");
            }

            var fontFileStream = pdfObjectParser.Parse(descriptor.FontFile.ObjectKey, reader, isLenientParsing) as PdfRawStream;

            if (fontFileStream == null)
            {
                return null;
            }

            var fontFile = fontFileStream.Decode(filterProvider);

            var font = trueTypeFontParser.Parse(new TrueTypeDataBytes(new ByteArrayInputBytes(fontFile)));

            return font;
        }
    }
}
