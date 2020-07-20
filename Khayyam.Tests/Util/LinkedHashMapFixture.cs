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

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Khayyam.Util;
using NUnit.Framework;

namespace Khayyam.Tests.Util
{
    [TestFixture]
    public class LinkedHashMapFixture
    {
        private static readonly Player[] Players =
        {
            new Player("12341", "Boeta Dippenaar"),
            new Player("23432", "Gary Kirsten"),
            new Player("23411", "Graeme Smith"),
            new Player("55221", "Jonty Rhodes"),
            new Player("61234", "Monde Zondeki"),
            new Player("23415", "Paul Adams")
        };

        private static void Fill(IDictionary<string, Player> lhm)
        {
            foreach (Player player in Players)
                lhm.Add(player.Id, player);
        }

        [Test]
        public void Add()
        {
            IDictionary<string, Player> lhm = new LinkedHashMap<string, Player>();
            Fill(lhm);
            lhm.Add("55555", new Player("55555", "Monde Zondeki"));

            Assert.AreEqual(7, lhm.Count);
        }

        [Test]
        public void LastKeyLastValue()
        {
            LinkedHashMap<string, Player> lhm = new LinkedHashMap<string, Player>();
            Fill(lhm);
            Assert.AreEqual(Players[^1].Id, lhm.LastKey);
            Assert.AreEqual(Players[^1], lhm.LastValue);

            // override
            Player antWithSameId = new Player("12341", "Another");
            lhm[antWithSameId.Id] = antWithSameId;
            Assert.AreEqual(antWithSameId.Id, lhm.LastKey);
            Assert.AreEqual(antWithSameId, lhm.LastValue);
        }

        [Test]
        public void FirstKeyFirstValue()
        {
            LinkedHashMap<string, Player> lhm = new LinkedHashMap<string, Player>();
            Fill(lhm);
            Assert.AreEqual(Players[0].Id, lhm.FirstKey);
            Assert.AreEqual(Players[0], lhm.FirstValue);

            // override First
            Player antWithSameId = new Player("12341", "Another");
            lhm[antWithSameId.Id] = antWithSameId;
            Assert.AreEqual(Players[1].Id, lhm.FirstKey);
            Assert.AreEqual(Players[1], lhm.FirstValue);
        }

        [Test]
        public void Clear()
        {
            IDictionary<string, Player> lhm = new LinkedHashMap<string, Player>();
            Player p = new Player("78945", "Someone");
            lhm[p.Id] = p;

            lhm.Clear();
            Assert.AreEqual(0, lhm.Count);

            foreach (var pair in lhm)
                Assert.Fail("Should not be any entries but found Key = " + pair.Key + " and Value = " + pair.Value);
        }

        [Test]
        public void Contains()
        {
            var lhm = new LinkedHashMap<string, Player>();
            Fill(lhm);

            Assert.IsTrue(lhm.Contains("12341"));
            Assert.IsFalse(lhm.Contains("55555"));
        }

        [Test]
        public void GetEnumerator()
        {
            IDictionary<string, Player> lhm = new LinkedHashMap<string, Player>();
            Fill(lhm);
            var index = 0;
            foreach (var pair in lhm)
            {
                Assert.AreEqual(Players[index].Id, pair.Key);
                Assert.AreEqual(Players[index], pair.Value);
                index++;
            }

            Assert.AreEqual(6, index);
        }

        [Test]
        public void GetEnumeratorEmpty()
        {
            // ReSharper disable once CollectionNeverUpdated.Local
            IDictionary<string, Player> lhm = new LinkedHashMap<string, Player>();
            Assert.AreEqual(0, lhm.Count);

            var entries =
                lhm.Count() +
                lhm.Keys.Count() +
                lhm.Values.Count();

            Assert.AreEqual(0, entries, "should not have any entries in the enumerators");
        }

        [Test]
        public void GetEnumeratorModifyExceptionFromAdd()
        {
            IDictionary<string, Player> lhm = new LinkedHashMap<string, Player>();
            lhm["123"] = new Player("123", "yyyyyyy");
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var pair in lhm)
                {
                    lhm["78945"] = new Player("78945", "Someone");
                }
            });
        }

        [Test]
        public void GetEnumeratorModifyExceptionFromRemove()
        {
            IDictionary<string, Player> lhm = new LinkedHashMap<string, Player>();
            lhm["123"] = new Player("123", "yyyyyyy");
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var pair in lhm)
                {
                    lhm.Remove(pair.Key);
                }
            });
        }

        [Test]
        public void GetEnumeratorModifyExceptionFromUpdate()
        {
            IDictionary<string, Player> lhm = new LinkedHashMap<string, Player>();
            lhm["123"] = new Player("123", "yyyyyyy");
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var pair in lhm)
                {
                    lhm["123"] = new Player("123", "aaaaaaa");
                }
            });
        }

        [Test]
        public void Remove()
        {
            IDictionary<string, Player> lhm = new LinkedHashMap<string, Player>();
            Fill(lhm);

            // remove an item that exists
            var removed = lhm.Remove("23411");
            Assert.IsTrue(removed);
            Assert.AreEqual(5, lhm.Count);

            // try to remove an item that does not exist
            removed = lhm.Remove("65432");
            Assert.IsFalse(removed);
            Assert.AreEqual(5, lhm.Count);
        }

        [Test]
        public void ContainsValue()
        {
            var lhm = new LinkedHashMap<string, Player>();
            Fill(lhm);
            Assert.IsTrue(lhm.ContainsValue(new Player("55221", "Jonty Rhodes")));
            Assert.IsFalse(lhm.ContainsValue(new Player("55221", "SameKeyDiffName")));
        }

        [Test]
        public void CopyTo()
        {
            IDictionary<string, Player> lhm = new LinkedHashMap<string, Player>();
            Fill(lhm);
            var destArray = new KeyValuePair<string, Player>[lhm.Count + 1];
            destArray[0] = new KeyValuePair<string, Player>("999", new Player("999", "The number nine"));
            lhm.CopyTo(destArray, 1);

            for (var i = 1; i < destArray.Length; i++)
            {
                Assert.AreEqual(Players[i - 1].Id, destArray[i].Key);
                Assert.AreEqual(Players[i - 1], destArray[i].Value);
            }
        }

        [Test]
        public void Keys()
        {
            IDictionary<string, Player> lhm = new LinkedHashMap<string, Player>();
            Fill(lhm);
            var index = 0;
            foreach (var s in lhm.Keys)
            {
                Assert.AreEqual(Players[index].Id, s);
                index++;
            }
        }

        [Test]
        public void Values()
        {
            IDictionary<string, Player> lhm = new LinkedHashMap<string, Player>();
            Fill(lhm);
            var index = 0;
            foreach (var p in lhm.Values)
            {
                Assert.AreEqual(Players[index], p);
                index++;
            }
        }

        [Test]
        public void Serialization()
        {
            IDictionary<string, Player> lhm = new LinkedHashMap<string, Player>();
            Fill(lhm);

            var stream = new MemoryStream();
            var f = new BinaryFormatter();
            f.Serialize(stream, lhm);
            stream.Position = 0;

            var dlhm = (LinkedHashMap<string, Player>) f.Deserialize(stream);
            stream.Close();

            Assert.AreEqual(6, dlhm.Count);
            var index = 0;
            foreach (KeyValuePair<string, Player> pair in dlhm)
            {
                Assert.AreEqual(Players[index].Id, pair.Key);
                Assert.AreEqual(Players[index], pair.Value);
                index++;
            }

            Assert.AreEqual(6, index);
        }

        [Test, Explicit]
        public void ShowDiff()
        {
            IDictionary<string, Player> dict = new Dictionary<string, Player>();
            IDictionary<string, Player> lhm = new LinkedHashMap<string, Player>();
            Fill(dict);
            Fill(lhm);
            // Override the first element
            var o = new Player("12341", "Ovirride");
            dict[o.Id] = o;
            lhm[o.Id] = o;
            Console.Out.WriteLine("Dictionary order:");
            foreach (var pair in dict)
            {
                Console.Out.WriteLine("Key->{0}", pair.Key);
            }

            Console.Out.WriteLine("LinkedHashMap order:");
            foreach (var pair in lhm)
            {
                Console.Out.WriteLine("Key->{0}", pair.Key);
            }
        }

        [Test, Explicit]
        [SuppressMessage("ReSharper", "NotAccessedVariable")]
        [SuppressMessage("ReSharper", "RedundantAssignment")]
        public void Performance()
        {
            // Take care with this test because the result is not the same every times

            const int numOfRuns = 4;

            const int numOfEntries = short.MaxValue;

            var dictPopulateTicks = new long[numOfRuns];
            var dictItemTicks = new long[numOfRuns];

            var linkPopulateTicks = new long[numOfRuns];
            var linkItemTicks = new long[numOfRuns];

            for (var runIndex = 0; runIndex < numOfRuns; runIndex++)
            {
                string key;
                object value;
                IDictionary<string, object> dictionary = new Dictionary<string, object>();
                IDictionary<string, object> linked = new LinkedHashMap<string, object>();

                var dictStart = DateTime.Now.Ticks;

                for (var i = 0; i < numOfEntries; i++)
                {
                    dictionary.Add("test" + i, new object());
                }

                dictPopulateTicks[runIndex] = DateTime.Now.Ticks - dictStart;

                dictStart = DateTime.Now.Ticks;
                for (var i = 0; i < numOfEntries; i++)
                {
                    key = "test" + i;
                    value = dictionary[key];
                }

                dictItemTicks[runIndex] = DateTime.Now.Ticks - dictStart;

                dictionary.Clear();

                var linkStart = DateTime.Now.Ticks;

                for (var i = 0; i < numOfEntries; i++)
                {
                    linked.Add("test" + i, new object());
                }

                linkPopulateTicks[runIndex] = DateTime.Now.Ticks - linkStart;

                linkStart = DateTime.Now.Ticks;
                for (var i = 0; i < numOfEntries; i++)
                {
                    key = "test" + i;
                    value = linked[key];
                }

                linkItemTicks[runIndex] = DateTime.Now.Ticks - linkStart;

                linked.Clear();
            }

            for (var runIndex = 0; runIndex < numOfRuns; runIndex++)
            {
                var linkPopulateOverhead = (linkPopulateTicks[runIndex] / (decimal) dictPopulateTicks[runIndex]);
                var linkItemOverhead = (linkItemTicks[runIndex] / (decimal) dictItemTicks[runIndex]);

                var message = $"LinkedHashMap vs Dictionary (Run-{runIndex + 1}) :";
                message += "\n POPULATE:";
                message += "\n\t linked took " + linkPopulateTicks[runIndex] + " ticks.";
                message += "\n\t dictionary took " + dictPopulateTicks[runIndex] + " ticks.";
                message += "\n\t for an overhead of " + linkPopulateOverhead;
                message += "\n RETRIVE:";
                message += "\n\t linked took " + linkItemTicks[runIndex] + " ticks.";
                message += "\n\t dictionary took " + dictItemTicks[runIndex] + " ticks.";
                message += "\n\t for an overhead of " + linkItemOverhead;

                Console.Out.WriteLine(message);
                Console.Out.WriteLine();
            }
        }
    }

    [Serializable]
    public class Player
    {
        private string _id;
        private string _name;

        public Player(string id, string name)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");

            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            _id = id;
            _name = name;
        }

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            return _id.GetHashCode() ^ _name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Player that)) return false;
            return _id.Equals(that._id) && _name.Equals(that._name);
        }

        public override string ToString()
        {
            return $"<{_id}>{_name}";
        }
    }
}