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
        public void TestEmptyBook()
        {
            var book = new Fix.IndicationBook();
            Assert.AreEqual(0, book.Indications.Count);
	}
	
        [TestMethod]
        public void TestCreateIndication()
        {
            var message = new Fix.Message { MsgType = FIX_5_0SP2.Messages.IOI.MsgType };
            message.Fields.Set(FIX_5_0SP2.Fields.SenderCompID, "SENDER");
            message.Fields.Set(FIX_5_0SP2.Fields.TargetCompID, "TARGET");
            message.Fields.Set(FIX_5_0SP2.Fields.IOIID, 5000);
            message.Fields.Set(FIX_5_0SP2.Fields.IOITransType, FIX_5_0SP2.IOITransType.New.Value);

            var book = new Fix.IndicationBook();

            Assert.IsTrue(book.Process(message));
            Assert.AreEqual(1, book.Indications.Count);
        }

	[TestMethod]
        public void TestDuplicatedIndication()
        {
            var message = new Fix.Message { MsgType = FIX_5_0SP2.Messages.IOI.MsgType };
            message.Fields.Set(FIX_5_0SP2.Fields.SenderCompID, "SENDER");
            message.Fields.Set(FIX_5_0SP2.Fields.TargetCompID, "TARGET");
            message.Fields.Set(FIX_5_0SP2.Fields.IOIID, 5000);
            message.Fields.Set(FIX_5_0SP2.Fields.IOITransType, FIX_5_0SP2.IOITransType.New.Value);

            var book = new Fix.IndicationBook();

            Assert.IsTrue(book.Process(message));
            Assert.IsFalse(book.Process(message));
            Assert.AreEqual(1, book.Indications.Count);
        }

	[TestMethod]
        public void TestReplaceIndication()
        {
            var message = new Fix.Message { MsgType = FIX_5_0SP2.Messages.IOI.MsgType };
            message.Fields.Set(FIX_5_0SP2.Fields.SenderCompID, "SENDER");
            message.Fields.Set(FIX_5_0SP2.Fields.TargetCompID, "TARGET");
            message.Fields.Set(FIX_5_0SP2.Fields.IOIID, 1);
            message.Fields.Set(FIX_5_0SP2.Fields.IOITransType, FIX_5_0SP2.IOITransType.New.Value);

            var book = new Fix.IndicationBook();
            Assert.IsTrue(book.Process(message));

            message = new Fix.Message { MsgType = FIX_5_0SP2.Messages.IOI.MsgType };
            message.Fields.Set(FIX_5_0SP2.Fields.SenderCompID, "SENDER");
            message.Fields.Set(FIX_5_0SP2.Fields.TargetCompID, "TARGET");
            message.Fields.Set(FIX_5_0SP2.Fields.IOIID, 1);
            message.Fields.Set(FIX_5_0SP2.Fields.IOITransType, FIX_5_0SP2.IOITransType.Replace.Value);
            message.Fields.Set(FIX_5_0SP2.Fields.IOIRefID, 2);

            Assert.IsTrue(book.Process(message));
            Assert.AreEqual(1, book.Indications.Count);
        }

	[TestMethod]
        public void TestCancelIndication()
        {
            var message = new Fix.Message { MsgType = FIX_5_0SP2.Messages.IOI.MsgType };
            message.Fields.Set(FIX_5_0SP2.Fields.SenderCompID, "SENDER");
            message.Fields.Set(FIX_5_0SP2.Fields.TargetCompID, "TARGET");
            message.Fields.Set(FIX_5_0SP2.Fields.IOIID, 1);
            message.Fields.Set(FIX_5_0SP2.Fields.IOITransType, FIX_5_0SP2.IOITransType.New.Value);

            var book = new Fix.IndicationBook();
            Assert.IsTrue(book.Process(message));

            message = new Fix.Message { MsgType = FIX_5_0SP2.Messages.IOI.MsgType };
            message.Fields.Set(FIX_5_0SP2.Fields.SenderCompID, "SENDER");
            message.Fields.Set(FIX_5_0SP2.Fields.TargetCompID, "TARGET");
            message.Fields.Set(FIX_5_0SP2.Fields.IOIID, 1);
            message.Fields.Set(FIX_5_0SP2.Fields.IOITransType, FIX_5_0SP2.IOITransType.Cancel.Value);
            message.Fields.Set(FIX_5_0SP2.Fields.IOIRefID, 2);

            Assert.IsTrue(book.Process(message));
            Assert.AreEqual(0, book.Indications.Count);
        }
    }
}
