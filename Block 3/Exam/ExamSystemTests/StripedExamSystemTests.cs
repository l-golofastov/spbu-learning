using System.Reflection;
using ExamSystem.Implementation;

namespace ExamSystemTests
{
    public class StripedExamSystemTests
    {
        [Test]
        public void AddTest()
        {
            var examSystem = new StripedExamSystem();

            for (int i = 0; i < 15; i++)
            {
                examSystem.Add(i, i + 1);
            }

            Assert.AreEqual(15, examSystem.Count);
            Assert.IsTrue(examSystem.Contains(1, 2));
            Assert.IsFalse(examSystem.Contains(2, 1));
        }

        [Test]
        public void RemoveTest()
        {
            var examSystem = new StripedExamSystem();

            for (int i = 0; i < 15; i++)
            {
                examSystem.Add(i, i + 1);
            }

            Assert.AreEqual(15, examSystem.Count);
            Assert.IsTrue(examSystem.Contains(3, 4));

            for (int i = 3; i < 7; i++)
            {
                examSystem.Remove(i, i + 1);
            }

            Assert.AreEqual(11, examSystem.Count);
            Assert.IsFalse(examSystem.Contains(3, 4));
        }

        [Test]
        public void CollisionTest()
        {
            var examSystem = new StripedExamSystem();

            examSystem.Add(5, 6);
            examSystem.Add(4, 1);
            examSystem.Add(6, 7);
            examSystem.Remove(5, 6);

            Assert.IsTrue(examSystem.Contains(4, 1));
            Assert.IsTrue(examSystem.Contains(6, 7));
            Assert.IsFalse(examSystem.Contains(5, 6));
        }
        
        [Test]
        public void ResizeTest()
        {
            var examSystem = new StripedExamSystem();

            for (int i = 0; i < 555; i++)
            {
                examSystem.Add(i, i + 1);
            }

            Assert.AreEqual(555, examSystem.Count);
        }
    }
}