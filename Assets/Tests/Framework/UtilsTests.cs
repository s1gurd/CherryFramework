using System;
using System.Collections.Generic;
using CherryFramework.Utils;
using NUnit.Framework;
using UnityEngine;

namespace CherryFramework.Tests
{
    public class UtilsTests
    {
        private enum Sample
        {
            First,
            Second
        }

        [Test]
        public void DataUtils_CreateKey_JoinsTheValuesWithADash()
        {
            Assert.AreEqual("a-b-c", DataUtils.CreateKey("a", "b", "c"));
        }

        [Test]
        public void DataUtils_CreateKey_SkipsTheEmptyValues()
        {
            Assert.AreEqual("a-b", DataUtils.CreateKey("a", string.Empty, "b"));
        }

        [Test]
        public void StringUtils_IsNullOrWhiteSpace_MatchesTheStringClass()
        {
            Assert.IsTrue(((string)null).IsNullOrWhiteSpace());
            Assert.IsTrue("  ".IsNullOrWhiteSpace());
            Assert.IsFalse("text".IsNullOrWhiteSpace());
        }

        [Test]
        public void MathUtils_InRange_IncludesTheBounds()
        {
            Assert.IsTrue(5f.InRange(0f, 10f));
            Assert.IsTrue(0f.InRange(0f, 10f));
            Assert.IsFalse(11f.InRange(0f, 10f));
        }

        [Test]
        public void MathUtils_InRange_Exclusive_ExcludesTheBounds()
        {
            Assert.IsTrue(5f.InRange(0f, 10f, true));
            Assert.IsFalse(0f.InRange(0f, 10f, true));
        }

        [Test]
        public void MathUtils_InRange_WorksWithSwappedBounds()
        {
            Assert.IsTrue(5f.InRange(10f, 0f));
        }

        [Test]
        public void MathUtils_RotateByDegrees_TurnsTheVector()
        {
            var rotated = new Vector2(1f, 0f).RotateByDegrees(90f);

            Assert.AreEqual(0f, rotated.x, 0.0001f);
            Assert.AreEqual(1f, rotated.y, 0.0001f);
        }

        [Test]
        public void MathUtils_ArrayToVector2AndBack_KeepsTheValues()
        {
            var vector = MathUtils.ArrayToVector2(new[] { 1f, 2f });

            Assert.AreEqual(new Vector2(1f, 2f), vector);
            CollectionAssert.AreEqual(new[] { 1f, 2f }, MathUtils.Vector2ToArray(vector));
        }

        [Test]
        public void EnumerableUtils_GetAllEnumValues_ReturnsEveryValue()
        {
            CollectionAssert.AreEqual(new[] { Sample.First, Sample.Second }, Sample.First.GetAllEnumValues());
        }

        [Test]
        public void EnumerableUtils_SelectWhere_KeepsOnlyTheValidResults()
        {
            var result = new List<int>(new[] { 1, 2, 3, 4 }.SelectWhere(value => (value % 2 == 0, value * 10)));

            CollectionAssert.AreEqual(new[] { 20, 40 }, result);
        }

        [Test]
        public void EnumerableUtils_IndexOf_ReturnsThePositionOrMinusOne()
        {
            var source = new[] { "a", "b", "c" };

            Assert.AreEqual(1, source.IndexOf("b"));
            Assert.AreEqual(-1, source.IndexOf("z"));
        }

        [Test]
        public void EnumerableUtils_TryGet_FindsTheFirstMatch()
        {
            var source = new[] { 1, 2, 3 };

            Assert.IsTrue(source.TryGet(value => value > 1, out int found));
            Assert.AreEqual(2, found);
            Assert.IsFalse(source.TryGet(value => value > 5, out _));
        }

        [Test]
        public void EnumerableUtils_Each_PassesTheIndex()
        {
            var indexes = new List<int>();
            new[] { "a", "b", "c" }.Each((value, index) => indexes.Add(index));

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, indexes);
        }
    }
}
