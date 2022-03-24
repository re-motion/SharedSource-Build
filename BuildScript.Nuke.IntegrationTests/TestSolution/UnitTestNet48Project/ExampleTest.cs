using NUnit.Framework;

namespace UnittestTestProject
{
    public class ExampleTest
    {

        [Test, Category("Category1")]
        public void ExampleTest1()
        {
            Assert.Pass();
        }

        [Test, Category("Category2")]
        public void ExampleTest2()
        {
          Assert.Pass();
        }

        [Test, Category("Category2")]
        public void ExampleTest3()
        {
            Assert.Fail();
        }

        [Test]
        public void ExampleTest4()
        {
            Assert.Pass();
        }
    }
}