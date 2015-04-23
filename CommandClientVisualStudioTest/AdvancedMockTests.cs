using System;
using System.Net;
using System.Reflection;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Proshot.CommandClient;
using Rhino.Mocks;
using System.Linq;
using System.Collections.Generic;

namespace CommandClientVisualStudioTest
{
    [TestClass]
    public class AdvancedMockTests
    {
        private MockRepository mocks;

        [TestMethod]
        public void VerySimpleTest()
        {
            CMDClient client = new CMDClient(null, "Bogus network name");
            Assert.AreEqual("Bogus network name", client.NetworkName);
        }

        [TestInitialize()]
        public void Initialize()
        {
            mocks = new MockRepository();
        }

        [TestMethod]
        public void TestUserExitCommand()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            System.IO.Stream fakeStream = mocks.DynamicMock<System.IO.Stream>();
            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };

            using (mocks.Ordered())
            {
                fakeStream.Write(commandBytes, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ipLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ip, 0, 9);
                fakeStream.Flush();
                fakeStream.Write(metaDataLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(metaData, 0, 2);
                fakeStream.Flush();
            }
            mocks.ReplayAll();
            CMDClient client = new CMDClient(null, "Bogus network name");
            
            // we need to set the private variable here
            typeof(CMDClient).GetField("networkStream", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, fakeStream);

            client.SendCommandToServerUnthreaded(command);
            mocks.VerifyAll();
            
        }

        // Some C# utilities from StackOverflow
        static bool ArraysEqual<T>(T[] a1, T[] a2)
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a1.Length; i++)
            {
                if (!comparer.Equals(a1[i], a2[i])) return false;
            }
            return true;
        }
        public void Split<T>(T[] array, int index, out T[] first, out T[] second)
        {
            first = array.Take(index).ToArray();
            second = array.Skip(index).ToArray();
        }

        [TestMethod]
        public void TestUserExitCommandWithoutMocks()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            System.IO.MemoryStream fakeStream = new MemoryStream();
            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };

            CMDClient client = new CMDClient(null, "Bogus network name");

            // we need to set the private variable here
            typeof(CMDClient).GetField("networkStream", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, fakeStream);

            client.SendCommandToServerUnthreaded(command);

            // DONE: Task 2: Verify that the fakeStream has all bits.
            byte[] bufferBytes = fakeStream.ToArray();
            Assert.AreEqual(bufferBytes.Length, 23);
            byte[] commandBytes2 = {};
            byte[] ipLength2 = {};
            byte[] ip2 = {};
            byte[] metaDataLength2 = {};
            byte[] metaData2 = {};
            Split(bufferBytes, 4, out commandBytes2, out bufferBytes);
            Split(bufferBytes, 4, out ipLength2, out bufferBytes);
            Split(bufferBytes, 9, out ip2, out bufferBytes);
            Split(bufferBytes, 4, out metaDataLength2, out bufferBytes);
            Split(bufferBytes, 2, out metaData2, out bufferBytes);
            Assert.IsTrue(ArraysEqual(commandBytes, commandBytes2));
            Assert.IsTrue(ArraysEqual(ipLength, ipLength2));
            Assert.IsTrue(ArraysEqual(ip, ip2));
            Assert.IsTrue(ArraysEqual(metaDataLength, metaDataLength2));
            Assert.IsTrue(ArraysEqual(metaData, metaData2));

        }

        [TestMethod]
        public void TestSemaphoreReleaseOnNormalOperation()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            System.IO.Stream fakeStream = mocks.DynamicMock<System.IO.Stream>();
            System.Threading.Semaphore fakeSemaphore = mocks.DynamicMock<System.Threading.Semaphore>();
            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };

            using (mocks.Ordered())
            {
                Expect.Call(fakeSemaphore.WaitOne()).Return(true);
                fakeStream.Write(commandBytes, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ipLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ip, 0, 9);
                fakeStream.Flush();
                fakeStream.Write(metaDataLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(metaData, 0, 2);
                fakeStream.Flush();
                Expect.Call(fakeSemaphore.Release()).Return(0);
            }
            mocks.ReplayAll();
            CMDClient client = new CMDClient(null, "Bogus network name");

            // we need to set the private variable here
            typeof(CMDClient).GetField("networkStream", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, fakeStream);
            typeof(CMDClient).GetField("semaphore", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, fakeSemaphore);

            client.SendCommandToServerUnthreaded(command);
            mocks.VerifyAll();
        }

        [TestMethod]
        public void TestSemaphoreReleaseOnExceptionalOperation()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            System.IO.Stream fakeStream = mocks.DynamicMock<System.IO.Stream>();
            System.Threading.Semaphore fakeSemaphore = mocks.DynamicMock<System.Threading.Semaphore>();
            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };
            System.IO.IOException exception = new System.IO.IOException();

            using (mocks.Ordered())
            {
                Expect.Call(fakeSemaphore.WaitOne()).Return(true);
                fakeStream.Write(commandBytes, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ipLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ip, 0, 9);
                fakeStream.Flush();
                fakeStream.Write(metaDataLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(metaData, 0, 2);
                fakeStream.Flush();
                LastCall.On(fakeStream).Throw(exception);
                Expect.Call(fakeSemaphore.Release()).Return(0);
            }
            mocks.ReplayAll();
            CMDClient client = new CMDClient(null, "Bogus network name");

            // we need to set the private variable here
            typeof(CMDClient).GetField("networkStream", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, fakeStream);
            typeof(CMDClient).GetField("semaphore", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, fakeSemaphore);

            try
            {
                client.SendCommandToServerUnthreaded(command);
                Assert.Fail("No IOException is thrown");
            }
            catch (System.IO.IOException e) 
            { 
                // Placeholder
            }
            
            mocks.VerifyAll();

            

        }
    }
}
