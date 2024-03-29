﻿// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Formatting.DataSets;
using System.Net.Http.Formatting.Mocks;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using Microsoft.TestCommon;
using Moq;
using Xunit.Extensions;
using Assert = Microsoft.TestCommon.AssertEx;
using FactAttribute = Microsoft.TestCommon.WsrFactAttribute;

namespace System.Net.Http.Formatting
{
    public class MediaTypeFormatterTests
    {
        private const string TestMediaType = "text/test";
        private MediaTypeHeaderValue TestMediaTypeHeader = new MediaTypeHeaderValue(TestMediaType);

        public static TheoryDataSet<string, string[], string> SelectCharacterEncodingTestData
        {
            get
            {
                // string bodyEncoding, string[] supportedEncodings, string expectedEncoding
                return new TheoryDataSet<string, string[], string>
                {
                    { null, new string[] { "utf-8", "utf-16"}, "utf-8" },
                    { null, new string[] { "utf-16", "utf-8"}, "utf-16"},
                    { "utf-32", new string[] { "utf-8", "utf-16"}, "utf-8" },
                    { "utf-32", new string[] { "utf-8", "utf-16", "utf-32"}, "utf-32"}
                };
            }
        }

        [Fact]
        public void TypeIsCorrect()
        {
            Assert.Type.HasProperties(typeof(MediaTypeFormatter), TypeAssert.TypeProperties.IsPublicVisibleClass | TypeAssert.TypeProperties.IsAbstract);
        }

        [Fact]
        public void Constructor()
        {
            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            Collection<MediaTypeHeaderValue> supportedMediaTypes = formatter.SupportedMediaTypes;
            Assert.NotNull(supportedMediaTypes);
            Assert.Equal(0, supportedMediaTypes.Count);

            Collection<MediaTypeMapping> mappings = formatter.MediaTypeMappings;
            Assert.NotNull(mappings);
            Assert.Equal(0, mappings.Count);
        }

        [Fact]
        public void MaxCollectionKeySize_RoundTrips()
        {
            Assert.Reflection.IntegerProperty<MediaTypeFormatter, int>(
                null,
                c => MediaTypeFormatter.MaxHttpCollectionKeys,
                expectedDefaultValue: PlatformInfo.Platform == Platform.Net40 ? 1000 : Int32.MaxValue,
                minLegalValue: 1,
                illegalLowerValue: 0,
                maxLegalValue: null,
                illegalUpperValue: null,
                roundTripTestValue: 125);
        }

        [Fact]
        public void SupportedMediaTypes_IsMutable()
        {
            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            Collection<MediaTypeHeaderValue> supportedMediaTypes = formatter.SupportedMediaTypes;
            MediaTypeHeaderValue[] mediaTypes = HttpUnitTestDataSets.LegalMediaTypeHeaderValues.ToArray();
            foreach (MediaTypeHeaderValue mediaType in mediaTypes)
            {
                supportedMediaTypes.Add(mediaType);
            }

            Assert.True(mediaTypes.SequenceEqual(formatter.SupportedMediaTypes));
        }

        [Fact]
        public void SupportedMediaTypes_AddThrowsWithNullMediaType()
        {
            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            Collection<MediaTypeHeaderValue> supportedMediaTypes = formatter.SupportedMediaTypes;

            Assert.ThrowsArgumentNull(() => supportedMediaTypes.Add(null), "item");
        }

        [Theory]
        [TestDataSet(typeof(HttpUnitTestDataSets), "LegalMediaRangeValues")]
        public void SupportedMediaTypes_AddThrowsWithMediaRange(MediaTypeHeaderValue mediaType)
        {
            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            Collection<MediaTypeHeaderValue> supportedMediaTypes = formatter.SupportedMediaTypes;
            Assert.ThrowsArgument(() => supportedMediaTypes.Add(mediaType), "item", Error.Format(Properties.Resources.CannotUseMediaRangeForSupportedMediaType, typeof(MediaTypeHeaderValue).Name, mediaType.MediaType));
        }

        [Fact]
        public void SupportedMediaTypes_InsertThrowsWithNullMediaType()
        {
            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            Collection<MediaTypeHeaderValue> supportedMediaTypes = formatter.SupportedMediaTypes;

            Assert.ThrowsArgumentNull(() => supportedMediaTypes.Insert(0, null), "item");
        }

        [Theory]
        [TestDataSet(typeof(HttpUnitTestDataSets), "LegalMediaRangeValues")]
        public void SupportedMediaTypes_InsertThrowsWithMediaRange(MediaTypeHeaderValue mediaType)
        {
            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            Collection<MediaTypeHeaderValue> supportedMediaTypes = formatter.SupportedMediaTypes;

            Assert.ThrowsArgument(() => supportedMediaTypes.Insert(0, mediaType), "item", Error.Format(Properties.Resources.CannotUseMediaRangeForSupportedMediaType, typeof(MediaTypeHeaderValue).Name, mediaType.MediaType));
        }

        [Fact]
        public void MediaTypeMappings_IsMutable()
        {
            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            Collection<MediaTypeMapping> mappings = formatter.MediaTypeMappings;
            MediaTypeMapping[] standardMappings = HttpUnitTestDataSets.StandardMediaTypeMappings.ToArray();
            foreach (MediaTypeMapping mapping in standardMappings)
            {
                mappings.Add(mapping);
            }

            Assert.True(standardMappings.SequenceEqual(formatter.MediaTypeMappings));
        }

        [Fact]
        public void SelectCharacterEncoding_ThrowsIfNoSupportedEncodings()
        {
            // Arrange
            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter { CallBase = true };
            HttpContent content = new StringContent("Hello World", Encoding.UTF8, "text/plain");

            // Act
            Assert.Throws<InvalidOperationException>(() => formatter.SelectCharacterEncoding(content.Headers));
        }

        [Theory]
        [PropertyData("SelectCharacterEncodingTestData")]
        public void SelectCharacterEncoding_ReturnsBestEncoding(string bodyEncoding, string[] supportedEncodings, string expectedEncoding)
        {
            // Arrange
            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter { CallBase = true };

            foreach (string supportedEncoding in supportedEncodings)
            {
                formatter.SupportedEncodings.Add(Encoding.GetEncoding(supportedEncoding));
            }

            HttpContentHeaders contentHeaders = null;
            if (bodyEncoding != null)
            {
                Encoding bodyEnc = Encoding.GetEncoding(bodyEncoding);
                HttpContent content = new StringContent("Hello World", bodyEnc, "text/plain");
                contentHeaders = content.Headers;
            }

            // Act
            Encoding actualEncoding = formatter.SelectCharacterEncoding(contentHeaders);

            // Assert
            Encoding expectedEnc = expectedEncoding != null ? Encoding.GetEncoding(expectedEncoding) : null;
            Assert.Equal(expectedEnc, actualEncoding);
        }

        [Fact]
        public void ReadFromStreamAsync_ThrowsNotSupportedException()
        {
            var formatter = new Mock<MediaTypeFormatter> { CallBase = true }.Object;

            Assert.Throws<NotSupportedException>(() => formatter.ReadFromStreamAsync(null, null, null, null),
                "The media type formatter of type 'MediaTypeFormatterProxy' does not support reading because it does not implement the ReadFromStreamAsync method.");
        }

        [Fact]
        public void WriteToStreamAsync_ThrowsNotSupportedException()
        {
            var formatter = new Mock<MediaTypeFormatter> { CallBase = true }.Object;

            Assert.Throws<NotSupportedException>(() => formatter.WriteToStreamAsync(null, null, null, null, null),
                "The media type formatter of type 'MediaTypeFormatterProxy' does not support writing because it does not implement the WriteToStreamAsync method.");
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(string))]
        [InlineData(typeof(Nullable<int>))]
        public void GetDefaultValueForType_ReturnsNullForReferenceTypes(Type referenceType)
        {
            Assert.Null(MediaTypeFormatter.GetDefaultValueForType(referenceType));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(0)]
        [InlineData('a')]
        public void GetDefaultValueForType_ReturnsValueForValueTypes<T>(T value)
        {
            Type valueType = value.GetType();
            T defaultValue = default(T);
            Assert.Equal(defaultValue, MediaTypeFormatter.GetDefaultValueForType(valueType));
        }

        [Fact]
        public void GetDefaultValueForType_ReturnsValueForStruct()
        {
            TestStruct s = new TestStruct();

            TestStruct result = (TestStruct)MediaTypeFormatter.GetDefaultValueForType(typeof(TestStruct));

            Assert.Equal(s, result);
        }

        [Fact]
        public void SetDefaultContentHeaders_ThrowsOnNullType()
        {
            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            HttpContentHeaders contentHeaders = FormattingUtilities.CreateEmptyContentHeaders();
            Assert.ThrowsArgumentNull(() => formatter.SetDefaultContentHeaders(null, contentHeaders, TestMediaTypeHeader), "type");
        }

        [Fact]
        public void SetDefaultContentHeaders_ThrowsOnNullHeaders()
        {
            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            Type type = typeof(object);
            Assert.ThrowsArgumentNull(() => formatter.SetDefaultContentHeaders(type, null, TestMediaTypeHeader), "headers");
        }

        [Fact]
        public void SetDefaultContentHeaders_UsesNonNullMediaTypeClone()
        {
            // Arrange
            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            Type type = typeof(object);
            HttpContentHeaders contentHeaders = FormattingUtilities.CreateEmptyContentHeaders();

            // Act
            formatter.SetDefaultContentHeaders(type, contentHeaders, TestMediaTypeHeader);

            // Assert
            Assert.NotSame(TestMediaTypeHeader, contentHeaders.ContentType);
            Assert.Equal(TestMediaType, contentHeaders.ContentType.MediaType);
        }

        [Fact]
        public void SetDefaultContentHeaders_UsesDefaultSupportedMediaTypeClone()
        {
            // Arrange
            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            formatter.SupportedMediaTypes.Add(TestMediaTypeHeader);
            Type type = typeof(object);
            HttpContentHeaders contentHeaders = FormattingUtilities.CreateEmptyContentHeaders();

            // Act
            formatter.SetDefaultContentHeaders(type, contentHeaders, null);

            // Assert
            Assert.NotSame(TestMediaTypeHeader, contentHeaders.ContentType);
            Assert.Equal(TestMediaType, contentHeaders.ContentType.MediaType);
        }

        [Fact]
        public void SetDefaultContentHeaders_UsesDefaultSupportedEncoding()
        {
            // Arrange
            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            Encoding encoding = new UnicodeEncoding();
            formatter.SupportedEncodings.Add(encoding);
            Type type = typeof(object);
            HttpContentHeaders contentHeaders = FormattingUtilities.CreateEmptyContentHeaders();

            // Act
            formatter.SetDefaultContentHeaders(type, contentHeaders, TestMediaTypeHeader);

            // Assert
            Assert.Equal(TestMediaType, contentHeaders.ContentType.MediaType);
            Assert.Equal(encoding.WebName, contentHeaders.ContentType.CharSet);
        }

        [Fact]
        public void SetDefaultContentHeaders_UsesDefaultSupportedMediaTypeAndEncoding()
        {
            // Arrange
            MockMediaTypeFormatter formatter = new MockMediaTypeFormatter();
            formatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue(TestMediaType));
            Encoding encoding = new UnicodeEncoding();
            formatter.SupportedEncodings.Add(encoding);
            Type type = typeof(object);
            HttpContentHeaders contentHeaders = FormattingUtilities.CreateEmptyContentHeaders();

            // Act
            formatter.SetDefaultContentHeaders(type, contentHeaders, null);

            // Assert
            Assert.Equal(TestMediaType, contentHeaders.ContentType.MediaType);
            Assert.Equal(encoding.WebName, contentHeaders.ContentType.CharSet);
        }

        public struct TestStruct
        {
            private int I;
            public TestStruct(int i)
            {
                I = i + 1;
            }
        }
    }
}
