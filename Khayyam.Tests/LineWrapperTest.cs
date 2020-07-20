/*
 * Copyright (C) 2020 Arian Dashti.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Diagnostics.CodeAnalysis;
using Khayyam.Util;
using NUnit.Framework;

namespace Khayyam.Tests
{
    [TestFixture]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class LineWrapperTest
    {
        [Test]
        public void Wrap()
        {
            var @out = new AppendableStringBuilder();
            var lineWrapper = new LineWrapper(@out, "  ", 10);
            lineWrapper.Append("abcde");
            lineWrapper.WrappingSpace(2);
            lineWrapper.Append("fghij");
            lineWrapper.Close();
            Assert.AreEqual("abcde\n    fghij", @out.ToString());
        }
        
        [Test]
        public void NoWrap()
        {
            var @out = new AppendableStringBuilder();
            var lineWrapper = new LineWrapper(@out, "  ", 10);
            lineWrapper.Append("abcde");
            lineWrapper.WrappingSpace(2);
            lineWrapper.Append("fghi");
            lineWrapper.Close();
            Assert.AreEqual("abcde fghi", @out.ToString());
        }
        
        [Test]
        public void ZeroWidthNoWrap()
        {
            var @out = new AppendableStringBuilder();
            var lineWrapper = new LineWrapper(@out, "  ", 10);
            lineWrapper.Append("abcde");
            lineWrapper.ZeroWidthSpace(2);
            lineWrapper.Append("fghij");
            lineWrapper.Close();
            Assert.AreEqual("abcdefghij", @out.ToString());
        }
        
        [Test]
        public void NospaceWrapMax()
        {
            var @out = new AppendableStringBuilder();
            var lineWrapper = new LineWrapper(@out, "  ", 10);
            lineWrapper.Append("abcde");
            lineWrapper.ZeroWidthSpace(2);
            lineWrapper.Append("fghijk");
            lineWrapper.Close();
            Assert.AreEqual("abcde\n    fghijk", @out.ToString());
        }
        
        [Test]
        public void MultipleWrite()
        {
            var @out = new AppendableStringBuilder();
            var lineWrapper = new LineWrapper(@out, "  ", 10);
            lineWrapper.Append("ab");
            lineWrapper.WrappingSpace(1);
            lineWrapper.Append("cd");
            lineWrapper.WrappingSpace(1);
            lineWrapper.Append("ef");
            lineWrapper.WrappingSpace(1);
            lineWrapper.Append("gh");
            lineWrapper.WrappingSpace(1);
            lineWrapper.Append("ij");
            lineWrapper.WrappingSpace(1);
            lineWrapper.Append("kl");
            lineWrapper.WrappingSpace(1);
            lineWrapper.Append("mn");
            lineWrapper.WrappingSpace(1);
            lineWrapper.Append("op");
            lineWrapper.WrappingSpace(1);
            lineWrapper.Append("qr");
            lineWrapper.Close();
            Assert.AreEqual("ab cd ef\n  gh ij kl\n  mn op qr", @out.ToString());
        }
        
        [Test]
        public void Fencepost()
        {
            var @out = new AppendableStringBuilder();
            var lineWrapper = new LineWrapper(@out, "  ", 10);
            lineWrapper.Append("abcde");
            lineWrapper.Append("fghij");
            lineWrapper.WrappingSpace(2);
            lineWrapper.Append("k");
            lineWrapper.Append("lmnop");
            lineWrapper.Close();
            Assert.AreEqual("abcdefghij\n    klmnop", @out.ToString());
        }
        
        [Test]
        public void FencepostZeroWidth()
        {
            var @out = new AppendableStringBuilder();
            var lineWrapper = new LineWrapper(@out, "  ", 10);
            lineWrapper.Append("abcde");
            lineWrapper.Append("fghij");
            lineWrapper.ZeroWidthSpace(2);
            lineWrapper.Append("k");
            lineWrapper.Append("lmnop");
            lineWrapper.Close();
            Assert.AreEqual("abcdefghij\n    klmnop", @out.ToString());
        }


        [Test]
        public void OverlyLongLinesWithoutLeadingSpace()
        {
            var @out = new AppendableStringBuilder();
            var lineWrapper = new LineWrapper(@out, "  ", 10);
            lineWrapper.Append("abcdefghijkl");
            lineWrapper.Close();
            Assert.AreEqual("abcdefghijkl", @out.ToString());
        }
        
        
        [Test]
        public void OverlyLongLinesWithLeadingSpace()
        {
            var @out = new AppendableStringBuilder();
            var lineWrapper = new LineWrapper(@out, "  ", 10);
            lineWrapper.WrappingSpace(2);
            lineWrapper.Append("abcdefghijkl");
            lineWrapper.Close();
            Assert.AreEqual("\n    abcdefghijkl", @out.ToString());
        }


        [Test]
        public void OverlyLongLinesWithLeadingZeroWidth()
        {
            var @out = new AppendableStringBuilder();
            var lineWrapper = new LineWrapper(@out, "  ", 10);
            lineWrapper.ZeroWidthSpace(2);
            lineWrapper.Append("abcdefghijkl");
            lineWrapper.Close();
            Assert.AreEqual("abcdefghijkl", @out.ToString());
        }
        
        [Test]
        public void NoWrapEmbeddedNewlines()
        {
            var @out = new AppendableStringBuilder();
            var lineWrapper = new LineWrapper(@out, "  ", 10);
            lineWrapper.Append("abcde");
            lineWrapper.WrappingSpace(2);
            lineWrapper.Append("fghi\njklmn");
            lineWrapper.Append("opqrstuvwxy");
            lineWrapper.Close();
            Assert.AreEqual("abcde fghi\njklmnopqrstuvwxy", @out.ToString());
        }
        
        [Test]
        public void WrapEmbeddedNewlines()
        {
            var @out = new AppendableStringBuilder();
            var lineWrapper = new LineWrapper(@out, "  ", 10);
            lineWrapper.Append("abcde");
            lineWrapper.WrappingSpace(2);
            lineWrapper.Append("fghij\nklmn");
            lineWrapper.Append("opqrstuvwxy");
            lineWrapper.Close();
            Assert.AreEqual("abcde\n    fghij\nklmnopqrstuvwxy", @out.ToString());
        }
        
        [Test]
        public void noWrapEmbeddedNewlines_ZeroWidth()
        {
            var @out = new AppendableStringBuilder();
            var lineWrapper = new LineWrapper(@out, "  ", 10);
            lineWrapper.Append("abcde");
            lineWrapper.ZeroWidthSpace(2);
            lineWrapper.Append("fghij\nklmn");
            lineWrapper.Append("opqrstuvwxyz");
            lineWrapper.Close();
            Assert.AreEqual("abcdefghij\nklmnopqrstuvwxyz", @out.ToString());
        }
        
        [Test]
        public void wrapEmbeddedNewlines_ZeroWidth()
        {
            var @out = new AppendableStringBuilder();
            var lineWrapper = new LineWrapper(@out, "  ", 10);
            lineWrapper.Append("abcde");
            lineWrapper.ZeroWidthSpace(2);
            lineWrapper.Append("fghijk\nlmn");
            lineWrapper.Append("opqrstuvwxy");
            lineWrapper.Close();
            Assert.AreEqual("abcde\n    fghijk\nlmnopqrstuvwxy", @out.ToString());
        }
        
        [Test]
        public void NoWrapMultipleNewlines()
        {
            var @out = new AppendableStringBuilder();
            var lineWrapper = new LineWrapper(@out, "  ", 10);
            lineWrapper.Append("abcde");
            lineWrapper.WrappingSpace(2);
            lineWrapper.Append("fghi\nklmnopq\nr");
            lineWrapper.WrappingSpace(2);
            lineWrapper.Append("stuvwxyz");
            lineWrapper.Close();
            Assert.AreEqual("abcde fghi\nklmnopq\nr stuvwxyz", @out.ToString());
        }
        
        [Test]
        public void WrapMultipleNewlines()
        {
            var @out = new AppendableStringBuilder();
            var lineWrapper = new LineWrapper(@out, "  ", 10);
            lineWrapper.Append("abcde");
            lineWrapper.WrappingSpace(2);
            lineWrapper.Append("fghi\nklmnopq\nrs");
            lineWrapper.WrappingSpace(2);
            lineWrapper.Append("tuvwxyz1");
            lineWrapper.Close();
            Assert.AreEqual("abcde fghi\nklmnopq\nrs\n    tuvwxyz1", @out.ToString());
        }
    }
}