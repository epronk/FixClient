using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static Fix.Dictionary;

namespace FixTests
{
    [TestClass]
    public class IndicationBookTests
    {
        [TestMethod]
        public void TestNew()
        {
            var ioi = new Fix.Message("8=FIX.4.49=11235=649=ACCEPTOR56=INITIATOR34=3652=20210822-05:44:42.71923=asdadasasd55=BHP54=160=20210822-05:29:17.71210=035");
            var book = new Fix.IndicationBook();

            Assert.AreEqual(0, book.Indications.Count);
            Assert.IsTrue(book.Process(ioi));
            Assert.AreEqual(1, book.Indications.Count);
        }
    }   
}
