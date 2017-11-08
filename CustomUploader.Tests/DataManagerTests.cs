using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CustomUploader.Logic;
using CustomUploader.Logic.Timepad.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CustomUploader.Tests
{
    [TestClass]
    public class DataManagerTests
    {
        [TestMethod]
        public void CreateFolderTest()
        {
            using (var dataManager = new DataManager("client_secret.json", ParentFolderId, null))
            {
                string id = dataManager.GetOrCreateFolder("Test").Result;
                Console.WriteLine(id);
            }
        }

        [TestMethod]
        public void GetFolderTest()
        {
            using (var dataManager = new DataManager("client_secret.json", ParentFolderId, null))
            {
                string id = dataManager.GetOrCreateFolder("Test").Result;
                Assert.AreEqual(FolderId, id);
            }
        }

        [TestMethod]
        public void GetTimepadEvents()
        {
            Task<List<Event>> task =
                DataManager.GetTimepadEvents(DateTime.Now.AddDays(-7), DateTime.Now, "https://api.timepad.ru",
                                                "/v1/events", 1235);
            List<Event> events = task.Result;
            Assert.IsNotNull(events);
            Assert.AreNotSame(0, events.Count);
        }

        [TestMethod]
        public void UploadEmptyFileTest()
        {
            UploadFileTest(EmptyFilePath);
        }

        [TestMethod]
        public void UploadTextFileTest()
        {
            UploadFileTest(TextFilePath);
        }

        [TestMethod]
        public void UploadAudioFileTest()
        {
            UploadFileTest(AudioFilePath);
        }

        [TestMethod]
        public void UploadVideoFileTest()
        {
            UploadFileTest(VideoFilePath);
        }

        private static void UploadFileTest(string path)
        {
            using (var provider = new DataManager("client_secret.json", ParentFolderId, null))
            {
                bool success = provider.UploadFile(new FileInfo(path), FolderId, 10, null).Result;
                Assert.IsTrue(success);
            }
        }

        private const string ParentFolderId = "0B1IWpTUgXs1xaUI4bDA2WWFlSFU";
        private const string FolderId = "0B1IWpTUgXs1xZzBDUTV6eFBIT1U";

        private const string EmptyFilePath = "D:/Test/empty.txt";
        private const string TextFilePath = "D:/Test/text.txt";
        private const string AudioFilePath = "D:/Test/audio.mp3";
        private const string VideoFilePath = "D:/Test/video.mkv";
    }
}