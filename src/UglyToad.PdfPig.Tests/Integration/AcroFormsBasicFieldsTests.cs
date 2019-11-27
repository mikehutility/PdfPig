﻿namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Linq;
    using AcroForms.Fields;
    using Xunit;

    public class AcroFormsBasicFieldsTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("AcroFormsBasicFields");
        }

        [Fact]
        public void GetFormNotNull()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var form = document.GetForm();
                Assert.NotNull(form);
            }
        }

        [Fact]
        public void GetFormDisposedThrows()
        {
            var document = PdfDocument.Open(GetFilename());

            document.Dispose();

            Action action = () => document.GetForm();

            Assert.Throws<ObjectDisposedException>(action);
        }

        [Fact]
        public void GetsAllFormFields()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var form = document.GetForm();
                Assert.Equal(18, form.Fields.Count);
            }
        }

        [Fact]
        public void GetFormFieldsByPage()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var form = document.GetForm();
                var fields = form.GetFieldsForPage(1).ToList();
                Assert.Equal(18, fields.Count);
            }
        }

        [Fact]
        public void GetsRadioButtonState()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var form = document.GetForm();
                var radioButtons = form.Fields.OfType<AcroRadioButtonsField>().ToList();

                Assert.Equal(2, radioButtons.Count);

                // ReSharper disable once PossibleInvalidOperationException
                var ordered = radioButtons.OrderBy(x => x.Children.Min(y => y.Bounds.Value.Left)).ToList();

                var left = ordered[0];

                Assert.Equal(2, left.Children.Count);
                foreach (var acroFieldBase in left.Children)
                {
                    var button = Assert.IsType<AcroRadioButtonField>(acroFieldBase);
                    Assert.False(button.IsSelected);
                }

                var right = ordered[1];
                Assert.Equal(2, right.Children.Count);

                var buttonOn = Assert.IsType<AcroRadioButtonField>(right.Children[0]);
                Assert.True(buttonOn.IsSelected);

                var buttonOff = Assert.IsType<AcroRadioButtonField>(right.Children[1]);
                Assert.False(buttonOff.IsSelected);
            }
        }
    }
}
