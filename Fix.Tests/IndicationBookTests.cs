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

            Assert.AreEqual(book.Process(message), Fix.IndicationBookMessageEffect.Modified);

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

            Assert.AreEqual(book.Process(message), Fix.IndicationBookMessageEffect.Modified);
            Assert.AreEqual(book.Process(message), Fix.IndicationBookMessageEffect.Rejected);
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
            message.Fields.Set(FIX_5_0SP2.Fields.Price, 45);
            var book = new Fix.IndicationBook();
            Assert.AreEqual(book.Process(message), Fix.IndicationBookMessageEffect.Modified);
            Fix.Indication? indication = book.Indications[0];
            Assert.AreEqual(indication.Price, 45);

            message = new Fix.Message { MsgType = FIX_5_0SP2.Messages.IOI.MsgType };
            message.Fields.Set(FIX_5_0SP2.Fields.SenderCompID, "SENDER");
            message.Fields.Set(FIX_5_0SP2.Fields.TargetCompID, "TARGET");
            message.Fields.Set(FIX_5_0SP2.Fields.IOIID, 2);
            message.Fields.Set(FIX_5_0SP2.Fields.IOITransType, FIX_5_0SP2.IOITransType.Replace.Value);
            message.Fields.Set(FIX_5_0SP2.Fields.IOIRefID, 1);
            message.Fields.Set(FIX_5_0SP2.Fields.Price, 50);

            Assert.AreEqual(book.Process(message), Fix.IndicationBookMessageEffect.Modified);

            Assert.AreEqual(2, book.Indications.Count);
            Assert.AreEqual(50m, book.Indications[1].Price);
            Assert.AreEqual("2", book.Indications[1].IOIID);
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
            Assert.AreEqual(book.Process(message), Fix.IndicationBookMessageEffect.Modified);

            message = new Fix.Message { MsgType = FIX_5_0SP2.Messages.IOI.MsgType };
            message.Fields.Set(FIX_5_0SP2.Fields.SenderCompID, "SENDER");
            message.Fields.Set(FIX_5_0SP2.Fields.TargetCompID, "TARGET");
            message.Fields.Set(FIX_5_0SP2.Fields.IOIID, 1);
            message.Fields.Set(FIX_5_0SP2.Fields.IOITransType, FIX_5_0SP2.IOITransType.Cancel.Value);
            message.Fields.Set(FIX_5_0SP2.Fields.IOIRefID, 2);

            Assert.AreEqual(book.Process(message), Fix.IndicationBookMessageEffect.Modified);
            Assert.AreEqual(0, book.Indications.Count);
        }
    }
}
